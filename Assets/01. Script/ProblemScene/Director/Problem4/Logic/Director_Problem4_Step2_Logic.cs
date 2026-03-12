using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 필름 컷 데이터 인터페이스.
/// 각 컷의 ID, 표시 텍스트, 생각/사실 유형을 정의한다.
/// </summary>
public interface IFilmCutData
{
    string CutId { get; }      // 컷 고유 ID (로그용)
    string Text { get; }       // 컷에 표시할 텍스트 (CSV 기반)
    bool IsThinking { get; }   // true=생각(편집 대상), false=사실(통과)
}

/// <summary>
/// Director_Problem4_Step2_Logic - 문제4 스텝2의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 필름 컷을 "생각(편집 대상)" vs "사실(통과)"로 분류하는 활동을 담당한다.
///         사용자가 Cut(편집) 또는 Pass(통과) 버튼을 눌러 각 컷을 판정한다.
///         정답이면 컷/통과 애니메이션 재생 후 다음 카드로 진행,
///         오답이면 에러 메시지 + 흔들림 효과를 표시한다.
///         모든 생각 컷이 삭제되고 사실 컷이 통과되면 색상 복원 애니메이션 재생 후 완료.
///         애니메이션 처리는 Problem4_Step2_EffectController에 위임한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측.
/// 【문제/스텝】 Director 테마 / 문제4 / 스텝2 (메인 활동 - 필름 컷 편집)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem4_Step2 (Binder 자식 클래스)
/// 【참조되는 곳】 IFilmCutData (컷 데이터 인터페이스), Problem4_Step2_EffectController (애니메이션),
///               DialogueSequencer (대사/에러 메시지), StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → 그레이스케일 + 등장 애니메이션 → enter 대사 →
///         카드별 Cut/Pass 선택 → 정답: 애니메이션 → 다음 카드 / 오답: 에러 흔들림 →
///         모든 분류 완료 → 색상 복원 → 완료 팝업 → DB 저장 → 다음 스텝
/// </summary>
public abstract class Director_Problem4_Step2_Logic : ProblemStepBase
{
    /// <summary>각 컷의 최종 분류 결과 (DB 저장용)</summary>
    [Serializable]
    protected class CutAttemptLog
    {
        public string cutID;         // 컷 고유 ID
        public string text;          // 컷 텍스트 내용
        public bool isThinking;      // "생각" 컷 여부
        public string finalStatus;   // 최종 상태 ("deleted", "passed", "active", "cutting")
    }

    /// <summary>사용자의 개별 행동 로그 (DB 저장용)</summary>
    [Serializable]
    protected class CutActionLog
    {
        public string cutID;         // 대상 컷 ID
        public string action;        // "cut" 또는 "pass"
        public bool wasCorrect;      // 정답 여부
    }

    /// <summary>Attempt 전체 페이로드 (DB 저장용)</summary>
    [Serializable]
    protected class AttemptBody
    {
        public CutAttemptLog[] cuts;     // 모든 컷의 최종 상태
        public CutActionLog[] actions;   // 모든 행동 로그
    }

    /// <summary>각 컷의 현재 상태를 나타내는 열거형</summary>
    protected enum CutStatus
    {
        ACTIVE,    // 아직 처리되지 않음 (현재 또는 대기 중)
        CUTTING,   // 편집(삭제) 애니메이션 진행 중
        PASSED,    // 통과 완료
        DELETED    // 편집(삭제) 완료
    }

    // ======================
    // 자식에서 제공할 추상 프로퍼티
    // ======================

    [Header("컷 데이터 (자식 제공)")]
    protected abstract IFilmCutData[] FilmCuts { get; }

    [Header("필름 카드 UI")]
    protected abstract Text FilmSentenceLabel { get; }
    protected abstract Text FilmIndexLabel { get; }

    [Header("하단 버튼")]
    protected abstract Button CutBtn { get; }
    protected abstract Button PassBtn { get; }

    [Header("완료 게이트")]
    protected abstract StepCompletionGate StepCompletionGate { get; }

    [Header("이펙트 컨트롤러")]
    protected abstract Problem4_Step2_EffectController EffectController { get; }

    [Header("오답 피드백")]
    protected abstract int ErrorTextId { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("완료 시 UI")]
    protected abstract GameObject HideObjectOnComplete { get; }
    protected abstract RectTransform ShowImageOnComplete { get; }
    protected virtual float CompletionDelayDuration => 4f;

    // ======================
    // 내부 상태
    // ======================

    private bool _interactionLocked = true;
    private CutStatus[] _status;
    private bool _isColorRestored;
    private bool _stepCompleted;
    private readonly List<CutActionLog> _actionLogs = new List<CutActionLog>();

