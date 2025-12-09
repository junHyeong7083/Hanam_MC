using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem5 Step2: Effect Controller
/// - 장면 아이콘 글로우 펄스
/// - 모달 줌 아웃 애니메이션 (클로즈업 → 줌 아웃 전환)
/// - 모달 페이드 인/아웃
///
/// [Director 필드 → EffectController 연결]
/// - zoomModalRoot           → modalRoot + modalCanvasGroup
/// - modalCloseUpRoot        → closeUpRoot + closeUpCanvasGroup
/// - modalFullSceneRoot      → fullSceneRoot + fullSceneCanvasGroup
/// - scenes[].unrevealedRoot → 각각 GlowPulse 스크립트 붙이기 (또는 별도 관리)
/// </summary>
public class Problem5_Step2_EffectController : MonoBehaviour
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

    // 상태
    private bool _isAnimating;
    private float _elapsed;
    private AnimPhase _phase;
    private Action _onZoomOutComplete;
    private Action _onModalCloseComplete;

    // 초기값 저장
    private Vector3 _closeUpBaseScale;
    private bool _initialized;

    private enum AnimPhase
    {
        Idle,
        ModalFadeIn,
        ZoomingOut,
        FullSceneFadeIn,
        ModalFadeOut
    }

    public bool IsAnimating => _isAnimating;

    private void Awake()
    {
        SaveInitialState();
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;

        switch (_phase)
        {
            case AnimPhase.ModalFadeIn:
                ProcessModalFadeIn();
                break;
            case AnimPhase.ZoomingOut:
                ProcessZoomOut();
                break;
            case AnimPhase.FullSceneFadeIn:
                ProcessFullSceneFadeIn();
                break;
            case AnimPhase.ModalFadeOut:
                ProcessModalFadeOut();
                break;
        }
    }

    #region Public API

    /// <summary>
    /// 초기 상태 저장
    /// </summary>
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
        if (_isAnimating) return;

        SaveInitialState();

        _onZoomOutComplete = onZoomOutComplete;
        _isAnimating = true;
        _elapsed = 0f;

        // 초기 상태: 모달 표시, 클로즈업만 보임
        if (modalRoot != null)
            modalRoot.SetActive(true);

        if (modalCanvasGroup != null)
            modalCanvasGroup.alpha = 0f;

        if (closeUpRect != null)
        {
            closeUpRect.gameObject.SetActive(true);
            closeUpRect.localScale = _closeUpBaseScale * zoomStartScale;
        }

        if (closeUpCanvasGroup != null)
            closeUpCanvasGroup.alpha = 0f;

        if (fullSceneRoot != null)
            fullSceneRoot.SetActive(false);

        _phase = AnimPhase.ModalFadeIn;
    }

    /// <summary>
    /// 모달 닫기 애니메이션
    /// </summary>
    public void PlayModalClose(Action onComplete = null)
    {
        if (_isAnimating) return;

        _onModalCloseComplete = onComplete;
        _isAnimating = true;
        _elapsed = 0f;
        _phase = AnimPhase.ModalFadeOut;
    }

    /// <summary>
    /// 즉시 모달 닫기 (애니메이션 없이)
    /// </summary>
    public void CloseModalImmediate()
    {
        _isAnimating = false;
        _phase = AnimPhase.Idle;

        if (modalRoot != null)
            modalRoot.SetActive(false);

        if (closeUpRect != null)
            closeUpRect.gameObject.SetActive(false);

        if (fullSceneRoot != null)
            fullSceneRoot.SetActive(false);
    }

    /// <summary>
    /// 스텝 진입 시 리셋
    /// </summary>
    public void ResetAll()
    {
        _isAnimating = false;
        _phase = AnimPhase.Idle;
        _elapsed = 0f;

        CloseModalImmediate();

        if (closeUpRect != null && _initialized)
            closeUpRect.localScale = _closeUpBaseScale;
    }

    #endregion

    #region Animation Processing

    private void ProcessModalFadeIn()
    {
        float t = Mathf.Clamp01(_elapsed / modalFadeInDuration);

        if (modalCanvasGroup != null)
            modalCanvasGroup.alpha = t;

        if (closeUpCanvasGroup != null)
            closeUpCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            // 줌 아웃 시작
            _phase = AnimPhase.ZoomingOut;
            _elapsed = 0f;
        }
    }

    private void ProcessZoomOut()
    {
        float t = Mathf.Clamp01(_elapsed / zoomOutDuration);

        // 스케일: 1.2 → 0.55
        if (closeUpRect != null)
        {
            float scale = Mathf.Lerp(zoomStartScale, zoomEndScale, EaseInOutQuad(t));
            closeUpRect.localScale = _closeUpBaseScale * scale;
        }

        // 알파: 후반부에 약간 페이드
        if (closeUpCanvasGroup != null)
        {
            if (t > 0.7f)
            {
                float alphaT = (t - 0.7f) / 0.3f;
                closeUpCanvasGroup.alpha = Mathf.Lerp(1f, zoomEndAlpha, alphaT);
            }
        }

        if (t >= 1f)
        {
            // 클로즈업 숨기고 풀씬 페이드인
            if (closeUpRect != null)
                closeUpRect.gameObject.SetActive(false);

            if (fullSceneRoot != null)
                fullSceneRoot.SetActive(true);

            if (fullSceneCanvasGroup != null)
                fullSceneCanvasGroup.alpha = 0f;

            _phase = AnimPhase.FullSceneFadeIn;
            _elapsed = 0f;
        }
    }

    private void ProcessFullSceneFadeIn()
    {
        float t = Mathf.Clamp01(_elapsed / fullSceneFadeInDuration);

        if (fullSceneCanvasGroup != null)
            fullSceneCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            _isAnimating = false;
            _phase = AnimPhase.Idle;

            _onZoomOutComplete?.Invoke();
            _onZoomOutComplete = null;
        }
    }

    private void ProcessModalFadeOut()
    {
        float t = Mathf.Clamp01(_elapsed / modalFadeOutDuration);

        if (modalCanvasGroup != null)
            modalCanvasGroup.alpha = 1f - t;

        if (t >= 1f)
        {
            CloseModalImmediate();

            _isAnimating = false;
            _phase = AnimPhase.Idle;

            _onModalCloseComplete?.Invoke();
            _onModalCloseComplete = null;
        }
    }

    #endregion

    #region Easing

    private float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

    #endregion
}
