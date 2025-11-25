using System;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("문항 배열 설정")]
    [SerializeField] private Question[] questions;

    [Header("힌트 UI")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Text hintLabel;

    [Header("힌트 Fade 설정")]
    [SerializeField] private CanvasGroup hintCanvasGroup;
    [SerializeField] private float hintShowDuration = 1.5f;
    [SerializeField] private float hintFadeDuration = 0.4f;

    [Header("정답 시 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRootOnCorrect;

    [Header("비선택/잠금 색상")]
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.4f);

    // ====== 베이스에 전달할 프로퍼티 구현 (이름 그대로 사용) ======
    protected override GameObject HintRoot => hintRoot;
    protected override Text HintLabel => hintLabel;
    protected override CanvasGroup HintCanvasGroup => hintCanvasGroup;
    protected override GameObject HideRootOnCorrect => hideRootOnCorrect;
    protected override Color DisabledColor => disabledColor;

    // 질문 개수 / 질문 가져오기만 남겨두면 됨
    protected override int QuestionCount => questions != null ? questions.Length : 0;

    protected override float HintShowDuration => hintShowDuration;

    protected override float HintFadeDuration => hintFadeDuration;

    protected override Question GetQuestion(int index)
    {
        if (questions == null || index < 0 || index >= questions.Length)
            return null;
        return questions[index];
    }
}
