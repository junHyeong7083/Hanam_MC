using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem9_Step3 - 문제9 스텝3 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 라운드 데이터, 하남박스, 퍼즐 이미지, 마이크, 루트의 UI 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제9 > 스텝3 (마무리 - STT 말하기)
/// 【부모 클래스】 Director_Problem9_Step3_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem9 Step3 GameObject에 부착
/// 【참조되는 곳】 Director_Problem9_Step3_Logic (대사 조각 말하기 로직)
/// </summary>
public class Director_Problem9_Step3 : Director_Problem9_Step3_Logic
{
    [Header("===== 라운드 데이터 =====")]
    [SerializeField] private RoundData[] rounds;                   // 3라운드 데이터 배열

    [Header("===== 하남박스 =====")]
    [SerializeField] private Text guideText;                       // 가이드 텍스트
    [SerializeField] private int guideTextId_Fail;                 // 실패 안내 textId
    [SerializeField] private int guideTextId_Success;              // 완료 성공 textId

    [Header("===== 퍼즐 이미지 =====")]
    [SerializeField] private Image puzzleImage;                    // 퍼즐 이미지 (키워드/전체 문장)
    [SerializeField] private Text puzzleText;                      // 퍼즐 텍스트

    [Header("===== 마이크 =====")]
    [SerializeField] private Button micButton;                     // 마이크 녹음 버튼
    [SerializeField] private MicRecordingIndicator micIndicator;   // STT 녹음 인디케이터

    [Header("===== 루트 =====")]
    [SerializeField] private GameObject mainRoot;                  // 메인 활동 UI 루트
    [SerializeField] private GameObject completeRoot;              // 완료 UI 루트

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RoundData[] Rounds => rounds;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override int GuideTextId_Success => guideTextId_Success;
    protected override Image PuzzleImage => puzzleImage;
    protected override Text PuzzleText => puzzleText;
    protected override Button MicButton => micButton;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject MainRoot => mainRoot;
    protected override GameObject CompleteRoot => completeRoot;
}
