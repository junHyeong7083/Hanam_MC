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

        public string Id => id;
        public string QuestionText => questionText;
        public string[] Options => options;
        public int CorrectIndex => correctIndex;
        public string[] WrongHints => wrongHints;
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

    [Header("비활성/선택 색상")]
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.4f);

    // ====== 베이스로 전달할 프로퍼티 ======
    protected override GameObject HintRoot => hintRoot;
    protected override Text HintLabel => hintLabel;
    protected override CanvasGroup HintCanvasGroup => hintCanvasGroup;
    protected override GameObject HideRootOnCorrect => hideRootOnCorrect;
    protected override Color DisabledColor => disabledColor;
    protected override int QuestionCount => questions != null ? questions.Length : 0;
    protected override float HintShowDuration => hintShowDuration;
    protected override float HintFadeDuration => hintFadeDuration;

    protected override Question GetQuestion(int index)
    {
        if (questions == null || index < 0 || index >= questions.Length)
            return null;
        return questions[index];
    }

    // ====== 이펙트 컨트롤러 사용하도록 Override ======

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
        // 정답 효과 재생 (선택된 버튼 위치에)
        if (effectController != null)
        {
            RectTransform buttonRect = null;
            if (optionButtons != null && optionIndex >= 0 && optionIndex < optionButtons.Length)
            {
                var btn = optionButtons[optionIndex];
                if (btn != null)
                    buttonRect = btn.GetComponent<RectTransform>();
            }
            effectController.PlayCorrectEffect(buttonRect);
        }

        // 베이스 로직 실행 (버튼 색상 변경, Gate 완료 등)
        base.HandleCorrect(optionIndex);
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

        // 문제 등장 애니메이션 (옵션)
        if (effectController != null)
        {
            effectController.PlayQuestionAppear();
        }
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        // 이펙트 컨트롤러 정리
        if (effectController != null)
        {
            effectController.HideHintImmediate();
        }
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
