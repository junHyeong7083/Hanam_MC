using System.Collections;
using UnityEngine;

/// <summary>
/// 아래에서 위로 슬라이드하며 페이드인하는 애니메이션
/// - CTA 버튼, 완료 패널 등장에 사용
///
/// [사용처]
/// - Problem2 Step1: completeRoot 내부 CTA 버튼
/// - 모든 Step의 완료 버튼 등장
/// </summary>
public class SlideUpFadeIn : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float startOffsetY = 50f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private bool playOnEnable = true;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 내부 상태
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _basePosition;
    private bool _initialized;
    private Coroutine _animCoroutine;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // 첫 Enable 시 기본 위치 저장
        if (!_initialized && _rectTransform != null)
        {
            _basePosition = _rectTransform.anchoredPosition;
            _initialized = true;
        }

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }

    #region Public API

    public void Play()
    {
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = StartCoroutine(PlayRoutine());
    }

    public void ResetToStart()
    {
        if (_rectTransform != null && _initialized)
        {
            _rectTransform.anchoredPosition = _basePosition + new Vector2(0, -startOffsetY);
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    public void SetToEnd()
    {
        if (_rectTransform != null && _initialized)
        {
            _rectTransform.anchoredPosition = _basePosition;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    #endregion

    private IEnumerator PlayRoutine()
    {
        if (_rectTransform == null) yield break;

        // 시작 상태
        Vector2 startPos = _basePosition + new Vector2(0, -startOffsetY);
        Vector2 endPos = _basePosition;

        _rectTransform.anchoredPosition = startPos;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        // 딜레이
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 애니메이션
        float t = 0f;
        float safeDuration = Mathf.Max(0.001f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / safeDuration;
            float eased = easingCurve.Evaluate(Mathf.Clamp01(t));

            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

            if (_canvasGroup != null)
                _canvasGroup.alpha = eased;

            yield return null;
        }

        // 최종 상태
        _rectTransform.anchoredPosition = endPos;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        _animCoroutine = null;
    }
}
