using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem3 / Step3
/// - 객관식 문제 형태의 강점 찾기
/// - 이펙트는 EffectController에 위임
/// </summary>
public class Director_Problem3_Step3
    : Problem3_MultipleChoiceStepBase<Director_Problem3_Step3.Question>
{
    [Serializable]
    public class Question : Problem3_Question
    {
        public string id;
        [TextArea] public string questionText;
        public string[] options;
        public int correctIndex;
        public string[] wrongHints;
        [Tooltip("STT 매칭용 키워드 (옵션 인덱스별)")]
        public string[] keywords;

        public string Id => id;
        public string QuestionText => questionText;
        public string[] Options => options;
        public int CorrectIndex => correctIndex;
        public string[] WrongHints => wrongHints;
        public string[] Keywords => keywords;
    }

    [Header("문제 배열 데이터")]
    [SerializeField] private Question[] questions;

    [Header("상단 가이드 텍스트 (정답 전/후)")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdBefore = 0;
    [SerializeField] private int guideTextIdAfter = 0;

    [Header("상단 진행 인덱스(옵션)")]
    [SerializeField] private Text topIndexText; // 예: 1/3

    [Header("상단 버튼들")]
    [SerializeField] private GameObject micButtonRoot; // 상단 마이크 버튼 루트(활성/비활성 제어용)
    [SerializeField] private GameObject summaryButtonRoot; // 상단 요약보기 버튼 루트(원하면 사용)

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem3_Step3_EffectController effectController;

    [Header("힌트 UI (이펙트 컨트롤러 미사용 시 폴백)")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Text hintLabel;
    [SerializeField] private CanvasGroup hintCanvasGroup;
    [SerializeField] private float hintShowDuration = 1.5f;
    [SerializeField] private float hintFadeDuration = 0.4f;

    [Header("정답 시 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRootOnCorrect;

    [Header("마이크 STT (옵션)")]
    [SerializeField] private MicRecordingIndicator micIndicator;

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

        SetGuideBefore();
        UpdateTopIndexText();

        if (micButtonRoot != null)
            micButtonRoot.SetActive(true);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(false);

        // MicIndicator STT 이벤트 구독
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
        // 새 문항 들어가면 "정답 전" 가이드로
        SetGuideBefore();
        UpdateTopIndexText();

        // 이펙트 컨트롤러 리셋
        if (effectController != null)
            effectController.ResetForNextQuestion();

        // 베이스 로직 실행 (질문/보기 세팅 + 힌트 초기화)
        base.ApplyQuestionUI(index, q);

        // 옵션 버튼이 숨겨져 있을 수 있으니(이전 정답 처리) 다시 켜줌
        RestoreOptionButtons();

        // MicIndicator에 현재 문제의 키워드 설정
        if (micIndicator != null && q != null)
        {
            var keywords = q.Keywords;
            if (keywords != null && keywords.Length > 0)
                micIndicator.SetKeywords(keywords);
            else
                micIndicator.SetKeywords(q.Options); // 키워드 없으면 옵션 텍스트를 키워드로 사용
        }

        // 문제 등장 애니메이션 (옵션)
        if (effectController != null)
            effectController.PlayQuestionAppear();
    }

    protected override void HandleWrong(int optionIndex)
    {
        // 이펙트 컨트롤러가 있으면 사용
        if (effectController != null)
        {
            var q = GetQuestion(_currentQuestionIndex);
            if (q == null) return;

            string hint = GetHintText(q.WrongHints, optionIndex);
            effectController.PlayHintSequence(hint);
        }
        else
        {
            base.HandleWrong(optionIndex);
        }
    }

    protected override void HandleCorrect(int optionIndex)
    {
        // 정답 후 상단 가이드로 변경
        SetGuideAfter();
        UpdateTopIndexText();

        // 드롭 애니메이션 재생
        if (effectController != null)
            effectController.PlayDropAnimation();

        // "정답만 남기고 나머지(0/2 등) 숨기기"
        HideIncorrectOptions(optionIndex);

        // 정답 시 숨길 루트 처리 (옵션)
        if (hideRootOnCorrect != null)
            hideRootOnCorrect.SetActive(false);

        // Gate 완료
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 다음 문제 or 종료
        GoNextQuestionOrFinish();
    }

    protected override void OnAllQuestionsCompleted()
    {
        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(true);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        // MicIndicator 이벤트 구독 해제
        if (micIndicator != null)
        {
            micIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
            micIndicator.OnNoMatch -= OnSTTNoMatch;
        }

        if (effectController != null)
            effectController.HideHintImmediate();
    }

    // ====== STT 이벤트 핸들러 ======

    private void OnSTTKeywordMatched(int matchedIndex)
    {
        if (_stepCompleted) return;

        var q = GetQuestion(_currentQuestionIndex);
        if (q == null) return;

        int correctIndex = q.CorrectIndex;
        bool isCorrect = (matchedIndex == correctIndex);

        // Attempt 로그
        OnQuestionAttempted(q, matchedIndex, isCorrect);

        if (isCorrect)
            HandleCorrect(matchedIndex);
        else
            HandleWrong(matchedIndex);
    }

    private void OnSTTNoMatch(string sttResult)
    {
        // 매칭 실패 시: 필요하면 여기서 힌트나 안내 출력
    }

    // ====== UI 유틸 ======

    private void RestoreOptionButtons()
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            btn.gameObject.SetActive(true);
            btn.interactable = true;

            // 색은 ApplyQuestionUI에서 다시 세팅되지 않으므로,
            // 필요하면 여기서 기본 색으로 복구 로직을 추가해도 됨.
        }
    }

    private void HideIncorrectOptions(int correctOptionIndex)
    {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            if (i == correctOptionIndex)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = false; // 정답 확정이면 더 이상 클릭 불가
            }
            else
            {
                btn.gameObject.SetActive(false); // 나머지 2개 숨김
            }
        }
    }

    private void UpdateTopIndexText()
    {
        if (topIndexText == null) return;

        int total = QuestionCount;
        int current = Mathf.Clamp(_currentQuestionIndex + 1, 1, Mathf.Max(1, total));
        topIndexText.text = $"{current}/{total}";
    }

    private void SetGuideBefore()
    {
        if (guideText == null) return;

        if (guideTextIdBefore != 0)
            guideText.text = ProblemRuntime.L(guideTextIdBefore);
    }

    private void SetGuideAfter()
    {
        if (guideText == null) return;

        if (guideTextIdAfter != 0)
            guideText.text = ProblemRuntime.L(guideTextIdAfter);
    }

    // ====== 텍스트 유틸 ======

    private string GetHintText(string[] wrongHints, int optionIndex)
    {
        string hint = null;

        if (wrongHints != null &&
            optionIndex >= 0 &&
            optionIndex < wrongHints.Length &&
            !string.IsNullOrEmpty(wrongHints[optionIndex]))
        {
            hint = wrongHints[optionIndex];
        }

        if (string.IsNullOrEmpty(hint))
            hint = "조금만 더 생각해볼까요? 화면에 보이는 단서를 다시 살펴보세요.";

        return hint;
    }
}