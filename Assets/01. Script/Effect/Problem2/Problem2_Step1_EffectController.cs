using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem2_Step1_EffectController - 문제2 스텝1(마음 렌즈 드래그)의 시각 효과 관리자
///
/// 【역할】 좌측/우측에서 슬라이드+페이드인으로 진입하는 인트로 애니메이션과,
///          드래그&드롭 성공 시 드롭 타겟의 스케일 펀치 이펙트를 관리한다.
/// 【사용 위치】 ProblemScene - Problem2 Step1 (마음 렌즈를 필름 위로 드래그하는 스텝)
/// 【트리거】 Logic 클래스에서 PlayIntroAnimation(), PlayDropSuccessEffect() 호출
/// 【의존성】 EffectControllerBase(상속), DOTween, leftEnterRoot/rightEnterRoot(RectTransform)
/// </summary>
public class Problem2_Step1_EffectController : EffectControllerBase
{
    [Header("===== 좌측 진입 루트 =====")]
    [SerializeField] private RectTransform leftEnterRoot;      // 좌측에서 슬라이드인할 UI 루트
    [SerializeField] private float leftStartOffsetX = -300f;   // 좌측 시작 오프셋 (음수=왼쪽)

    [Header("===== 우측 진입 루트 =====")]
    [SerializeField] private RectTransform rightEnterRoot;     // 우측에서 슬라이드인할 UI 루트
    [SerializeField] private float rightStartOffsetX = 300f;   // 우측 시작 오프셋 (양수=오른쪽)

    [Header("===== 인트로 타이밍 =====")]
    [SerializeField] private float introDelay = 0f;            // 인트로 시작 전 대기 시간
    [SerializeField] private float introDuration = 0.6f;       // 슬라이드 애니메이션 소요 시간
    [SerializeField] private Ease introEase = Ease.OutQuad;    // 이징 타입

    [Header("===== 드롭 완료 이펙트 =====")]
    [SerializeField] private RectTransform dropTargetRect;     // 드롭 성공 시 펀치 효과를 줄 대상
    [SerializeField] private float dropScalePunch = 0.1f;      // 스케일 펀치 강도
    [SerializeField] private float dropScaleDuration = 0.3f;   // 스케일 펀치 소요 시간

    // 초기 위치 저장
    private Vector2 _leftBasePos;
    private Vector2 _rightBasePos;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialPositions();
    }

    #region Public API

    /// <summary>
    /// 초기 위치 저장
    /// </summary>
    public void SaveInitialPositions()
    {
        if (_initialized) return;

        if (leftEnterRoot != null)
            _leftBasePos = leftEnterRoot.anchoredPosition;

        if (rightEnterRoot != null)
            _rightBasePos = rightEnterRoot.anchoredPosition;

        _initialized = true;
    }

    /// <summary>
    /// 인트로 애니메이션 재생 (좌/우 슬라이드 + 페이드인)
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        SaveInitialPositions();

        var seq = CreateSequence();

        // 초기 상태 설정
        if (leftEnterRoot != null)
        {
            leftEnterRoot.anchoredPosition = _leftBasePos + new Vector2(leftStartOffsetX, 0f);
            var leftCg = GetOrAddCanvasGroup(leftEnterRoot.gameObject);
            if (leftCg != null) leftCg.alpha = 0f;
        }

        if (rightEnterRoot != null)
        {
            rightEnterRoot.anchoredPosition = _rightBasePos + new Vector2(rightStartOffsetX, 0f);
            var rightCg = GetOrAddCanvasGroup(rightEnterRoot.gameObject);
            if (rightCg != null) rightCg.alpha = 0f;
        }

        // 딜레이
        if (introDelay > 0f)
            seq.AppendInterval(introDelay);

        // 좌측 애니메이션
        if (leftEnterRoot != null)
        {
            var leftCg = GetOrAddCanvasGroup(leftEnterRoot.gameObject);

            seq.Join(leftEnterRoot.DOAnchorPos(_leftBasePos, introDuration).SetEase(introEase));
            if (leftCg != null)
                seq.Join(leftCg.DOFade(1f, introDuration).SetEase(introEase));
        }

        // 우측 애니메이션 (동시)
        if (rightEnterRoot != null)
        {
            var rightCg = GetOrAddCanvasGroup(rightEnterRoot.gameObject);

            seq.Join(rightEnterRoot.DOAnchorPos(_rightBasePos, introDuration).SetEase(introEase));
            if (rightCg != null)
                seq.Join(rightCg.DOFade(1f, introDuration).SetEase(introEase));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 드롭 성공 시 스케일 펀치 이펙트
    /// </summary>
    public void PlayDropSuccessEffect(Action onComplete = null)
    {
        if (dropTargetRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();
        seq.Append(dropTargetRect.DOPunchScale(Vector3.one * dropScalePunch, dropScaleDuration, 1, 0.5f));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 초기 상태로 리셋
    /// </summary>
    public void ResetToInitial()
    {
        KillCurrentSequence();

        if (leftEnterRoot != null && _initialized)
        {
            leftEnterRoot.anchoredPosition = _leftBasePos;
            var leftCg = GetOrAddCanvasGroup(leftEnterRoot.gameObject);
            if (leftCg != null) leftCg.alpha = 1f;
        }

        if (rightEnterRoot != null && _initialized)
        {
            rightEnterRoot.anchoredPosition = _rightBasePos;
            var rightCg = GetOrAddCanvasGroup(rightEnterRoot.gameObject);
            if (rightCg != null) rightCg.alpha = 1f;
        }
    }

    #endregion
}
