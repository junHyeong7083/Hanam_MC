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

public abstract class Director_Problem4_Step2_Logic : ProblemStepBase
{
    [Serializable]
    protected class CutAttemptLog
    {
        public string cutID;
        public string text;
        public bool isThinking;
        public string finalStatus; // "active" / "deleted" / "passed" / "cutting"
    }

    [Serializable]
    protected class CutActionLog
    {
        public string cutID;     // 어떤 컷에 대한 선택인지
        public string action;    // "cut" 또는 "pass"
        public bool wasCorrect;  // 이 선택이 정답이었는지
    }

    [Serializable]
    protected class AttemptBody
    {
        public CutAttemptLog[] cuts;
        public CutActionLog[] actions;
    }

    protected enum CutStatus
    {
        ACTIVE,   // 아직 처리 안됨
        CUTTING,  // 잘라내는 중(연출용)
        PASSED,   // 통과된 사실 컷
        DELETED   // 잘라낸 생각 컷
    }

    // ======================
    // 자식에서 주입할 추상 프로퍼티
    // ======================

    [Header("컷 데이터 (자식 주입)")]
    protected abstract IFilmCutData[] FilmCuts { get; }

    [Header("필름 카드 UI")]
    protected abstract GameObject FilmCardRoot { get; }
    protected abstract Text FilmSentenceLabel { get; }
    protected abstract Text FilmIndexLabel { get; }

    [Header("오류 메세지 UI")]
    protected abstract GameObject ErrorRoot { get; }
    protected abstract Text ErrorLabel { get; }
    protected abstract string DefaultErrorMessage { get; }

    [Header("컬러 복원 연출용 UI")]
    protected abstract GameObject ColorRestoreRoot { get; }
    protected abstract GameObject BeforeColorRoot { get; }

    [Header("하단 버튼")]
    protected abstract Button CutBtn { get; }
    protected abstract Button PassBtn { get; }

    [Header("완료 게이트")]
    protected abstract StepCompletionGate StepCompletionGate { get; }

    [Header("오류 메시지 유지 시간")]
    protected abstract float ErrorShowDuration { get; }

    [Header("카드 위치/등장 연출")]
    protected abstract RectTransform FilmCardRect { get; }
    protected abstract CanvasGroup FilmCardCanvasGroup { get; }
    protected abstract RectTransform FilmAppearStart { get; }
    protected abstract float AppearDuration { get; }

    [Header("PASS 연출 (통과 카드 이동 위치)")]
    protected abstract RectTransform PassTargetRect { get; }
    protected abstract float PassMoveDuration { get; }

    [Header("가위 연출")]
    protected abstract RectTransform ScissorsRect { get; }
    protected abstract float ScissorsMoveDuration { get; }
    protected abstract Vector2 ScissorsOffsetFromCard { get; }

    [Header("분할 카드 연출")]
    protected abstract RectTransform CardLeftRect { get; }
    protected abstract RectTransform CardRightRect { get; }
    protected abstract CanvasGroup CardLeftCanvas { get; }
    protected abstract CanvasGroup CardRightCanvas { get; }
    protected abstract float SplitDuration { get; }
    protected abstract float SplitHorizontalOffset { get; }
    protected abstract float SplitFallDistance { get; }
    protected abstract float SplitRotateAngle { get; }

    // ======================
    // 내부 상태
    // ======================

    private CutStatus[] _status;
    private bool _isColorRestored;
    private bool _stepCompleted;
    private Coroutine _errorRoutine;

    private readonly List<CutActionLog> _actionLogs = new List<CutActionLog>();

    private Vector2 _filmCardDefaultPos;
    private bool _defaultPosInitialized;

    // =========================================
    // ProblemStepBase 구현
    // =========================================

    protected override void OnStepEnter()
    {
        var cuts = FilmCuts;
        if (cuts == null || cuts.Length == 0)
        {
            Debug.LogWarning("[Problem4_Step2] FilmCuts 가 비어 있음");
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

        var cardRect = FilmCardRect;

        if (cardRect != null && !_defaultPosInitialized)
        {
            _filmCardDefaultPos = cardRect.anchoredPosition;
            _defaultPosInitialized = true;
        }

        if (ColorRestoreRoot != null) ColorRestoreRoot.SetActive(false);
        if (BeforeColorRoot != null) BeforeColorRoot.SetActive(true);

        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }
        if (ErrorRoot != null)
            ErrorRoot.SetActive(false);

        if (CutBtn != null) CutBtn.interactable = true;
        if (PassBtn != null) PassBtn.interactable = true;

        if (StepCompletionGate != null)
            StepCompletionGate.ResetGate(1);

        if (FilmCardRoot != null)
            FilmCardRoot.SetActive(true);
        if (FilmCardCanvasGroup != null)
            FilmCardCanvasGroup.alpha = 1f;

        if (CardLeftRect != null)
            CardLeftRect.gameObject.SetActive(false);
        if (CardRightRect != null)
            CardRightRect.gameObject.SetActive(false);

        if (CardLeftCanvas != null)
            CardLeftCanvas.alpha = 0f;
        if (CardRightCanvas != null)
            CardRightCanvas.alpha = 0f;

        if (ScissorsRect != null)
            ScissorsRect.gameObject.SetActive(false);

        RefreshCurrentCutUI();

        if (FilmCardRect != null && FilmCardCanvasGroup != null)
            StartCoroutine(PlayAppearAnimationForCurrentCard());
    }

