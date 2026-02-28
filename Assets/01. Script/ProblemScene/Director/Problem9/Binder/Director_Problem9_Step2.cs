using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem9_Step2 : Director_Problem9_Step2_Logic
{
    [Header("===== 라운드 데이터 =====")]
    [SerializeField] private RoundData[] rounds;

    [Header("===== 하남박스 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_Fail;
    [SerializeField] private Button nextDialogueButton;
    [SerializeField] private GameObject nextStepButtonRoot;

    [Header("===== 씬 이미지 =====")]
    [SerializeField] private Image sceneCardImage;

    [Header("===== 질문 영역 =====")]
    [SerializeField] private GameObject questionRoot;
    [SerializeField] private Button[] questionButtons;
    [SerializeField] private Text[] questionLabels;

    [Header("===== 답변 영역 =====")]
    [SerializeField] private GameObject answerRoot;
    [SerializeField] private Text speechBubbleText;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RoundData[] Rounds => rounds;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override Button NextDialogueButton => nextDialogueButton;
    protected override GameObject NextStepButtonRoot => nextStepButtonRoot;
    protected override Image SceneCardImage => sceneCardImage;
    protected override GameObject QuestionRoot => questionRoot;
    protected override Button[] QuestionButtons => questionButtons;
    protected override Text[] QuestionLabels => questionLabels;
    protected override GameObject AnswerRoot => answerRoot;
    protected override Text SpeechBubbleText => speechBubbleText;
}