    // =========================================
    // ProblemStepBase 구현
    // =========================================

    protected override void OnStepEnter()
    {
        var cuts = FilmCuts;
        if (cuts == null || cuts.Length == 0)
        {
            Debug.LogWarning("[Problem4_Step2] FilmCuts가 비어있음");
            if (FilmSentenceLabel != null)
                FilmSentenceLabel.text = "(설정된 필름 컷이 없습니다)";
            if (StepCompletionGate != null)
                StepCompletionGate.ResetGate(1);
            return;
        }

        _status = new CutStatus[cuts.Length];
        for (int i = 0; i < _status.Length; ++i)
            _status[i] = CutStatus.ACTIVE;

        _isColorRestored = false;
        _stepCompleted = false;
        _actionLogs.Clear();

        // 이펙트 컨트롤러 초기화
        var effect = EffectController;
        if (effect != null)
        {
            effect.SaveDefaultPosition();
            effect.SetGrayscale();
            effect.ResetForNextCard();
        }

        // 버튼 활성화
        if (CutBtn != null) CutBtn.interactable = true;
        if (PassBtn != null) PassBtn.interactable = true;

        // 완료 게이트 리셋
        if (StepCompletionGate != null)
            StepCompletionGate.ResetGate(1);

        // 첫 카드 표시
        RefreshCurrentCutUI();

        // 등장 애니메이션 (버튼은 이미 활성화됨)
        if (effect != null)
        {
            effect.PlayAppearAnimation();
        }

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;
    }

    // =========================================
    // 현재 컷 찾기 & UI 갱신
    // =========================================

    private int GetCurrentActiveIndex()
    {
        if (_status == null) return -1;

        for (int i = 0; i < _status.Length; i++)
        {
            if (_status[i] == CutStatus.ACTIVE ||
                _status[i] == CutStatus.CUTTING)
            {
                return i;
            }
        }

        return -1;
    }

    private void RefreshCurrentCutUI()
    {
        int idx = GetCurrentActiveIndex();
        var cuts = FilmCuts;

        if (cuts == null)
            return;

        if (idx == -1)
        {
            TryCompleteStep();
            return;
        }

        var cut = cuts[idx];

        if (FilmSentenceLabel != null)
            FilmSentenceLabel.text = cut.Text;

        if (FilmIndexLabel != null)
            FilmIndexLabel.text = string.Format("{0} / {1}", idx + 1, cuts.Length);
    }

    // =========================================
    // 버튼 OnClick
    // =========================================

    public void OnClickCut()
    {
        if (_interactionLocked) return;
        if (_stepCompleted) return;

        int idx = GetCurrentActiveIndex();
        var cuts = FilmCuts;
        if (cuts == null || idx == -1) return;

        var cut = cuts[idx];
        var effect = EffectController;
        bool isCorrect = cut.IsThinking;

        _actionLogs.Add(new CutActionLog
        {
            cutID = cut.CutId,
            action = "cut",
            wasCorrect = isCorrect
        });

        if (isCorrect)
        {
            _status[idx] = CutStatus.DELETED;

            // 버튼 비활성화
            if (CutBtn != null) CutBtn.interactable = false;
            if (PassBtn != null) PassBtn.interactable = false;

            // 컷 애니메이션
            if (effect != null)
            {
                effect.PlayCutAnimation(OnCutAnimationComplete);
            }
            else
            {
                OnCutAnimationComplete();
            }
        }
        else
        {
            if (dialogueSequencer != null && ErrorTextId > 0)
                dialogueSequencer.SetText(ErrorTextId);

            if (effect != null)
            {
                effect.PlayErrorShake();
            }
        }
    }

    public void OnClickPass()
    {
        if (_interactionLocked) return;
        if (_stepCompleted) return;

        int idx = GetCurrentActiveIndex();
        var cuts = FilmCuts;
        if (cuts == null || idx == -1) return;

        var cut = cuts[idx];
        var effect = EffectController;
        bool isCorrect = !cut.IsThinking;

        _actionLogs.Add(new CutActionLog
        {
            cutID = cut.CutId,
            action = "pass",
            wasCorrect = isCorrect
        });

        if (isCorrect)
        {
            _status[idx] = CutStatus.PASSED;

            // 버튼 비활성화
            if (CutBtn != null) CutBtn.interactable = false;
            if (PassBtn != null) PassBtn.interactable = false;

            // 통과 애니메이션
            if (effect != null)
            {
                effect.PlayPassAnimation(OnPassAnimationComplete);
            }
            else
            {
                OnPassAnimationComplete();
            }
        }
        else
        {
            if (dialogueSequencer != null && ErrorTextId > 0)
                dialogueSequencer.SetText(ErrorTextId);

            if (effect != null)
            {
                effect.PlayErrorShake();
            }
        }
    }

