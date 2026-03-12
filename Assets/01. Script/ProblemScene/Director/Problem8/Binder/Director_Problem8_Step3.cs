using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem8_Step3 - 문제8 스텝3 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 액션 카드, 마이크, 가이드 텍스트의 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제8 > 스텝3 (마무리 - 카드 선택 + STT 말하기)
/// 【부모 클래스】 Director_Problem8_Step3_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem8 Step3 GameObject에 부착
/// 【참조되는 곳】 Director_Problem8_Step3_Logic (첫 장면 결정 로직)
/// </summary>
public class Director_Problem8_Step3 : Director_Problem8_Step3_Logic
{
    [Header("===== 액션 카드 =====")]
    [SerializeField] private ActionItem[] actionChoices;           // 액션 카드 선택지 배열

    [Header("===== 마이크 =====")]
    [SerializeField] private Button micButton;                     // 마이크 녹음 버튼
    [SerializeField] private MicRecordingIndicator micIndicator;   // STT 녹음 인디케이터

    [Header("===== 가이드 텍스트 =====")]
    [SerializeField] private Text guideText;                       // 가이드 텍스트 UI
    [SerializeField] private int guideTextId_Main;                 // 메인 안내 textId
    [SerializeField] private int guideTextId_Fail;                 // 실패 안내 textId
    [SerializeField] private int guideTextId_Success;              // 성공 안내 textId

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override ActionItem[] ActionChoices => actionChoices;
    protected override Button MicButton => micButton;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Main => guideTextId_Main;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override int GuideTextId_Success => guideTextId_Success;
}
