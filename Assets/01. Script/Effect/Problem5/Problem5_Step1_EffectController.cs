using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem5 Step1: Effect Controller
/// - 줌 렌즈 드롭 성공 시 모니터 줌 아웃 연출
/// - 클로즈업 → 줌 아웃 장면 전환
/// - 카메라 아이콘 페이드아웃 + 스케일업
///
/// [사용법]
/// - Director_Problem5_Step1에서 드롭 성공 시 PlayActivateEffect() 호출
/// - 이펙트 컨트롤러에 연결할 필드:
///   - monitorRect: Director의 targetVisualRoot (모니터 전체)
///   - closeUpRoot: Director의 closeUpRoot
///   - zoomOutRoot: Director의 zoomOutRoot
///   - cameraIcon: 모니터 위 카메라 아이콘 (선택)
/// </summary>
public class Problem5_Step1_EffectController : MonoBehaviour
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
    [SerializeField] private float sceneTransitionDelay = 0.8f;  // 줌 아웃 시작 후 장면 전환 타이밍
    [SerializeField] private float sceneFadeDuration = 0.4f;

    [Header("===== 카메라 아이콘 =====")]
    [SerializeField] private RectTransform cameraIconRect;
    [SerializeField] private CanvasGroup cameraIconCanvasGroup;
    [SerializeField] private float cameraIconDuration = 2f;
    [SerializeField] private float cameraIconStartAlpha = 0.3f;
    [SerializeField] private float cameraIconPeakAlpha = 0.8f;
    [SerializeField] private float cameraIconStartScale = 1f;
    [SerializeField] private float cameraIconEndScale = 1.5f;

    // 상태
    private bool _isAnimating;
    private float _elapsed;
    private Action _onCompleteCallback;

    // 초기값 저장
    private Vector3 _monitorBaseScale;
    private Vector3 _cameraIconBaseScale;
    private bool _initialized;

    public bool IsAnimating => _isAnimating;

    private void Awake()
    {
        SaveInitialState();
    }

    #region Public API

    /// <summary>
    /// 초기 상태 저장
    /// </summary>
    public void SaveInitialState()
    {
        if (_initialized) return;

        if (monitorRect != null)
            _monitorBaseScale = monitorRect.localScale;

        if (cameraIconRect != null)
            _cameraIconBaseScale = cameraIconRect.localScale;

        _initialized = true;
    }

    /// <summary>
    /// 활성화 이펙트 재생 (드롭 성공 시 호출)
    /// </summary>
    public void PlayActivateEffect(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveInitialState();

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

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

        // 클로즈업 보이고 줌 아웃 숨김
        if (closeUpRoot != null)
            closeUpRoot.SetActive(true);
        if (closeUpCanvasGroup != null)
            closeUpCanvasGroup.alpha = 1f;

        if (zoomOutRoot != null)
            zoomOutRoot.SetActive(true);
        if (zoomOutCanvasGroup != null)
            zoomOutCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 초기 상태로 리셋 (스텝 진입 시)
    /// </summary>
    public void ResetToInitial()
    {
        _isAnimating = false;
        _elapsed = 0f;

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

        // 클로즈업 보이고 줌 아웃 숨김
        if (closeUpRoot != null)
            closeUpRoot.SetActive(true);
        if (closeUpCanvasGroup != null)
            closeUpCanvasGroup.alpha = 1f;

        if (zoomOutRoot != null)
            zoomOutRoot.SetActive(false);
    }

    #endregion

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;

        ProcessMonitorZoomOut();
        ProcessCameraIcon();
        ProcessSceneTransition();

        // 완료 체크 (모니터 줌 아웃 기준)
        if (_elapsed >= zoomOutDuration)
        {
            CompleteAnimation();
        }
    }

    #region Animation Processing

    /// <summary>
    /// 모니터 줌 아웃 (scale: 1 → 1.05 → 0.5, alpha: 1 → 1 → 0.3)
    /// </summary>
    private void ProcessMonitorZoomOut()
    {
        if (monitorRect == null) return;

        float t = Mathf.Clamp01(_elapsed / zoomOutDuration);

        // 스케일: 처음엔 살짝 커졌다가 → 작아짐
        float scale;
        if (t < 0.3f)
        {
            // 0~0.3: 1 → 1.05
            float localT = t / 0.3f;
            scale = Mathf.Lerp(zoomOutStartScale, zoomOutPeakScale, EaseOutQuad(localT));
        }
        else
        {
            // 0.3~1: 1.05 → 0.5
            float localT = (t - 0.3f) / 0.7f;
            scale = Mathf.Lerp(zoomOutPeakScale, zoomOutEndScale, EaseInOutQuad(localT));
        }

        monitorRect.localScale = _monitorBaseScale * scale;

        // 알파: 후반부에 페이드
        if (monitorCanvasGroup != null)
        {
            if (t < 0.5f)
            {
                monitorCanvasGroup.alpha = 1f;
            }
            else
            {
                float alphaT = (t - 0.5f) / 0.5f;
                monitorCanvasGroup.alpha = Mathf.Lerp(1f, zoomOutEndAlpha, EaseInQuad(alphaT));
            }
        }
    }

    /// <summary>
    /// 카메라 아이콘 (alpha: 0.3 → 0.8 → 0, scale: 1 → 1.5)
    /// </summary>
    private void ProcessCameraIcon()
    {
        if (cameraIconRect == null) return;

        float t = Mathf.Clamp01(_elapsed / cameraIconDuration);

        // 스케일: 점점 커짐
        float scale = Mathf.Lerp(cameraIconStartScale, cameraIconEndScale, EaseOutQuad(t));
        cameraIconRect.localScale = _cameraIconBaseScale * scale;

        // 알파: 밝아졌다가 사라짐
        if (cameraIconCanvasGroup != null)
        {
            float alpha;
            if (t < 0.3f)
            {
                // 0~0.3: 0.3 → 0.8
                float localT = t / 0.3f;
                alpha = Mathf.Lerp(cameraIconStartAlpha, cameraIconPeakAlpha, EaseOutQuad(localT));
            }
            else
            {
                // 0.3~1: 0.8 → 0
                float localT = (t - 0.3f) / 0.7f;
                alpha = Mathf.Lerp(cameraIconPeakAlpha, 0f, EaseInQuad(localT));
            }

            cameraIconCanvasGroup.alpha = alpha;
        }
    }

    /// <summary>
    /// 장면 전환 (클로즈업 → 줌 아웃)
    /// </summary>
    private void ProcessSceneTransition()
    {
        if (_elapsed < sceneTransitionDelay) return;

        float transitionElapsed = _elapsed - sceneTransitionDelay;
        float t = Mathf.Clamp01(transitionElapsed / sceneFadeDuration);

        // 클로즈업 페이드 아웃
        if (closeUpCanvasGroup != null)
            closeUpCanvasGroup.alpha = 1f - t;

        // 줌 아웃 페이드 인
        if (zoomOutCanvasGroup != null)
            zoomOutCanvasGroup.alpha = t;

        // 전환 완료 시 클로즈업 비활성화
        if (t >= 1f && closeUpRoot != null)
            closeUpRoot.SetActive(false);
    }

    private void CompleteAnimation()
    {
        _isAnimating = false;

        // 카메라 아이콘 숨김
        if (cameraIconRect != null)
            cameraIconRect.gameObject.SetActive(false);

        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }

    #endregion

    #region Easing

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private float EaseInQuad(float t) => t * t;
    private float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

    #endregion
}
