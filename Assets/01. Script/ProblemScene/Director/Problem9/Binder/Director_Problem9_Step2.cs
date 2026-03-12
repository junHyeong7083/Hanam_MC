using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem9_Step2 - 문제9 스텝2 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 라운드 데이터, 하남박스, 씬 이미지, 질문/답변 영역,
///          대화 이미지의 UI 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제9 > 스텝2 (메인 활동 - 3라운드 대사 선택)
/// 【부모 클래스】 Director_Problem9_Step2_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem9 Step2 GameObject에 부착
/// 【참조되는 곳】 Director_Problem9_Step2_Logic (대사 선택 로직)
/// </summary>
public class Director_Problem9_Step2 : Director_Problem9_Step2_Logic
{
    [Header("===== 라운드 데이터 =====")]
    [SerializeField] private RoundData[] rounds;                   // 3라운드 데이터 배열

    [Header("===== 하남박스 =====")]
    [SerializeField] private Text guideText;                       // 가이드/결과 텍스트
    [SerializeField] private int guideTextId_Fail;                 // 오답 안내 textId
    [SerializeField] private Button nextDialogueButton;            // 다음 라운드 버튼

    [Header("===== 씬 이미지 =====")]
    [SerializeField] private Image sceneCardImage;                 // 씬 카드 이미지 (질문/답변)

    [Header("===== 질문 영역 =====")]
    [SerializeField] private GameObject questionRoot;              // 질문 UI 루트
    [SerializeField] private Button[] questionButtons;             // 선택지 버튼 (3개)
    [SerializeField] private Text[] questionLabels;                // 선택지 라벨 (3개)

    [Header("===== 답변 영역 =====")]
    [SerializeField] private GameObject answerRoot;                // 답변 UI 루트
    [SerializeField] private Text speechBubbleText;                // 말풍선 텍스트

    [Header("===== 대화 이미지 =====")]
    [SerializeField] private GameObject myDialogueImage;           // 내 캐릭터 이미지
    [SerializeField] private GameObject otherDialogueImage;        // 상대 캐릭터 이미지

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RoundData[] Rounds => rounds;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override Button NextDialogueButton => nextDialogueButton;
    protected override Image SceneCardImage => sceneCardImage;
    protected override GameObject QuestionRoot => questionRoot;
    protected override Button[] QuestionButtons => questionButtons;
    protected override Text[] QuestionLabels => questionLabels;
    protected override GameObject AnswerRoot => answerRoot;
    protected override Text SpeechBubbleText => speechBubbleText;
    protected override GameObject MyDialogueImage => myDialogueImage;
    protected override GameObject OtherDialogueImage => otherDialogueImage;
}
