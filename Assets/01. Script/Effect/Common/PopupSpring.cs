using UnityEngine;

/// <summary>
/// 팝업 스프링 애니메이션
/// - scale 0에서 튀어나오는 효과
/// - 오버슈트 + 바운스
///
/// [사용처]
/// - Problem2 Step2: 감정 라벨 팝업
/// - 정답 표시, 보상 등장
/// </summary>
public class PopupSpring : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float overshoot = 1.2f;  // 최대 스케일 (오버슈트)

    [Header("타이밍")]
    [SerializeField] private float delay = 0f;

    // 내부
    private Vector3 _targetScale;
    private float _elapsedTime;
    private bool _isAnimating;

    private void Awake()
    {
        _targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        _elapsedTime = -delay;
        _isAnimating = true;
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime < 0) return;

        float t = Mathf.Clamp01(_elapsedTime / duration);

        // 스프링 커브: 오버슈트 후 정착
        float scale = SpringEase(t, overshoot);
        transform.localScale = _targetScale * scale;

        if (t >= 1f)
        {
            transform.localScale = _targetScale;
            _isAnimating = false;
        }
    }

    /// <summary>
    /// 스프링 이징 (오버슈트 + 감쇠)
    /// </summary>
    private float SpringEase(float t, float overshoot)
    {
        // 간단한 스프링: sin 기반 감쇠 진동
        float decay = 1f - t;
        float oscillation = Mathf.Sin(t * Mathf.PI * 2f) * decay * (overshoot - 1f);
        return t + oscillation * (1f - t);
    }

    /// <summary>
    /// 외부에서 재생
    /// </summary>
    public void Play()
    {
        OnEnable();
    }

    /// <summary>
    /// 딜레이 설정
    /// </summary>
    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }
}
