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
    protected override Color DisabledColor => Color.white; // 사용 안함
    protected override int QuestionCount => questions != null ? questions.Length : 0;
    protected override float HintShowDuration => hintShowDuration;
    protected override float HintFadeDuration => hintFadeDuration;

    protected override Question GetQuestion(int index)
    {
        if (questions == null || index < 0 || index >= questions.Length)
            return null;
        return questions[index];
    }

    // ====== STT + 이펙트 Override ======

    protected override void OnStepEnter()
    {
        base.OnStepEnter();

        // MicIndicator STT 이벤트 구독
        if (micIndicator != null)
        {
            micIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
            micIndicator.OnKeywordMatched += OnSTTKeywordMatched;
            micIndicator.OnNoMatch -= OnSTTNoMatch;
            micIndicator.OnNoMatch += OnSTTNoMatch;
        }
    }

    protected override void HandleWrong(int optionIndex)
    {
        // 이펙트 컨트롤러가 있으면 사용
        if (effectController != null)
        {
            var q = GetQuestion(_currentQuestionIndex);
            if (q == null) return;

            var wrongHints = q.WrongHints;
            string hint = GetHintText(wrongHints, optionIndex);

            effectController.PlayHintSequence(hint);
        }
        else
        {
            // 폴백: 베이스 클래스 로직 사용
            base.HandleWrong(optionIndex);
        }
    }

    protected override void HandleCorrect(int optionIndex)
    {
        // 드롭 애니메이션 재생 (위에서 아래로 떨어지는 효과)
        if (effectController != null)
        {
            effectController.PlayDropAnimation();
        }

        // 모든 옵션 버튼 숨기기
        if (optionButtons != null)
        {
            foreach (var btn in optionButtons)
            {
                if (btn != null)
                    btn.gameObject.SetActive(false);
            }
        }

        // 정답 시 숨길 루트 처리
        if (hideRootOnCorrect != null)
            hideRootOnCorrect.SetActive(false);

        // Gate 완료
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 다음 문제 또는 스텝 완료
        GoNextQuestionOrFinish();
    }

    protected override void ApplyQuestionUI(int index, Question q)
    {
        // 이펙트 컨트롤러 리셋
        if (effectController != null)
        {
            effectController.ResetForNextQuestion();
        }

        // 베이스 로직 실행
        base.ApplyQuestionUI(index, q);

        // MicIndicator에 현재 문제의 키워드 설정
        if (micIndicator != null && q != null)
        {
            var keywords = q.Keywords;
            if (keywords != null && keywords.Length > 0)
            {
                micIndicator.SetKeywords(keywords);
            }
            else
            {
                // 키워드가 없으면 옵션 텍스트를 키워드로 사용
                micIndicator.SetKeywords(q.Options);
            }
        }

        // 문제 등장 애니메이션 (옵션)
        if (effectController != null)
        {
            effectController.PlayQuestionAppear();
        }
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

        // 이펙트 컨트롤러 정리
        if (effectController != null)
        {
            effectController.HideHintImmediate();
        }
    }

    // ====== STT 이벤트 핸들러 ======

    /// <summary>
    /// STT 키워드 매칭 성공 시 호출
    /// </summary>
    private void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem3_Step3] STT 매칭: index={matchedIndex}");

        if (_stepCompleted) return;

        var q = GetQuestion(_currentQuestionIndex);
        if (q == null) return;

        int correctIndex = q.CorrectIndex;
        bool isCorrect = (matchedIndex == correctIndex);

        // Attempt 로그
        OnQuestionAttempted(q, matchedIndex, isCorrect);

        if (isCorrect)
        {
            Debug.Log("[Problem3_Step3] STT로 정답!");
            HandleCorrect(matchedIndex);
        }
        else
        {
            Debug.Log($"[Problem3_Step3] STT 오답: 정답={correctIndex}, 인식={matchedIndex}");
            HandleWrong(matchedIndex);
        }
    }

    /// <summary>
    /// STT 매칭 실패 시 호출
    /// </summary>
    private void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem3_Step3] STT 매칭 실패: {sttResult}");
        // 매칭 실패 시에는 아무것도 하지 않음 - 사용자가 다시 녹음하거나 버튼 클릭 가능
    }

    // ====== 유틸리티 ======

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
