using UnityEngine;

/// <summary>
/// Director_Problem10_Step1_Logic - 문제10 스텝1 포스터 프레임 드롭 로직 (추상 클래스)
///
/// 【역할】 인벤토리에서 영화 포스터를 드롭 박스에 드롭하여 포스터 프레임을 활성화하는 스텝.
///          활성화 완료 시 인트로 루트를 숨기고 DB에 포스터 드롭 이벤트를 저장한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층.
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝1 (인트로/도입 - 포스터 드롭)
/// 【부모 클래스】 InventoryDropTargetStepBase → ProblemStepBase
/// 【참조하는 곳】 Director_Problem10_Step1 (Binder)
/// 【흐름】 스텝 진입 → 인트로 루트 표시 → 인벤토리에서 포스터 드래그&드롭
///         → 스케일 애니메이션 → 인트로 숨김 + DB 저장 → 완료 게이트 열림
/// </summary>
public abstract class Director_Problem10_Step1_Logic : InventoryDropTargetStepBase
{
    /// <summary>포스터 프레임의 RectTransform (스케일 애니메이션 대상)</summary>
    protected abstract RectTransform PosterVisualRoot { get; }
    /// <summary>인트로 안내 화면 루트 (드롭 완료 시 숨김)</summary>
    protected abstract GameObject IntroRoot { get; }
    /// <summary>스텝 완료 게이트</summary>
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    // ===== InventoryDropTargetStepBase 추상 속성 연결 =====
    /// <summary>부모의 TargetVisualRoot를 포스터 영역으로 연결</summary>
    protected override RectTransform TargetVisualRoot => PosterVisualRoot;
    /// <summary>이 스텝에는 별도 안내 루트가 없으므로 null</summary>
    protected override GameObject InstructionRoot => null;
    /// <summary>부모의 CompletionGate를 완료 게이트로 연결</summary>
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    /// <summary>드롭 활성화 시 스케일 목표값 (1.1배)</summary>
    protected override float ActivateScale => 1.1f;
    /// <summary>스케일 애니메이션 재생 시간 (0.5초)</summary>
    protected override float ActivateDuration => 0.5f;
    /// <summary>활성화 완료 후 게이트 완료까지 대기 시간 (1초)</summary>
    protected override float DelayBeforeComplete => 1.0f;

    /// <summary>스텝 진입 시 인트로 루트를 표시한다.</summary>
    protected override void OnStepEnterExtra()
    {
        if (IntroRoot != null) IntroRoot.SetActive(true);
    }

    /// <summary>
    /// 드롭 활성화 완료 후 호출. 인트로 루트를 숨기고 포스터 드롭 이벤트를 DB에 저장한다.
    /// </summary>
    protected override void OnActivateComplete()
    {
        // 인트로 화면 숨김 → 포스터 프레임만 남김
        if (IntroRoot != null) IntroRoot.SetActive(false);

        // DB에 포스터 드롭 이벤트 저장
        SaveAttempt(new
        {
            action = "poster_dropped",
            targetItem = "poster_frame"
        });
    }
}
