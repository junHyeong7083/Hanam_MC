using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem5 Step1: Effect Controller
/// - 줌 렌즈 드롭 성공 시 모니터 줌 아웃 연출
/// - 클로즈업 → 줌 아웃 장면 전환
/// - 카메라 아이콘 페이드아웃 + 스케일업
/// </summary>
public class Problem5_Step1_EffectController : EffectControllerBase
{
    [Header("===== 모니터 줌 아웃 =====")]
    [SerializeField] private RectTransform monitorRect;
    [SerializeField] private CanvasGroup monitorCanvasGroup;
    [SerializeField] private float zoomOutDuration = 2f;
    [SerializeField] private float zoomOutStartScale = 1f;
    [SerializeField] private float zoomOutPeakScale = 1.05f;
    [SerializeField] private float zoomOutEndScale = 0.5f;
    [SerializeField] private float zoomOutEndAlpha = 0.3f;

    [Header("===== 장면 전환 (클로즈업 → 줌 아웃) =====")]
    [SerializeField] private GameObject closeUpRoot;
    [SerializeField] private CanvasGroup closeUpCanvasGroup;
    [SerializeField] private GameObject zoomOutRoot;
    [SerializeField] private CanvasGroup zoomOutCanvasGroup;
    [SerializeField] private float sceneTransitionDelay = 0.8f;
    [SerializeField] private float sceneFadeDuration = 0.4f;

    [Header("===== 카메라 아이콘 =====")]
    [SerializeField] private RectTransform cameraIconRect;
    [SerializeField] private CanvasGroup cameraIconCanvasGroup;
    [SerializeField] private float cameraIconDuration = 2f;
    [SerializeField] private float cameraIconStartAlpha = 0.3f;
    [SerializeField] private float cameraIconPeakAlpha = 0.8f;
    [SerializeField] private float cameraIconStartScale = 1f;
    [SerializeField] private float cameraIconEndScale = 1.5f;

    // 초기값 저장
    private Vector3 _monitorBaseScale;
    private Vector3 _cameraIconBaseScale;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }

    #region Public API

    public void SaveInitialState()
    {
        if (_initialized) return;

        if (monitorRect != null)
            _monitorBaseScale = monitorRect.localScale;

        if (cameraIconRect != null)
            _cameraIconBaseScale = cameraIconRect.localScale;

        _initialized = true;
    }

    public void PlayActivateEffect(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveInitialState();

        // 초기 상태 설정
        if (monitorRect != null)
            monitorRect.localScale = _monitorBaseScale * zoomOutStartScale;

        if (monitorCanvasGroup != null)
            monitorCanvasGroup.alpha = 1f;

        if (cameraIconRect != null)
        {
            cameraIconRect.localScale = _cameraIconBaseScale * cameraIconStartScale;
            cameraIconRect.gameObject.SetActive(true);
        }

        if (cameraIconCanvasGroup != null)
            cameraIconCanvasGroup.alpha = cameraIconStartAlpha;

        if (closeUpRoot != null) closeUpRoot.SetActive(true);
        if (closeUpCanvasGroup != null) closeUpCanvasGroup.alpha = 1f;
        if (zoomOutRoot != null) zoomOutRoot.SetActive(true);
        if (zoomOutCanvasGroup != null) zoomOutCanvasGroup.alpha = 0f;

        var seq = CreateSequence();

        // 모니터 줌 아웃: 1 → 1.05 (0.6초) → 0.5 (1.4초)
        if (monitorRect != null)
        {
            float peakTime = zoomOutDuration * 0.3f;
            float shrinkTime = zoomOutDuration * 0.7f;

            seq.Append(monitorRect.DOScale(_monitorBaseScale * zoomOutPeakScale, peakTime).SetEase(Ease.OutQuad));
            seq.Append(monitorRect.DOScale(_monitorBaseScale * zoomOutEndScale, shrinkTime).SetEase(Ease.InOutQuad));
        }

        // 모니터 알파 (후반부에 페이드)
        if (monitorCanvasGroup != null)
        {
            seq.Insert(zoomOutDuration * 0.5f, monitorCanvasGroup.DOFade(zoomOutEndAlpha, zoomOutDuration * 0.5f).SetEase(Ease.InQuad));
        }

        // 카메라 아이콘: 스케일업 + 알파 (0.3 → 0.8 → 0)
        if (cameraIconRect != null)
        {
            seq.Insert(0f, cameraIconRect.DOScale(_cameraIconBaseScale * cameraIconEndScale, cameraIconDuration).SetEase(Ease.OutQuad));
        }

        if (cameraIconCanvasGroup != null)
        {
            float peakTime = cameraIconDuration * 0.3f;
            float fadeTime = cameraIconDuration * 0.7f;

            seq.Insert(0f, cameraIconCanvasGroup.DOFade(cameraIconPeakAlpha, peakTime).SetEase(Ease.OutQuad));
            seq.Insert(peakTime, cameraIconCanvasGroup.DOFade(0f, fadeTime).SetEase(Ease.InQuad));
        }

        // 장면 전환: 딜레이 후 클로즈업 페이드아웃 + 줌아웃 페이드인
        if (closeUpCanvasGroup != null)
            seq.Insert(sceneTransitionDelay, closeUpCanvasGroup.DOFade(0f, sceneFadeDuration));

        if (zoomOutCanvasGroup != null)
            seq.Insert(sceneTransitionDelay, zoomOutCanvasGroup.DOFade(1f, sceneFadeDuration));

        seq.InsertCallback(sceneTransitionDelay + sceneFadeDuration, () =>
        {
            if (closeUpRoot != null) closeUpRoot.SetActive(false);
        });

        // 완료 처리
        seq.OnComplete(() =>
        {
            if (cameraIconRect != null) cameraIconRect.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void ResetToInitial()
    {
        KillCurrentSequence();

        if (monitorRect != null && _initialized)
            monitorRect.localScale = _monitorBaseScale;

        if (monitorCanvasGroup != null)
            monitorCanvasGroup.alpha = 1f;

        if (cameraIconRect != null && _initialized)
        {
            cameraIconRect.localScale = _cameraIconBaseScale;
            cameraIconRect.gameObject.SetActive(true);
        }

        if (cameraIconCanvasGroup != null)
            cameraIconCanvasGroup.alpha = cameraIconStartAlpha;

        if (closeUpRoot != null) closeUpRoot.SetActive(true);
        if (closeUpCanvasGroup != null) closeUpCanvasGroup.alpha = 1f;
        if (zoomOutRoot != null) zoomOutRoot.SetActive(false);
    }

    #endregion
}
