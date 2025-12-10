using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem5 Step2: Effect Controller
/// - 장면 아이콘 글로우 펄스
/// - 모달 줌 아웃 애니메이션 (클로즈업 → 줌 아웃 전환)
/// - 모달 페이드 인/아웃
/// </summary>
public class Problem5_Step2_EffectController : EffectControllerBase
{
    [Header("===== 모달 전체 =====")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private CanvasGroup modalCanvasGroup;
    [SerializeField] private float modalFadeInDuration = 0.3f;
    [SerializeField] private float modalFadeOutDuration = 0.2f;

    [Header("===== 클로즈업 (줌 아웃 애니메이션) =====")]
    [SerializeField] private RectTransform closeUpRect;
    [SerializeField] private CanvasGroup closeUpCanvasGroup;
    [SerializeField] private float zoomOutDuration = 1.5f;
    [SerializeField] private float zoomStartScale = 1.2f;
    [SerializeField] private float zoomEndScale = 0.55f;
    [SerializeField] private float zoomEndAlpha = 0.9f;

    [Header("===== 풀씬 (줌 아웃 후 표시) =====")]
    [SerializeField] private GameObject fullSceneRoot;
    [SerializeField] private CanvasGroup fullSceneCanvasGroup;
    [SerializeField] private float fullSceneFadeInDuration = 0.4f;

    // 초기값 저장
    private Vector3 _closeUpBaseScale;
    private bool _initialized;

    // 모달 닫기용 별도 시퀀스
    private Sequence _closeSequence;

    private void Awake()
    {
        SaveInitialState();
    }

    #region Public API

    public void SaveInitialState()
    {
        if (_initialized) return;

        if (closeUpRect != null)
            _closeUpBaseScale = closeUpRect.localScale;

        _initialized = true;
    }

    /// <summary>
    /// 모달 열기 + 줌 아웃 애니메이션 시작
    /// </summary>
    public void PlayZoomOutSequence(Action onZoomOutComplete = null)
    {
        if (IsAnimating) return;

        SaveInitialState();

        // 초기 상태: 모달 표시, 클로즈업만 보임
        if (modalRoot != null) modalRoot.SetActive(true);
        if (modalCanvasGroup != null) modalCanvasGroup.alpha = 0f;

        if (closeUpRect != null)
        {
            closeUpRect.gameObject.SetActive(true);
            closeUpRect.localScale = _closeUpBaseScale * zoomStartScale;
        }

        if (closeUpCanvasGroup != null) closeUpCanvasGroup.alpha = 0f;
        if (fullSceneRoot != null) fullSceneRoot.SetActive(false);

        var seq = CreateSequence();

        // 1. 모달 페이드인
        if (modalCanvasGroup != null)
            seq.Append(modalCanvasGroup.DOFade(1f, modalFadeInDuration));

        if (closeUpCanvasGroup != null)
            seq.Join(closeUpCanvasGroup.DOFade(1f, modalFadeInDuration));

        // 2. 줌 아웃: scale 1.2 → 0.55
        if (closeUpRect != null)
            seq.Append(closeUpRect.DOScale(_closeUpBaseScale * zoomEndScale, zoomOutDuration).SetEase(Ease.InOutQuad));

        // 후반부에 알파 약간 감소
        if (closeUpCanvasGroup != null)
            seq.Insert(modalFadeInDuration + zoomOutDuration * 0.7f, closeUpCanvasGroup.DOFade(zoomEndAlpha, zoomOutDuration * 0.3f));

        // 3. 클로즈업 숨기고 풀씬 표시
        seq.AppendCallback(() =>
        {
            if (closeUpRect != null) closeUpRect.gameObject.SetActive(false);
            if (fullSceneRoot != null) fullSceneRoot.SetActive(true);
            if (fullSceneCanvasGroup != null) fullSceneCanvasGroup.alpha = 0f;
        });

        // 4. 풀씬 페이드인
        if (fullSceneCanvasGroup != null)
            seq.Append(fullSceneCanvasGroup.DOFade(1f, fullSceneFadeInDuration));

        seq.OnComplete(() => onZoomOutComplete?.Invoke());
    }

    /// <summary>
    /// 모달 닫기 애니메이션
    /// </summary>
    public void PlayModalClose(Action onComplete = null)
    {
        _closeSequence?.Kill();

        _closeSequence = DOTween.Sequence();

        if (modalCanvasGroup != null)
            _closeSequence.Append(modalCanvasGroup.DOFade(0f, modalFadeOutDuration));

        _closeSequence.OnComplete(() =>
        {
            CloseModalImmediate();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 즉시 모달 닫기
    /// </summary>
    public void CloseModalImmediate()
    {
        KillCurrentSequence();
        _closeSequence?.Kill();
        _closeSequence = null;

        if (modalRoot != null) modalRoot.SetActive(false);
        if (closeUpRect != null) closeUpRect.gameObject.SetActive(false);
        if (fullSceneRoot != null) fullSceneRoot.SetActive(false);
    }

    /// <summary>
    /// 스텝 진입 시 리셋
    /// </summary>
    public void ResetAll()
    {
        CloseModalImmediate();

        if (closeUpRect != null && _initialized)
            closeUpRect.localScale = _closeUpBaseScale;
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        _closeSequence?.Kill();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _closeSequence?.Kill();
    }
}
