using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem3 / Step2 로직 베이스.
/// - 시나리오 문장을 단계별로 "새로운 관점"으로 다시 쓰는 스텝.
/// - 데이터/UI는 자식에서 주입.
/// </summary>
public interface IRewriteStepData
{
    int Id { get; }
    string OriginalText { get; }
    string RewrittenText { get; }
    string[] Options { get; }
}

public abstract class Director_Problem3_Step2_Logic : ProblemStepBase
{
    // ===== 자식에서 넘겨주는 추상 프로퍼티들 =====

    [Header("재해석 단계 데이터 (자식 주입)")]
    protected abstract IRewriteStepData[] Steps { get; }

    [Header("문장 UI")]
    protected abstract Text SentenceText { get; }
    protected abstract Color OriginalTextColor { get; }
    protected abstract Color RewrittenTextColor { get; }
    protected abstract CanvasGroup SentenceCanvasGroup { get; }

    [Header("펜 아이콘 연출 (옵션)")]
    protected abstract RectTransform PenIcon { get; }
    protected abstract float PenAnimDelay { get; }
    protected abstract float FadeOutDuration { get; }
    protected abstract float FadeInDuration { get; }

    [Header("옵션 버튼들 (최대 N개)")]
    protected abstract Button[] OptionButtons { get; }
    protected abstract Text[] OptionLabels { get; }

    [Header("옵션 색상")]
    protected abstract Color OptionNormalColor { get; }
    protected abstract Color OptionSelectedColor { get; }
    protected abstract Color OptionDisabledColor { get; }

    [Header("상단 진행도 점들 (옵션)")]
    protected abstract Image[] ProgressDots { get; }
    protected abstract Color ProgressDoneColor { get; }
    protected abstract Color ProgressCurrentColor { get; }
    protected abstract Color ProgressPendingColor { get; }

    [Header("하단 네비게이션 버튼")]
    protected abstract GameObject NextButtonRoot { get; }
    protected abstract Text NextButtonLabel { get; }
    protected abstract string MiddleStepLabel { get; }
    protected abstract string LastStepLabel { get; }

    [Header("완료 게이트 (옵션)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // ===== 내부 상태 =====
    private int _currentIndex;
    private int[] _selectedOptionIndices;   // step별 선택된 옵션 인덱스 (-1 이면 미선택)
    private bool[] _stepCompleted;          // step별 재해석 완료 여부
    private bool _isRewriting;
    private Coroutine _rewriteRoutine;

    // === Attempt 저장용 DTO ===
    [Serializable]
    private class AttemptStepLog
    {
        public int stepId;
        public string originalText;
        public string selectedOption;
        public string rewrittenText;
    }

    [Serializable]
    private class AttemptBody
    {
        public AttemptStepLog[] steps;
    }

    // =============================
    // ProblemStepBase 구현
    // =============================
    protected override void OnStepEnter()
    {
        Debug.Log("[Problem3_Step2] OnStepEnter");

        var steps = Steps;
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[Problem3_Step2] steps 데이터가 비어있음");
            if (SentenceText != null)
            {
                SentenceText.text = "(설정된 시나리오가 없습니다)";
            }
            if (CompletionGate != null)
            {
                CompletionGate.ResetGate(1);
            }
            return;
        }

        _currentIndex = 0;
        _selectedOptionIndices = new int[steps.Length];
        _stepCompleted = new bool[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            _selectedOptionIndices[i] = -1;
            _stepCompleted[i] = false;
        }

        _isRewriting = false;
        if (_rewriteRoutine != null)
        {
            StopCoroutine(_rewriteRoutine);
            _rewriteRoutine = null;
        }

        if (CompletionGate != null)
        {
            // 이 스텝 전체를 1칸짜리 Gate로 사용
            CompletionGate.ResetGate(1);
        }

        RefreshAllUI();
    }

    protected override void OnStepExit()
    {
        // 필요 시 정리
        if (_rewriteRoutine != null)
        {
            StopCoroutine(_rewriteRoutine);
            _rewriteRoutine = null;
        }
    }

    // =============================
    // UI 세팅
    // =============================

    private void RefreshAllUI()
    {
        ApplyProgressDots();
        ApplySentenceOriginalInstant();
        SetupOptionsForCurrentStep();
        UpdateNextButton();
    }

    private void ApplyProgressDots()
    {
        var dots = ProgressDots;
        if (dots == null || dots.Length == 0)
            return;

        for (int i = 0; i < dots.Length; i++)
        {
            var img = dots[i];
            if (img == null) continue;

            if (i < _currentIndex)
            {
                img.color = ProgressDoneColor;
            }
            else if (i == _currentIndex)
            {
                img.color = ProgressCurrentColor;
            }
            else
            {
                img.color = ProgressPendingColor;
            }
        }
    }

    private void ApplySentenceOriginalInstant()
    {
        var steps = Steps;
        if (steps == null || steps.Length == 0 || _currentIndex < 0 || _currentIndex >= steps.Length)
            return;

        var step = steps[_currentIndex];

        if (SentenceText != null)
        {
            SentenceText.text = step.OriginalText;
            SentenceText.color = OriginalTextColor;
        }

        if (SentenceCanvasGroup != null)
        {
            SentenceCanvasGroup.alpha = 1f;
        }

        var pen = PenIcon;
        if (pen != null)
        {
            pen.gameObject.SetActive(false);
        }
    }

    private void SetupOptionsForCurrentStep()
    {
        var optionButtons = OptionButtons;
        if (optionButtons == null || optionButtons.Length == 0)
            return;

        var steps = Steps;
        var step = steps[_currentIndex];
        var options = (step.Options != null) ? step.Options : Array.Empty<string>();

        var optionLabels = OptionLabels;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            Text label = (optionLabels != null && i < optionLabels.Length) ? optionLabels[i] : null;

            bool active = i < options.Length;

            if (btn != null)
            {
                btn.gameObject.SetActive(active);

                if (active)
                {
                    btn.interactable = true;
                    if (btn.targetGraphic != null)
                        btn.targetGraphic.color = OptionNormalColor;
                }
            }

            if (active && label != null)
            {
                label.text = options[i];
            }
        }

        // 이 스텝에서 아직 선택 안 됨
        if (_selectedOptionIndices != null)
        {
            _selectedOptionIndices[_currentIndex] = -1;
        }
    }

