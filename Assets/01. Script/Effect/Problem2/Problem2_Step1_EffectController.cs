using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem2 Step1: Effect Controller
/// - 인트로 슬라이드 애니메이션 (좌/우에서 진입)
/// - 드래그&드롭 완료 시 이펙트
/// </summary>
public class Problem2_Step1_EffectController : EffectControllerBase
{
    [Header("===== 좌측 진입 루트 =====")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private float leftStartOffsetX = -300f;

    [Header("===== 우측 진입 루트 =====")]
    [SerializeField] private RectTransform rightEnterRoot;
    [SerializeField] private float rightStartOffsetX = 300f;

    [Header("===== 인트로 타이밍 =====")]
    [SerializeField] private float introDelay = 0f;
    [SerializeField] private float introDuration = 0.6f;
    [SerializeField] private Ease introEase = Ease.OutQuad;

    [Header("===== 드롭 완료 이펙트 =====")]
    [SerializeField] private RectTransform dropTargetRect;
    [SerializeField] private float dropScalePunch = 0.1f;
    [SerializeField] private float dropScaleDuration = 0.3f;

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
