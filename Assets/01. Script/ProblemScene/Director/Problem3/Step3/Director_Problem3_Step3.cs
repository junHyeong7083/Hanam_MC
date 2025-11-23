using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Director / Problem3 / Step3
/// - 여러 문항 중에서 강점을 고르는 퀴즈.
/// - 각 문항: 질문 + 보기들 + 정답 인덱스.
/// - 문항 정답을 맞추면 버튼 숨기고 StepCompletionGate.MarkOneDone().
/// </summary>
public class Director_Problem3_Step3 : ProblemStepBase
{
    [Serializable]
    public class Question
    {
        [Tooltip("문항 ID (로그용, 선택 사항)")]
        public string id;

        [TextArea]
        [Tooltip("화면에 보여줄 질문 텍스트")]
        public string questionText;

        [Tooltip("보기 텍스트들 (Button 수보다 많지 않게 구성)")]
        public string[] options;

        [Tooltip("정답인 보기의 인덱스 (0 기반)")]
        public int correctIndex;

        [Tooltip("각 보기 선택시 보여줄 힌트 (옵션). 길이가 options와 같으면 인덱스로 대응")]
        public string[] wrongHints;
    }

    [Serializable]
    class QuestionAttemptBody
    {
        public string stepKey;
        public string questionId;
        public int questionIndex;
        public int selectedOptionIndex;
        public string selectedOptionText;
        public bool isCorrect;
        public DateTime answeredAt;
    }

    [Header("문항 배열 설정")]
    [SerializeField] private Question[] questions;

    [Header("질문 / 보기 UI")]
    [SerializeField] private Text questionLabel;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionLabels;

