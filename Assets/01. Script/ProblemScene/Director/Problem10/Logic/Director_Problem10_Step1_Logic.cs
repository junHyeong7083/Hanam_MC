using UnityEngine;

/// <summary>
/// Director / Problem10 / Step1 로직 베이스
/// - 빈 포스터 프레임에 '영화 포스터' 아이템을 드롭
/// - 드롭 성공 시 1초 대기 후 Gate 완료 → completeRoot(포스터 발견 화면)가 자동 표시됨
/// - "포스터 완성하기" 버튼은 인스펙터에서 직접 이벤트 연결
/// </summary>
public abstract class Director_Problem10_Step1_Logic : InventoryDropTargetStepBase
{
    #region Abstract Properties (파생 클래스에서 구현)

    [Header("===== 드롭 타겟 (빈 포스터 프레임) =====")]
    protected abstract RectTransform PosterFrameDropTargetRect { get; }

    [Header("===== 드롭 인디케이터 =====")]
    protected abstract GameObject PosterDropIndicatorRoot { get; }

    [Header("===== 드롭 시 스케일 애니메이션 대상 =====")]
    protected abstract RectTransform PosterVisualRoot { get; }

    [Header("===== 화면 루트 =====")]
    /// <summary>초기 화면 (빈 포스터 + 조감독 대사 + 안내) - 드롭 성공 시 숨김</summary>
    protected abstract GameObject IntroRoot { get; }

    [Header("===== 완료 게이트 =====")]
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    #endregion

    #region InventoryDropTargetStepBase 구현

    protected override RectTransform DropTargetRect => PosterFrameDropTargetRect;
    protected override GameObject DropIndicatorRoot => PosterDropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => PosterVisualRoot;
    protected override GameObject InstructionRoot => null;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    #endregion

    #region Virtual Config

    protected override float DropRadius => 150f;
    protected override float ActivateScale => 1.1f;
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 1.0f; // 드롭 후 1초 대기

    #endregion

    #region Step Lifecycle

    protected override void OnStepEnterExtra()
    {
        // 초기 화면 표시
        if (IntroRoot != null) IntroRoot.SetActive(true);
    }

    #endregion

    #region Drop Handling (Override)

    /// <summary>
    /// 드롭 성공 시: introRoot 숨기고 Gate 완료
    /// </summary>
    protected override void OnDropComplete()
    {
        // introRoot 숨기기
        if (IntroRoot != null) IntroRoot.SetActive(false);

        // DB 저장
        SaveAttempt(new
        {
            action = "poster_dropped",
            targetItem = "poster_frame"
        });
    }

    #endregion
}
