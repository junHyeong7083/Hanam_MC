using UnityEngine;

/// <summary>
/// 범용 펄스(맥박) 애니메이션
/// - Scale 또는 Alpha를 반복적으로 변화시킴
/// - NG 뱃지, 감정 조명 글로우 등에 사용
///
/// [사용처]
/// - Problem2 Step1: NG 뱃지, 감정 조명 글로우
/// - Problem2 Step2: lightGlowImage
/// - 기타 강조가 필요한 UI 요소
/// </summary>
public class PulseAnimation : MonoBehaviour
{
    public enum PulseType
    {
        Scale,      // localScale 변화
        Alpha,      // CanvasGroup alpha 변화
        Both        // Scale + Alpha 동시
    }

    [Header("===== 펄스 설정 =====")]
    [SerializeField] private PulseType pulseType = PulseType.Scale;

    [Header("Scale 설정")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 1.3f;

    [Header("Alpha 설정")]
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("타이밍")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private int loopCount = -1; // -1 = 무한, 0 이상 = 해당 횟수만큼

    [Header("Easing")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 내부 상태
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector3 _baseScale;
    private float _baseAlpha;
    private bool _isPlaying;
    private float _time;
    private int _currentLoopCount;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();

        // 기본값 저장
        if (_rectTransform != null)
            _baseScale = _rectTransform.localScale;

        if (_canvasGroup != null)
            _baseAlpha = _canvasGroup.alpha;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _time += Time.deltaTime;
        float normalizedTime = (_time % duration) / duration;

        // 0 → 1 → 0 사이클 (sin 곡선)
        float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;
        float easedWave = easingCurve.Evaluate(wave);

        ApplyPulse(easedWave);

        // 루프 체크
        if (!loop && _time >= duration)
        {
            _currentLoopCount++;
            if (loopCount >= 0 && _currentLoopCount >= loopCount)
            {
                Stop();
            }
        }
    }

    private void ApplyPulse(float t)
    {
        if (pulseType == PulseType.Scale || pulseType == PulseType.Both)
        {
            if (_rectTransform != null)
            {
                float scale = Mathf.Lerp(minScale, maxScale, t);
                _rectTransform.localScale = _baseScale * scale;
            }
        }

        if (pulseType == PulseType.Alpha || pulseType == PulseType.Both)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            }
        }
    }

    #region Public API

    public void Play()
    {
        _isPlaying = true;
        _time = 0f;
        _currentLoopCount = 0;
    }

    public void Stop()
    {
        _isPlaying = false;

        // 원래 상태로 복원
        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale;

        if (_canvasGroup != null)
            _canvasGroup.alpha = _baseAlpha;
    }

    /// <summary>
    /// 특정 횟수만큼 펄스 재생 (NG 뱃지처럼 3번만 펄스)
    /// </summary>
    public void PlayTimes(int times)
    {
        loop = false;
        loopCount = times;
        Play();
    }

    #endregion
}
