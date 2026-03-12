using UnityEngine;

/// <summary>
/// Director_Problem6_Step1_Logic - 문제6 스텝1 인벤토리 드롭 로직 (추상 클래스)
///
/// 【역할】 인벤토리에서 "휴식용 의자" 아이템을 드롭 박스에 떨어뜨려 활성화하는 스텝.
///          의자가 배치되면 글로우/스파클 이펙트를 표시하고 스텝 완료 처리한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. 실제 SerializeField는 Binder(Director_Problem6_Step1)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제6 > 스텝1 (인트로/도입)
/// 【부모 클래스】 InventoryDropTargetStepBase → ProblemStepBase
/// 【참조하는 곳】 Director_Problem6_Step1 (Binder, concrete 클래스)
/// 【참조되는 곳】 InventoryDropTargetStepBase (드롭 감지/스케일 애니메이션 등 공통 로직)
/// 【흐름】 스텝 진입 → 의자 아이콘/이펙트 숨김 → 사용자가 인벤토리에서 의자를 드래그&드롭
///         → 스케일 애니메이션 재생 → 의자 아이콘 + 글로우 + 스파클 표시 → 완료 게이트 열림
/// </summary>
public abstract class Director_Problem6_Step1_Logic : InventoryDropTargetStepBase
{
    // ===== 파생 클래스(Binder)에서 넘겨줄 UI 프로퍼티 =====

    /// <summary>의자가 배치될 드롭 영역의 RectTransform (스케일 애니메이션 대상)</summary>
    protected abstract RectTransform ChairTargetVisualRoot { get; }

    /// <summary>드롭 안내 UI 루트 (안내 문구 등)</summary>
    protected abstract GameObject InstructionRootObject { get; }

    /// <summary>스텝 완료 판정용 게이트</summary>
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    /// <summary>의자가 배치된 후 표시할 아이콘 루트 오브젝트</summary>
    protected abstract GameObject ChairPlacedIconRoot { get; }

    /// <summary>의자 배치 완료 시 표시할 글로우 이펙트 이미지</summary>
    protected abstract GameObject GlowImage { get; }

    /// <summary>의자 배치 완료 시 표시할 스파클(반짝임) 이펙트 이미지</summary>
    protected abstract GameObject SparkleImage { get; }

    // ===== InventoryDropTargetStepBase 추상 속성 연결 =====

    /// <summary>부모의 TargetVisualRoot를 의자 드롭 영역으로 연결</summary>
    protected override RectTransform TargetVisualRoot => ChairTargetVisualRoot;

    /// <summary>부모의 InstructionRoot를 안내 UI 루트로 연결</summary>
    protected override GameObject InstructionRoot => InstructionRootObject;

    /// <summary>부모의 CompletionGate를 완료 게이트로 연결</summary>
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    /// <summary>드롭 활성화 시 스케일 목표값 (1.02배 = 살짝 확대)</summary>
    protected override float ActivateScale => 1.02f;

    /// <summary>스케일 애니메이션 재생 시간 (0.5초)</summary>
    protected override float ActivateDuration => 0.5f;

    /// <summary>활성화 완료 후 게이트 완료까지 대기 시간 (2.5초)</summary>
    protected override float DelayBeforeComplete => 2.5f;

    /// <summary>
    /// 스텝 진입 시 추가 초기화.
    /// 의자 아이콘과 이펙트들을 모두 숨긴 상태로 시작한다.
    /// </summary>
    protected override void OnStepEnterExtra()
    {
        // 의자 배치 전이므로 관련 비주얼을 모두 비활성화
        if (ChairPlacedIconRoot != null)
            ChairPlacedIconRoot.SetActive(false);
        if (GlowImage != null)
            GlowImage.SetActive(false);
        if (SparkleImage != null)
            SparkleImage.SetActive(false);
    }

    /// <summary>
    /// 드롭 활성화 애니메이션이 완료된 후 호출.
    /// 의자 아이콘과 글로우/스파클 이펙트를 표시한다.
    /// </summary>
    protected override void OnActivateComplete()
    {
        // 의자 배치 완료 → 아이콘 + 이펙트 모두 활성화
        if (ChairPlacedIconRoot != null)
            ChairPlacedIconRoot.SetActive(true);
        if (GlowImage != null)
            GlowImage.SetActive(true);
        if (SparkleImage != null)
            SparkleImage.SetActive(true);
    }
}
