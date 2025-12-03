using UnityEngine;

/// <summary>
/// Director / Problem10 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step1_Logic(부모)에 있음.
///
/// [흐름]
/// 1. 빈 포스터 프레임 표시 (🎬 Film 아이콘 + "텅 빈 포스터")
/// 2. 인벤토리에서 '영화 포스터'를 빈 포스터 프레임에 드롭
/// 3. 1초 대기 후 introRoot 숨김
/// 4. Gate의 completeRoot(포스터 발견 화면)가 자동 표시됨
/// 5. "포스터 완성하기" 버튼은 인스펙터에서 직접 이벤트 연결
/// </summary>
public class Director_Problem10_Step1 : Director_Problem10_Step1_Logic
{
    [Header("===== 드롭 타겟 (빈 포스터 프레임) =====")]
    [SerializeField] private RectTransform posterFrameDropTargetRect;

    [Header("===== 드롭 인디케이터 =====")]
    [SerializeField] private GameObject posterDropIndicatorRoot;

    [Header("===== 드롭 시 스케일 애니메이션 대상 =====")]
    [SerializeField] private RectTransform posterVisualRoot;

    [Header("===== 화면 루트 =====")]
    [Tooltip("초기 화면 (빈 포스터 + 조감독 대사 + 안내) - 드롭 성공 시 숨김")]
    [SerializeField] private GameObject introRoot;

    [Header("===== 완료 게이트 =====")]
    [Tooltip("completeRoot에 포스터 발견 화면을 연결하세요")]
    [SerializeField] private StepCompletionGate completionGate;

    #region 부모 추상 프로퍼티 구현

    protected override RectTransform PosterFrameDropTargetRect => posterFrameDropTargetRect;
    protected override GameObject PosterDropIndicatorRoot => posterDropIndicatorRoot;
    protected override RectTransform PosterVisualRoot => posterVisualRoot;
    protected override GameObject IntroRoot => introRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;

    #endregion
}
