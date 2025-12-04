using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오브젝트 등장 애니메이션
/// - 아래에서 위로 슬라이드 + 페이드인 + 스케일
/// - 순차 등장을 위한 딜레이 지원
///
/// [사용처]
/// - Problem2 Step2: 필름 카드 순차 등장
/// - 버튼, 카드, UI 요소 등장
/// </summary>
public class AppearAnimation : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float delay = 0f;
    [SerializeField] private float duration = 0.4f;

    [Header("위치")]
    [SerializeField] private bool enableSlide = true;
    [SerializeField] private float slideDistance = 50f;  // 아래에서 올라오는 거리

    [Header("페이드")]
    [SerializeField] private bool enableFade = true;

    [Header("스케일")]
    [SerializeField] private bool enableScale = false;
    [SerializeField] private float startScale = 0.8f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 내부
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _targetPosition;
    private float _elapsedTime;
    private bool _isAnimating;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        // CanvasGroup 자동 추가 (페이드용)
        if (enableFade)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        // 시작 상태 설정
        _targetPosition = _rectTransform.anchoredPosition;

        if (enableSlide)
            _rectTransform.anchoredPosition = _targetPosition + Vector2.down * slideDistance;

        if (enableFade && _canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (enableScale)
            _rectTransform.localScale = Vector3.one * startScale;

        _elapsedTime = -delay;  // 딜레이 처리
        _isAnimating = true;
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsedTime += Time.deltaTime;

        // 딜레이 중
        if (_elapsedTime < 0) return;

        float t = Mathf.Clamp01(_elapsedTime / duration);
        float eased = easeCurve.Evaluate(t);

        // 위치 애니메이션
        if (enableSlide)
        {
            Vector2 startPos = _targetPosition + Vector2.down * slideDistance;
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _targetPosition, eased);
        }

        // 페이드 애니메이션
        if (enableFade && _canvasGroup != null)
        {
            _canvasGroup.alpha = eased;
        }

        // 스케일 애니메이션
        if (enableScale)
        {
            float scale = Mathf.Lerp(startScale, 1f, eased);
            _rectTransform.localScale = Vector3.one * scale;
        }

        // 완료
        if (t >= 1f)
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// 외부에서 딜레이 설정 (순차 등장용)
    /// </summary>
    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }

    /// <summary>
    /// 애니메이션 재시작
    /// </summary>
    public void Replay()
    {
        OnEnable();
    }
}