    private void UpdateNextButton()
    {
        bool show = (_stepCompleted != null &&
                     _currentIndex >= 0 &&
                     _currentIndex < _stepCompleted.Length &&
                     _stepCompleted[_currentIndex]);

        if (NextButtonRoot != null)
            NextButtonRoot.SetActive(show);

        if (!show) return;

        bool isLast = (_currentIndex == Steps.Length - 1);
        if (NextButtonLabel != null)
        {
            NextButtonLabel.text = isLast ? LastStepLabel : MiddleStepLabel;
        }
    }

    // =============================
    // 옵션 클릭 & 재작성 연출
    // =============================

    /// <summary>
    /// 옵션 버튼 OnClick에서 index를 넘겨 호출.
    /// ex) Button(0) → OnClickOption(0)
    /// </summary>
    public void OnClickOption(int optionIndex)
    {
        if (_isRewriting)
            return;

        var steps = Steps;
        if (steps == null || steps.Length == 0)
            return;

        if (_currentIndex < 0 || _currentIndex >= steps.Length)
            return;

        var step = steps[_currentIndex];
        if (step.Options == null || optionIndex < 0 || optionIndex >= step.Options.Length)
            return;

        // 이미 선택된 상태면 무시
        if (_selectedOptionIndices != null && _selectedOptionIndices[_currentIndex] != -1)
            return;

        _selectedOptionIndices[_currentIndex] = optionIndex;

        // 버튼 비주얼 업데이트
        ApplyOptionSelectionVisual(optionIndex);

        // 재작성 연출 시작
        if (_rewriteRoutine != null)
            StopCoroutine(_rewriteRoutine);

        _rewriteRoutine = StartCoroutine(RewriteSentenceCoroutine());
    }