    protected override void OnStepExit()
    {
        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }
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

        if (FilmCardRoot != null)
            FilmCardRoot.SetActive(true);

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
            StartCoroutine(PlayCutAnimationAndProceed(idx));
        }
        else
        {
            ShowError("이 문장은 '사실'이에요. 통과시켜 볼까요?");
        }
    }

    public void OnClickPass()
    {
        if (_stepCompleted) return;

        int idx = GetCurrentActiveIndex();
        var cuts = FilmCuts;
        if (cuts == null || idx == -1) return;

        var cut = cuts[idx];

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
            StartCoroutine(PlayPassAnimationAndProceed(idx));
        }
        else
        {
            ShowError("이 문장은 '내 생각' 같아요. 잘라내 볼까요?");
        }
    }

    // =========================================
    // 오류 메시지
    // =========================================

    private void ShowError(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            msg = DefaultErrorMessage;

        if (ErrorLabel != null)
            ErrorLabel.text = msg;

        if (ErrorRoot != null)
            ErrorRoot.SetActive(true);

        if (_errorRoutine != null)
            StopCoroutine(_errorRoutine);

        if (ErrorShowDuration > 0f)
            _errorRoutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(ErrorShowDuration);

        if (ErrorRoot != null)
            ErrorRoot.SetActive(false);

        _errorRoutine = null;
    }

    // =========================================
    // 완료 조건 체크 + 마무리
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

        if (BeforeColorRoot != null)
            BeforeColorRoot.SetActive(false);
        if (ColorRestoreRoot != null)
            ColorRestoreRoot.SetActive(true);

        if (CutBtn != null) CutBtn.interactable = false;
        if (PassBtn != null) PassBtn.interactable = false;

        SaveFilmEditingAttempt();

        if (StepCompletionGate != null)
            StepCompletionGate.MarkOneDone();

        Debug.Log("[Problem4_Step2] 필름 편집 스텝 완료");
    }

    // =========================================
    // 카드 등장 연출
    // =========================================

    private IEnumerator PlayAppearAnimationForCurrentCard()
    {
        var cardRect = FilmCardRect;
        var canvasGroup = FilmCardCanvasGroup;

        if (cardRect == null || canvasGroup == null)
            yield break;

        int idx = GetCurrentActiveIndex();
        if (idx == -1) yield break;

        if (FilmCardRoot != null)
            FilmCardRoot.SetActive(true);

        if (FilmAppearStart != null)
            cardRect.anchoredPosition = FilmAppearStart.anchoredPosition;
        else
            cardRect.anchoredPosition = _filmCardDefaultPos + new Vector2(-500f, 0f);

        canvasGroup.alpha = 0f;

        float t = 0f;
        float dur = Mathf.Max(0.01f, AppearDuration);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = t * t * (3f - 2f * t);

            cardRect.anchoredPosition = Vector2.Lerp(
                cardRect.anchoredPosition,
                _filmCardDefaultPos,
                eased
            );

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        cardRect.anchoredPosition = _filmCardDefaultPos;
        canvasGroup.alpha = 1f;
    }

    // =========================================
    // CUT 애니메이션
    // =========================================

    private IEnumerator PlayCutAnimationAndProceed(int cutIndex)
    {
        if (CutBtn != null) CutBtn.interactable = false;
        if (PassBtn != null) PassBtn.interactable = false;

        var scissorsRect = ScissorsRect;
        var cardRect = FilmCardRect;

        if (scissorsRect != null && cardRect != null)
        {
            scissorsRect.gameObject.SetActive(true);

            Vector2 cardPos = cardRect.anchoredPosition;
            Vector2 startPos = cardPos + ScissorsOffsetFromCard;
            Vector2 endPos = cardPos;

            scissorsRect.anchoredPosition = startPos;

            float t = 0f;
            float dur = Mathf.Max(0.01f, ScissorsMoveDuration);
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float eased = t * t * (3f - 2f * t);

                scissorsRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

                yield return null;
            }

            scissorsRect.anchoredPosition = endPos;
            yield return new WaitForSeconds(0.05f);
        }

        var cardLeftRect = CardLeftRect;
        var cardRightRect = CardRightRect;
        var cardLeftCanvas = CardLeftCanvas;
        var cardRightCanvas = CardRightCanvas;
        var filmCardRoot = FilmCardRoot;
        var cardCanvasGroup = FilmCardCanvasGroup;

        if (cardRect != null &&
            cardLeftRect != null && cardRightRect != null &&
            cardLeftCanvas != null && cardRightCanvas != null)
        {
            Vector2 center = cardRect.anchoredPosition;

            if (filmCardRoot != null)
                filmCardRoot.SetActive(false);

            cardLeftRect.gameObject.SetActive(true);
            cardRightRect.gameObject.SetActive(true);

            cardLeftRect.anchoredPosition = center;
            cardRightRect.anchoredPosition = center;

            cardLeftRect.localRotation = Quaternion.identity;
            cardRightRect.localRotation = Quaternion.identity;

            cardLeftCanvas.alpha = 1f;
            cardRightCanvas.alpha = 1f;

            float t2 = 0f;
            float dur2 = Mathf.Max(0.01f, SplitDuration);
            while (t2 < 1f)
            {
                t2 += Time.deltaTime / dur2;
                float eased = t2 * t2 * (3f - 2f * t2);
                float alpha = 1f - eased;

                Vector2 leftPos = center + new Vector2(-SplitHorizontalOffset * eased,
                                                       -SplitFallDistance * eased);
                Vector2 rightPos = center + new Vector2(SplitHorizontalOffset * eased,
                                                        -SplitFallDistance * eased);

                cardLeftRect.anchoredPosition = leftPos;
                cardRightRect.anchoredPosition = rightPos;

                cardLeftRect.localRotation = Quaternion.Euler(0f, 0f, -SplitRotateAngle * eased);
                cardRightRect.localRotation = Quaternion.Euler(0f, 0f, SplitRotateAngle * eased);

                cardLeftCanvas.alpha = alpha;
                cardRightCanvas.alpha = alpha;

                yield return null;
            }

            cardLeftCanvas.alpha = 0f;
            cardRightCanvas.alpha = 0f;

            cardLeftRect.gameObject.SetActive(false);
            cardRightRect.gameObject.SetActive(false);
        }
        else
        {
            if (cardRect != null && cardCanvasGroup != null)
            {
                Vector2 start = cardRect.anchoredPosition;
                Vector2 end = start + new Vector2(0f, -SplitFallDistance);

                float t = 0f;
                float dur = Mathf.Max(0.01f, SplitDuration);
                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    float eased = t * t * (3f - 2f * t);

                    cardRect.anchoredPosition = Vector2.Lerp(start, end, eased);
                    cardCanvasGroup.alpha = 1f - eased;

                    yield return null;
                }
            }
        }

        if (scissorsRect != null)
            scissorsRect.gameObject.SetActive(false);

        RefreshCurrentCutUI();

        if (_stepCompleted)
            yield break;

        if (FilmCardRect != null && FilmCardCanvasGroup != null)
        {
            FilmCardRoot.SetActive(true);
            FilmCardCanvasGroup.alpha = 0f;

            yield return StartCoroutine(PlayAppearAnimationForCurrentCard());
        }

        if (CutBtn != null) CutBtn.interactable = true;
        if (PassBtn != null) PassBtn.interactable = true;
    }

    // =========================================
    // PASS 애니메이션
    // =========================================

    private IEnumerator PlayPassAnimationAndProceed(int cutIndex)
    {
        if (CutBtn != null) CutBtn.interactable = false;
        if (PassBtn != null) PassBtn.interactable = false;

        var cardRect = FilmCardRect;
        var cardCanvasGroup = FilmCardCanvasGroup;

        if (cardRect != null && cardCanvasGroup != null)
        {
            Vector2 start = cardRect.anchoredPosition;
            Vector2 end;

            if (PassTargetRect != null)
                end = PassTargetRect.anchoredPosition;
            else
                end = start + new Vector2(300f, 0f);

            float t = 0f;
            float dur = Mathf.Max(0.01f, PassMoveDuration);

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float eased = t * t * (3f - 2f * t);

                cardRect.anchoredPosition = Vector2.Lerp(start, end, eased);
                cardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);

                yield return null;
            }

            cardCanvasGroup.alpha = 0f;
        }

        RefreshCurrentCutUI();

        if (_stepCompleted)
            yield break;

        if (FilmCardRect != null && FilmCardCanvasGroup != null)
        {
            FilmCardRoot.SetActive(true);
            FilmCardCanvasGroup.alpha = 0f;

            yield return StartCoroutine(PlayAppearAnimationForCurrentCard());
        }

        if (CutBtn != null) CutBtn.interactable = true;
        if (PassBtn != null) PassBtn.interactable = true;
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