    [Header("힌트 UI")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Text hintLabel;

    [Header("힌트 Fade 설정")]
    [SerializeField] private CanvasGroup hintCanvasGroup;
    [SerializeField] private float hintShowDuration = 1.5f;
    [SerializeField] private float hintFadeDuration = 0.4f;

    [Header("정답 시 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRootOnCorrect;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("버튼 색상 옵션")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.4f);

    private int _currentQuestionIndex = 0;
    private bool _stepCompleted;
    private Coroutine _hintRoutine;

    protected override void OnStepEnter()
    {
        _stepCompleted = false;
        _currentQuestionIndex = 0;

        // 힌트 초기화
        if (_hintRoutine != null)
        {
            StopCoroutine(_hintRoutine);
            _hintRoutine = null;
        }

        if (hintRoot != null)
            hintRoot.SetActive(false);
        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 0f;

        // 문항 수를 게이트에 등록
        if (completionGate != null && questions != null)
            completionGate.ResetGate(questions.Length);

        ShowQuestion(_currentQuestionIndex);
    }

    protected override void OnStepExit()
    {
        // 필요 시 정리
    }

    // =======================
    // 문항 표시 / 버튼 세팅
    // =======================
    private void ShowQuestion(int index)
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("[Step3] 질문이 설정되지 않음");
            return;
        }
        if (index < 0 || index >= questions.Length)
        {
            Debug.LogWarning("[Step3] 잘못된 문항 인덱스: " + index);
            return;
        }

        var q = questions[index];

        // 질문 텍스트
        if (questionLabel != null)
            questionLabel.text = q.questionText;

        // 힌트 숨기기 + 코루틴 정리
        if (_hintRoutine != null)
        {
            StopCoroutine(_hintRoutine);
            _hintRoutine = null;
        }

        if (hintRoot != null)
            hintRoot.SetActive(false);
        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 0f;

        // 보기 버튼 세팅
        int optionCount = Mathf.Min(q.options.Length, optionButtons.Length);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            var label = (optionLabels != null && i < optionLabels.Length)
                ? optionLabels[i]
                : null;

            if (i < optionCount)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                string optionText = q.options[i];

                // 1순위: 인스펙터에서 연결한 label
                if (label != null)
                {
                    label.text = optionText;
                }
                else
                {
                    // 2순위: Button 자식의 Text
                    var childText = btn.GetComponentInChildren<Text>();
                    if (childText != null)
                    {
                        childText.text = optionText;
                    }
                    else
                    {
                        // 3순위: TMP_Text 사용 시
                        var tmp = btn.GetComponentInChildren<TMP_Text>();
                        if (tmp != null)
                            tmp.text = optionText;
                    }
                }

                // 색 초기화
                var img = btn.targetGraphic as Image;
                if (img != null)
                    img.color = normalColor;

                // 리스너 다시 연결
                btn.onClick.RemoveAllListeners();

                int capturedIndex = i;
                btn.onClick.AddListener(() => OnClickOption(capturedIndex));
            }
            else
            {
                // 사용하지 않는 버튼은 숨김
                btn.gameObject.SetActive(false);
            }
        }
    }

    // =======================
    // 보기 클릭 처리
    // =======================
    private void OnClickOption(int optionIndex)
    {
        if (questions == null || questions.Length == 0)
            return;
        if (_currentQuestionIndex < 0 || _currentQuestionIndex >= questions.Length)
            return;
        if (_stepCompleted)
            return;

        var q = questions[_currentQuestionIndex];

        bool isCorrect = (optionIndex == q.correctIndex);
        string optionText = (optionIndex >= 0 && optionIndex < q.options.Length)
            ? q.options[optionIndex]
            : null;

        // Attempt 로그
        SaveQuestionAttempt(q, optionIndex, optionText, isCorrect);

        if (isCorrect)
        {
            HandleCorrect(q, optionIndex);
        }
        else
        {
            HandleWrong(q, optionIndex);
        }
    }

    private void HandleCorrect(Question q, int optionIndex)
    {
        // 버튼 색/인터랙션 처리
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            var img = btn.targetGraphic as Image;

            if (!btn.gameObject.activeSelf)
                continue;

            if (img != null)
            {
                if (i == optionIndex)
                    img.color = correctColor;
                else
                    img.color = disabledColor;
            }

            btn.interactable = false;
        }

        // 힌트 숨김 + 코루틴 정리
        if (_hintRoutine != null)
        {
            StopCoroutine(_hintRoutine);
            _hintRoutine = null;
        }

        if (hintRoot != null)
            hintRoot.SetActive(false);
        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 0f;

        // 정답 시 hideRoot 전체를 숨기고 싶을 때
        if (hideRootOnCorrect != null)
        {
            hideRootOnCorrect.SetActive(false);
        }
        else
        {
            // 기존처럼 버튼만 숨김
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        // 게이트에 1개 완료 보고
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 다음 문항이 있으면 자동으로 넘어감
        if (_currentQuestionIndex < questions.Length - 1)
        {
            _currentQuestionIndex++;
            ShowQuestion(_currentQuestionIndex);
        }
        else
        {
            // 마지막 문항까지 다 풀었으면 이 스텝은 논리적으로 완료
            _stepCompleted = true;
            // 이후 흐름은 StepCompletionGate 에 맡김
        }
    }

    private void HandleWrong(Question q, int optionIndex)
    {
        // 힌트 텍스트 결정
        string hint = null;

        if (q.wrongHints != null &&
            optionIndex >= 0 &&
            optionIndex < q.wrongHints.Length &&
            !string.IsNullOrEmpty(q.wrongHints[optionIndex]))
        {
            hint = q.wrongHints[optionIndex];
        }

        if (string.IsNullOrEmpty(hint))
            hint = "조금만 더 생각해볼까요? 화면에 나온 단서를 다시 떠올려보세요.";

        if (hintLabel != null)
            hintLabel.text = hint;

        if (hintRoot != null)
            hintRoot.SetActive(true);

        if (hintCanvasGroup != null)
        {
            // 바로 1로 세팅 후 코루틴에서 페이드
            hintCanvasGroup.alpha = 1f;

            if (_hintRoutine != null)
                StopCoroutine(_hintRoutine);

            _hintRoutine = StartCoroutine(HintFadeRoutine());
        }
    }

    private System.Collections.IEnumerator HintFadeRoutine()
    {
        if (hintCanvasGroup == null)
            yield break;

        // 잠깐 보여주기
        if (hintShowDuration > 0f)
            yield return new WaitForSeconds(hintShowDuration);

        if (hintFadeDuration <= 0f)
        {
            hintCanvasGroup.alpha = 0f;
            if (hintRoot != null)
                hintRoot.SetActive(false);
            _hintRoutine = null;
            yield break;
        }

        float t = 0f;
        float startAlpha = hintCanvasGroup.alpha;

        while (t < hintFadeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / hintFadeDuration);
            hintCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, x);
            yield return null;
        }

        hintCanvasGroup.alpha = 0f;
        if (hintRoot != null)
            hintRoot.SetActive(false);

        _hintRoutine = null;
    }

    // =======================
    // Attempt 저장
    // =======================
    private void SaveQuestionAttempt(Question q, int optionIndex, string optionText, bool isCorrect)
    {
        var body = new QuestionAttemptBody
        {
            stepKey = context != null ? context.CurrentStepKey : null,
            questionId = q != null ? q.id : null,
            questionIndex = _currentQuestionIndex,
            selectedOptionIndex = optionIndex,
            selectedOptionText = optionText,
            isCorrect = isCorrect,
            answeredAt = DateTime.UtcNow
        };

        SaveAttempt(body);
    }
}