    // =========================================
    // 애니메이션 완료 콜백
    // =========================================

    private void OnCutAnimationComplete()
    {
        ProceedToNextCard();
    }

    private void OnPassAnimationComplete()
    {
        ProceedToNextCard();
    }

    private void ProceedToNextCard()
    {
        RefreshCurrentCutUI();

        if (_stepCompleted)
            return;

        var effect = EffectController;

        // 다음 카드 준비
        if (effect != null)
        {
            effect.ResetForNextCard();
            effect.PlayAppearAnimation();
        }

        // 버튼 바로 활성화 (애니메이션 콜백에 의존하지 않음)
        if (CutBtn != null) CutBtn.interactable = true;
        if (PassBtn != null) PassBtn.interactable = true;
    }

    // =========================================
    // 완료 상태 체크 + 색상복원
    // =========================================

    private bool AllThinkingCutsDeleted()
    {
        var cuts = FilmCuts;
        if (cuts == null || _status == null) return false;

        for (int i = 0; i < cuts.Length; i++)
        {
            if (cuts[i].IsThinking)
            {
                if (_status[i] != CutStatus.DELETED)
                    return false;
            }
        }

        return true;
    }

    private bool AllFactCutsPassed()
    {
        var cuts = FilmCuts;
        if (cuts == null || _status == null) return false;

        for (int i = 0; i < cuts.Length; i++)
        {
            if (!cuts[i].IsThinking)
            {
                if (_status[i] != CutStatus.PASSED)
                    return false;
            }
        }

        return true;
    }

    private void TryCompleteStep()
    {
        if (_stepCompleted) return;

        bool doneThinking = AllThinkingCutsDeleted();
        bool doneFact = AllFactCutsPassed();

        if (!doneThinking || !doneFact)
        {
            int idx = GetCurrentActiveIndex();
            if (idx != -1)
                RefreshCurrentCutUI();
            return;
        }

        _stepCompleted = true;
        _isColorRestored = true;

        // 버튼 비활성화
        if (CutBtn != null) CutBtn.interactable = false;
        if (PassBtn != null) PassBtn.interactable = false;

        // 색상 복원 애니메이션
        var effect = EffectController;
        if (effect != null)
        {
            effect.PlayColorRestoreAnimation(OnColorRestoreComplete);
        }
        else
        {
            OnColorRestoreComplete();
        }

        Debug.Log("[Problem4_Step2] 필름 편집 분류 완료");
    }

    private void OnColorRestoreComplete()
    {
        SaveFilmEditingAttempt();

        // 완료 시 UI 처리
        if (HideObjectOnComplete != null)
            HideObjectOnComplete.SetActive(false);

        // 팝업 애니메이션으로 이미지 등장
        var effect = EffectController;
        if (ShowImageOnComplete != null && effect != null)
        {
            effect.PlayCompletionPopup(ShowImageOnComplete);
        }
        else if (ShowImageOnComplete != null)
        {
            ShowImageOnComplete.gameObject.SetActive(true);
        }

        // 지연 후 Gate 완료
        if (CompletionDelayDuration > 0f)
        {
            StartCoroutine(DelayedGateComplete());
        }
        else
        {
            if (StepCompletionGate != null)
                StepCompletionGate.MarkOneDone();
        }

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }

    private IEnumerator DelayedGateComplete()
    {
        yield return new WaitForSeconds(CompletionDelayDuration);

        if (StepCompletionGate != null)
            StepCompletionGate.MarkOneDone();
    }

    // =========================================
    // Attempt 저장
    // =========================================

    private void SaveFilmEditingAttempt()
    {
        var cuts = FilmCuts;
        if (cuts == null || _status == null)
            return;

        int len = cuts.Length;
        var logs = new CutAttemptLog[len];

        for (int i = 0; i < len; i++)
        {
            string statusStr = "active";

            switch (_status[i])
            {
                case CutStatus.DELETED:
                    statusStr = "deleted";
                    break;
                case CutStatus.PASSED:
                    statusStr = "passed";
                    break;
                case CutStatus.CUTTING:
                    statusStr = "cutting";
                    break;
                case CutStatus.ACTIVE:
                    statusStr = "active";
                    break;
            }

            logs[i] = new CutAttemptLog
            {
                cutID = cuts[i].CutId,
                text = cuts[i].Text,
                isThinking = cuts[i].IsThinking,
                finalStatus = statusStr
            };
        }

        var body = new AttemptBody
        {
            cuts = logs,
            actions = _actionLogs.ToArray()
        };

        SaveAttempt(body);
    }
}
