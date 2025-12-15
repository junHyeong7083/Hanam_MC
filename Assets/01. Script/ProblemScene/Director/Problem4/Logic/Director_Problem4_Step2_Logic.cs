using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IFilmCutData
{
    string CutId { get; }
    string Text { get; }
    bool IsThinking { get; }
}

/// <summary>
/// Director / Problem4 / Step2 / Logic
/// - 필름 컷 분류 로직 (생각 vs 사실)
/// - 애니메이션은 EffectController에 위임
/// </summary>
public abstract class Director_Problem4_Step2_Logic : ProblemStepBase
{
    [Serializable]
    protected class CutAttemptLog
    {
        public string cutID;
        public string text;
        public bool isThinking;
        public string finalStatus;
    }

    [Serializable]
    protected class CutActionLog
    {
        public string cutID;
        public string action;
        public bool wasCorrect;
    }

    [Serializable]
    protected class AttemptBody
    {
        public CutAttemptLog[] cuts;
        public CutActionLog[] actions;
    }

    protected enum CutStatus
    {
        ACTIVE,
        CUTTING,
        PASSED,
        DELETED
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

    [Header("오답 표시 UI")]
    protected abstract GameObject ErrorRoot { get; }

    [Header("완료 시 UI")]
    protected abstract GameObject HideObjectOnComplete { get; }
    protected abstract RectTransform ShowImageOnComplete { get; }
    protected abstract Text CompletionLabel { get; }
    protected abstract string CompletionText { get; }
    protected virtual float CompletionDelayDuration => 4f;

    // ======================
    // 내부 상태
    // ======================

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
    }

    protected override void OnStepExit()
    {
        // 필요시 정리 로직 추가
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

        if (ErrorRoot != null)
            ErrorRoot.SetActive(false);
    }

    // =========================================
    // 버튼 OnClick
    // =========================================

    public void OnClickCut()
    {
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
            if (ErrorRoot != null)
                ErrorRoot.SetActive(true);

            if (effect != null)
            {
                effect.PlayErrorShake();
            }
        }
    }

    public void OnClickPass()
    {
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
            if (ErrorRoot != null)
                ErrorRoot.SetActive(true);

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

        // 텍스트 설정
        if (CompletionLabel != null && !string.IsNullOrEmpty(CompletionText))
            CompletionLabel.text = CompletionText;

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