    private void ApplyOptionSelectionVisual(int selectedIndex)
    {
        var optionButtons = OptionButtons;
        if (optionButtons == null)
            return;

        var steps = Steps;
        var step = steps[_currentIndex];
        int optionCount = (step.Options != null) ? step.Options.Length : 0;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            if (i >= optionCount)
            {
                btn.gameObject.SetActive(false);
                continue;
            }

            bool isSelected = (i == selectedIndex);

            // 한 번 선택하면 다시 못 누르게 하기 위해 전부 비활성화
            btn.interactable = false;

            var g = btn.targetGraphic;
            if (g != null)
            {
                if (isSelected)
                    g.color = OptionSelectedColor;
                else
                    g.color = OptionDisabledColor;
            }
        }
    }

    private IEnumerator RewriteSentenceCoroutine()
    {
        _isRewriting = true;

        // 1) 약간 대기 (React에서 500ms 정도)
        if (PenAnimDelay > 0f)
            yield return new WaitForSeconds(PenAnimDelay);

        // 2) 펜 아이콘 켜기 (있다면)
        var pen = PenIcon;
        if (pen != null)
            pen.gameObject.SetActive(true);

        // 3) 기존 문장 페이드 아웃
        var canvasGroup = SentenceCanvasGroup;
        if (canvasGroup != null && FadeOutDuration > 0f)
        {
            float t = 0f;
            while (t < FadeOutDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / FadeOutDuration);
                canvasGroup.alpha = 1f - lerp;
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        else
        {
            yield return null;
        }

        // 4) 텍스트를 재해석 버전으로 교체
        var steps = Steps;
        var step = steps[_currentIndex];

        if (SentenceText != null)
        {
            SentenceText.text = step.RewrittenText;
            SentenceText.color = RewrittenTextColor;
        }

        // 5) 페이드 인
        canvasGroup = SentenceCanvasGroup;
        if (canvasGroup != null && FadeInDuration > 0f)
        {
            float t2 = 0f;
            while (t2 < FadeInDuration)
            {
                t2 += Time.deltaTime;
                float lerp2 = Mathf.Clamp01(t2 / FadeInDuration);
                canvasGroup.alpha = lerp2;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // 6) 펜 아이콘 끄기
        pen = PenIcon;
        if (pen != null)
            pen.gameObject.SetActive(false);

        _isRewriting = false;

        // 이 step 완료 플래그
        if (_stepCompleted != null && _currentIndex >= 0 && _currentIndex < _stepCompleted.Length)
            _stepCompleted[_currentIndex] = true;

        // 다음 버튼 표시 갱신
        UpdateNextButton();
    }

    // =============================
    // 다음/완료 버튼
    // =============================

    /// <summary>
    /// 하단 "다음 장면" / "강점 찾기 단계로" 버튼에서 호출.
    /// </summary>
    public void OnClickNextOrComplete()
    {
        if (_isRewriting)
            return;

        var steps = Steps;
        if (steps == null || steps.Length == 0)
            return;

        if (_stepCompleted == null ||
            _currentIndex < 0 ||
            _currentIndex >= _stepCompleted.Length ||
            !_stepCompleted[_currentIndex])
        {
            Debug.Log("[Problem3_Step2] 아직 이 단계의 재해석이 완료되지 않음");
            return;
        }

        bool isLast = (_currentIndex == steps.Length - 1);

        if (!isLast)
        {
            // 다음 내부 step 으로
            _currentIndex++;
            _isRewriting = false;

            // 새 step UI 세팅
            RefreshAllUI();
        }
        else
        {
            // 전체 step(3단계) 완료 → Attempt 저장 + Gate 완료
            SaveRewriteLogToDb();

            if (CompletionGate != null)
            {
                CompletionGate.MarkOneDone();
            }
        }
    }

    // =============================
    // DB 저장 (Attempt)
    // =============================

    private void SaveRewriteLogToDb()
    {
        var steps = Steps;
        if (steps == null || steps.Length == 0)
            return;

        int len = steps.Length;
        var logs = new AttemptStepLog[len];

        for (int i = 0; i < len; i++)
        {
            var s = steps[i];

            int selIndex = (_selectedOptionIndices != null && i < _selectedOptionIndices.Length)
                ? _selectedOptionIndices[i]
                : -1;

            string selectedOptionText = null;
            var options = s.Options;
            if (selIndex >= 0 && options != null && selIndex < options.Length)
            {
                selectedOptionText = options[selIndex];
            }

            logs[i] = new AttemptStepLog
            {
                stepId = s.Id,
                originalText = s.OriginalText,
                selectedOption = selectedOptionText,
                rewrittenText = s.RewrittenText
            };
        }

        var body = new AttemptBody
        {
            steps = logs
        };

        // ProblemStepBase 에서 DBGateway + UserDataService 를 통해 저장
        SaveAttempt(body);
        Debug.Log("[Problem3_Step2] SaveRewriteLogToDb 호출 완료");
    }
}
