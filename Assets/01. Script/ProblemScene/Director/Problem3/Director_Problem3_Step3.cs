using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Director / Problem3 / Step3
/// - 3개 보기 중 STT로 정답 선택
/// - 인식 시 outline 표시, 정답이면 나머지 페이드아웃
/// - 텍스트는 CSV textId로 관리
/// </summary>
public class Director_Problem3_Step3
    : Problem3_MultipleChoiceStepBase<Director_Problem3_Step3.Question>
{
    [Serializable]
    public class Question : Problem3_Question
    {
        public string id;
        public int questionTextId;
        public int[] optionTextIds;
        public int correctIndex;
        [TextArea] public string[] wrongHints;
        [Tooltip("STT 매칭용 키워드 (옵션 인덱스별)")]
        public string[] keywords;

        public string Id => id;
        public string QuestionText => questionTextId != 0 ? ProblemRuntime.L(questionTextId) : "";

        public string[] Options
        {
            get
            {
                if (optionTextIds == null) return Array.Empty<string>();
                var result = new string[optionTextIds.Length];
                for (int i = 0; i < optionTextIds.Length; i++)
                    result[i] = ProblemRuntime.L(optionTextIds[i]);
                return result;
            }
        }

        public int CorrectIndex => correctIndex;
        public string[] WrongHints => wrongHints ?? Array.Empty<string>();
        public string[] Keywords => keywords;
    }

    [Header("문제 배열 데이터")]
    [SerializeField] private Question[] questions;

    [Header("상단 가이드 텍스트")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdAfter;

    [Header("상단 버튼들")]
    [SerializeField] private GameObject micButtonRoot;
    [SerializeField] private GameObject summaryButtonRoot;

    [Header("버튼 아웃라인 (각 옵션 버튼별)")]
    [SerializeField] private GameObject[] outlineImages;

    [Header("힌트 UI (베이스 호환)")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Text hintLabel;
    [SerializeField] private CanvasGroup hintCanvasGroup;
    [SerializeField] private float hintShowDuration = 1.5f;
    [SerializeField] private float hintFadeDuration = 0.4f;

    [Header("정답 시 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRootOnCorrect;

    [Header("마이크 STT")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("오답 처리")]
    [SerializeField] private float wrongHintDuration = 2f;
    [SerializeField] private float incorrectFadeDuration = 0.5f;

    private Coroutine _wrongHintRoutine;

    // ====== 베이스로 전달할 프로퍼티 ======
    protected override GameObject HintRoot => hintRoot;
    protected override Text HintLabel => hintLabel;
    protected override CanvasGroup HintCanvasGroup => hintCanvasGroup;
    protected override GameObject HideRootOnCorrect => hideRootOnCorrect;
    protected override Color DisabledColor => Color.white;
    protected override int QuestionCount => questions != null ? questions.Length : 0;
    protected override float HintShowDuration => hintShowDuration;
    protected override float HintFadeDuration => hintFadeDuration;

    protected override Question GetQuestion(int index)
    {
        if (questions == null || index < 0 || index >= questions.Length)
            return null;
        return questions[index];
    }

    protected override void OnStepEnter()
    {
        base.OnStepEnter();

        SetGuideToQuestion();
        HideAllOutlines();

        if (micButtonRoot != null)
            micButtonRoot.SetActive(true);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(false);

        if (micIndicator != null)
        {
            micIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
            micIndicator.OnKeywordMatched += OnSTTKeywordMatched;
            micIndicator.OnNoMatch -= OnSTTNoMatch;
            micIndicator.OnNoMatch += OnSTTNoMatch;
        }

    }

    protected override void ApplyQuestionUI(int index, Question q)
    {
        SetGuideToQuestion();
        HideAllOutlines();

        base.ApplyQuestionUI(index, q);

        RestoreOptionButtons();

        if (micIndicator != null && q != null)
        {
            var keywords = q.Keywords;
            if (keywords != null && keywords.Length > 0)
                micIndicator.SetKeywords(keywords);
            else
                micIndicator.SetKeywords(q.Options);
        }
    }

    protected override void HandleWrong(int optionIndex)
    {
        ShowOutline(optionIndex);

        var q = GetQuestion(_currentQuestionIndex);
        if (q != null && guideText != null)
        {
            var wrongHints = q.WrongHints;
            string hint = null;

            if (wrongHints != null &&
                optionIndex >= 0 &&
                optionIndex < wrongHints.Length &&
                !string.IsNullOrEmpty(wrongHints[optionIndex]))
            {
                hint = wrongHints[optionIndex];
            }

            if (!string.IsNullOrEmpty(hint))
            {
                if (_wrongHintRoutine != null)
                    StopCoroutine(_wrongHintRoutine);
                _wrongHintRoutine = StartCoroutine(WrongHintRoutine(hint));
            }
        }
    }

    private IEnumerator WrongHintRoutine(string hint)
    {
        guideText.text = hint;
        yield return new WaitForSeconds(wrongHintDuration);
        SetGuideToQuestion();
        HideAllOutlines();
        _wrongHintRoutine = null;
    }

    protected override void HandleCorrect(int optionIndex)
    {
        ShowOutline(optionIndex);

        SetGuideAfter();

        FadeOutIncorrectOptions(optionIndex);

        if (hideRootOnCorrect != null)
            hideRootOnCorrect.SetActive(false);

        if (completionGate != null)
            completionGate.MarkOneDone();

        GoNextQuestionOrFinish();
    }

    protected override void OnAllQuestionsCompleted()
    {
        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(true);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (micIndicator != null)
        {
            micIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
            micIndicator.OnNoMatch -= OnSTTNoMatch;
        }

        HideAllOutlines();
        KillFadeTweens();

        if (_wrongHintRoutine != null)
        {
            StopCoroutine(_wrongHintRoutine);
            _wrongHintRoutine = null;
        }
    }

    // ====== STT 이벤트 핸들러 ======

    private void OnSTTKeywordMatched(int matchedIndex)
    {
        if (_stepCompleted) return;

        var q = GetQuestion(_currentQuestionIndex);
        if (q == null) return;

        int correctIndex = q.CorrectIndex;
        bool isCorrect = (matchedIndex == correctIndex);

        OnQuestionAttempted(q, matchedIndex, isCorrect);

        if (isCorrect)
            HandleCorrect(matchedIndex);
        else
            HandleWrong(matchedIndex);
    }

    private void OnSTTNoMatch(string sttResult)
    {
    }

    // ====== 아웃라인 ======

    private void ShowOutline(int index)
    {
        if (outlineImages == null) return;
        for (int i = 0; i < outlineImages.Length; i++)
        {
            if (outlineImages[i] != null)
                outlineImages[i].SetActive(i == index);
        }
    }

    private void HideAllOutlines()
    {
        if (outlineImages == null) return;
        foreach (var outline in outlineImages)
            if (outline != null) outline.SetActive(false);
    }

    // ====== 버튼 유틸 ======

    private void RestoreOptionButtons()
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            btn.gameObject.SetActive(true);
            btn.interactable = true;

            var cg = btn.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    private void FadeOutIncorrectOptions(int correctOptionIndex)
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i == correctOptionIndex) continue;

            var btn = optionButtons[i];
            if (btn == null) continue;

            btn.interactable = false;

            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

            int captured = i;
            cg.DOFade(0f, incorrectFadeDuration)
                .OnComplete(() =>
                {
                    if (optionButtons[captured] != null)
                        optionButtons[captured].gameObject.SetActive(false);
                });
        }

        // 정답 버튼은 interactable 유지 (false로 하면 Unity 기본 Disabled Color로 투명해짐)
    }

    private void KillFadeTweens()
    {
        if (optionButtons == null) return;
        foreach (var btn in optionButtons)
        {
            if (btn == null) continue;
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg != null) cg.DOKill();
        }
    }

    // ====== 가이드 텍스트 ======

    private void SetGuideToQuestion()
    {
        if (guideText == null) return;
        var q = GetQuestion(_currentQuestionIndex);
        if (q != null)
            guideText.text = q.QuestionText;
    }

    private void SetGuideAfter()
    {
        if (guideText == null || guideTextIdAfter == 0) return;

        guideText.text = ProblemRuntime.L(guideTextIdAfter);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(guideTextIdAfter);
    }
}
