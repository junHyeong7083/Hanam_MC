using UnityEngine;

/// <summary>
/// Director / Problem9 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step1_Logic(부모)에 있음.
///
/// [흐름]
/// 1. NG 갈등 장면 표시 (😔 나 💥 😤 동료)
/// 2. 인벤토리에서 '대본'을 💥 충돌 아이콘에 드롭
/// 3. 1초 대기 후 introRoot 숨김
/// 4. Gate의 completeRoot(대본 카드)가 자동 표시됨
/// 5. "대본 활용하기" 버튼은 인스펙터에서 직접 이벤트 연결
/// </summary>
public class Director_Problem9_Step1 : Director_Problem9_Step1_Logic
{
    [Header("===== 드롭 타겟 (충돌 아이콘 💥) =====")]
    [SerializeField] private RectTransform conflictIconDropTargetRect;

    [Header("===== 드롭 인디케이터 =====")]
    [SerializeField] private GameObject conflictDropIndicatorRoot;

    [Header("===== 드롭 시 스케일 애니메이션 대상 =====")]
    [SerializeField] private RectTransform conflictVisualRoot;

    [Header("===== 화면 루트 =====")]
    [Tooltip("초기 화면 (NG 갈등 장면 + 조감독 대사 + 안내) - 드롭 성공 시 숨김")]
    [SerializeField] private GameObject introRoot;

    [Header("===== 완료 게이트 =====")]
    [Tooltip("completeRoot에 대본 카드 화면을 연결하세요")]
    [SerializeField] private StepCompletionGate completionGate;

    #region 부모 추상 프로퍼티 구현

    protected override RectTransform ConflictIconDropTargetRect => conflictIconDropTargetRect;
    protected override GameObject ConflictDropIndicatorRoot => conflictDropIndicatorRoot;
    protected override RectTransform ConflictVisualRoot => conflictVisualRoot;
    protected override GameObject IntroRoot => introRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;

    #endregion
}
