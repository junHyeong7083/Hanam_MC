using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem3 / Step2
/// - 시나리오 문장을 단계별로 "새로운 관점"으로 다시 쓰는 스텝.
/// - React Part3Screen2RewriteScenario 로직을 Unity에 맞게 포팅.
/// - 마지막 단계까지 마치면 Attempt 를 DB에 1번 저장.
/// </summary>
public class Director_Problem3_Step2 : ProblemStepBase
{
    [Serializable]
    public class RewriteStepData
    {
        public int id = 1;

        [TextArea]
        public string originalText;

        [TextArea]
        public string rewrittenText;

        [TextArea]
        public string[] options;
    }

    [Header("재해석 단계 데이터")]
    [SerializeField] private RewriteStepData[] steps;

    [Header("문장 UI")]
    [SerializeField] private Text sentenceText;
    [SerializeField] private Color originalTextColor = new Color(0.24f, 0.18f, 0.14f);
    [SerializeField] private Color rewrittenTextColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private CanvasGroup sentenceCanvasGroup;   // 선택

    [Header("펜 아이콘 연출 (옵션)")]
    [SerializeField] private RectTransform penIcon;
    [SerializeField] private float penAnimDelay = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("옵션 버튼들 (최대 N개)")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionLabels;

    [Header("옵션 색상")]
    [SerializeField] private Color optionNormalColor = Color.white;
    [SerializeField] private Color optionSelectedColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private Color optionDisabledColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("상단 진행도 점들 (옵션)")]
    [SerializeField] private Image[] progressDots;
    [SerializeField] private Color progressDoneColor = new Color(0.22f, 0.8f, 0.4f);
    [SerializeField] private Color progressCurrentColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private Color progressPendingColor = new Color(1f, 1f, 1f, 0.2f);

    [Header("하단 네비게이션 버튼")]
    [SerializeField] private GameObject nextButtonRoot;    // 하나의 버튼 루트
    [SerializeField] private Text nextButtonLabel;
    [SerializeField] private string middleStepLabel = "다음 장면";
    [SerializeField] private string lastStepLabel = "강점 찾기 단계로";

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    // 내부 상태
    private int _currentIndex;
    private int[] _selectedOptionIndices;   // step별 선택된 옵션 인덱스 (-1 이면 미선택)
    private bool[] _stepCompleted;          // step별로 재해석 완료 여부
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

        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[Problem3_Step2] steps 데이터가 비어있음");
            if (sentenceText != null)
            {
                sentenceText.text = "(설정된 시나리오가 없습니다)";
            }
            if (completionGate != null)
            {
                completionGate.ResetGate(1);
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

        if (completionGate != null)
        {
            // 이 스텝 전체를 1칸짜리 Gate로 사용
            completionGate.ResetGate(1);
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
        if (progressDots == null || progressDots.Length == 0)
            return;

        for (int i = 0; i < progressDots.Length; i++)
        {
            var img = progressDots[i];
            if (img == null) continue;

            if (i < _currentIndex)
            {
                img.color = progressDoneColor;
            }
            else if (i == _currentIndex)
            {
                img.color = progressCurrentColor;
            }
            else
            {
                img.color = progressPendingColor;
            }
        }
    }

    private void ApplySentenceOriginalInstant()
    {
        var step = steps[_currentIndex];

        if (sentenceText != null)
        {
            sentenceText.text = step.originalText;
            sentenceText.color = originalTextColor;
        }

        if (sentenceCanvasGroup != null)
        {
            sentenceCanvasGroup.alpha = 1f;
        }

        if (penIcon != null)
        {
            penIcon.gameObject.SetActive(false);
        }
    }

    private void SetupOptionsForCurrentStep()
    {
        if (optionButtons == null || optionButtons.Length == 0)
            return;

        var step = steps[_currentIndex];
        var options = (step.options != null) ? step.options : Array.Empty<string>();

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
                        btn.targetGraphic.color = optionNormalColor;
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

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(show);

        if (!show) return;

        bool isLast = (_currentIndex == steps.Length - 1);
        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = isLast ? lastStepLabel : middleStepLabel;
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

        if (_currentIndex < 0 || _currentIndex >= steps.Length)
            return;

        var step = steps[_currentIndex];
        if (step.options == null || optionIndex < 0 || optionIndex >= step.options.Length)
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
        if (optionButtons == null)
            return;

        var step = steps[_currentIndex];
        int optionCount = (step.options != null) ? step.options.Length : 0;

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
                    g.color = optionSelectedColor;
                else
                    g.color = optionDisabledColor;
            }
        }
    }

    private IEnumerator RewriteSentenceCoroutine()
    {
        _isRewriting = true;

        // 1) 약간 대기 (React에서 500ms 정도)
        if (penAnimDelay > 0f)
            yield return new WaitForSeconds(penAnimDelay);

        // 2) 펜 아이콘 켜기 (있다면)
        if (penIcon != null)
            penIcon.gameObject.SetActive(true);

        // 3) 기존 문장 페이드 아웃
        if (sentenceCanvasGroup != null && fadeOutDuration > 0f)
        {
            float t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                float lerp = Mathf.Clamp01(t / fadeOutDuration);
                sentenceCanvasGroup.alpha = 1f - lerp;
                yield return null;
            }
            sentenceCanvasGroup.alpha = 0f;
        }
        else
        {
            yield return null;
        }

        // 4) 텍스트를 재해석 버전으로 교체
        var step = steps[_currentIndex];
        if (sentenceText != null)
        {
            sentenceText.text = step.rewrittenText;
            sentenceText.color = rewrittenTextColor;
        }

        // 5) 페이드 인
        if (sentenceCanvasGroup != null && fadeInDuration > 0f)
        {
            float t2 = 0f;
            while (t2 < fadeInDuration)
            {
                t2 += Time.deltaTime;
                float lerp2 = Mathf.Clamp01(t2 / fadeInDuration);
                sentenceCanvasGroup.alpha = lerp2;
                yield return null;
            }
            sentenceCanvasGroup.alpha = 1f;
        }

        // 6) 펜 아이콘 끄기
        if (penIcon != null)
            penIcon.gameObject.SetActive(false);

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

            if (completionGate != null)
            {
                completionGate.MarkOneDone();
            }
        }
    }

    // =============================
    // DB 저장 (Attempt)
    // =============================

    private void SaveRewriteLogToDb()
    {
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
            if (selIndex >= 0 && s.options != null && selIndex < s.options.Length)
            {
                selectedOptionText = s.options[selIndex];
            }

            logs[i] = new AttemptStepLog
            {
                stepId = s.id,
                originalText = s.originalText,
                selectedOption = selectedOptionText,
                rewrittenText = s.rewrittenText
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
