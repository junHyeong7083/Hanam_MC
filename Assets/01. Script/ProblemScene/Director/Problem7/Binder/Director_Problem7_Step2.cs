using UnityEngine;

/// <summary>
/// Director_Problem7_Step2 - 문제7 스텝2 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 가면 선택/감정 선택 화면의 UI 참조와 전환 딜레이를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제7 > 스텝2 (메인 활동 - 가면/감정 선택)
/// 【부모 클래스】 Director_Problem7_Step2_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem7 Step2 GameObject에 부착
/// 【참조되는 곳】 Director_Problem7_Step2_Logic (가면/감정 선택 로직)
/// </summary>
public class Director_Problem7_Step2 : Director_Problem7_Step2_Logic
{
    [Header("===== 페이즈 전환 텍스트 =====")]
    [SerializeField] private int maskSelectedTextId;              // 가면 선택 후 감정 화면 전환 시 안내 textId

    [Header("===== 가면 선택 화면 =====")]
    [SerializeField] private GameObject selectMaskRoot;            // 가면 선택 UI 루트
    [Tooltip("4개 가면: id/labelTextId/button 설정")]
    [SerializeField] private ChoiceItem[] maskChoices;             // 가면 선택지 4개

    [Header("===== 진짜 마음 선택 화면 =====")]
    [SerializeField] private GameObject selectFeelingRoot;         // 감정 선택 UI 루트
    [Tooltip("4개 감정: id/labelTextId/button 설정")]
    [SerializeField] private ChoiceItem[] feelingChoices;          // 감정 선택지 4개

    [Header("===== 전환 딜레이 (초) =====")]
    [SerializeField] private float maskSelectDelay = 2.0f;         // 가면 선택 → 감정 화면 전환 딜레이
    [SerializeField] private float feelingSelectDelay = 2.0f;      // 감정 선택 → 완료 처리 딜레이

    // ===== 부모 추상 프로퍼티를 인스펙터 필드로 연결 =====

    protected override int MaskSelectedTextId => maskSelectedTextId;

    protected override GameObject SelectMaskRoot => selectMaskRoot;
    protected override ChoiceItem[] MaskChoices => maskChoices;

    protected override GameObject SelectFeelingRoot => selectFeelingRoot;
    protected override ChoiceItem[] FeelingChoices => feelingChoices;

    protected override float MaskSelectDelay => maskSelectDelay;
    protected override float FeelingSelectDelay => feelingSelectDelay;
}
