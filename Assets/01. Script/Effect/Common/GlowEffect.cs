using UnityEngine;
using DG.Tweening;

/// <summary>
/// GlowEffect - DOTween 기반 범용 글로우(발광) 이펙트 컴포넌트
///
/// 【역할】 스케일 펄스(X/Y/XY 축 선택)와 알파 페이드(min↔max)를 조합하여
///          오브젝트에 반복적인 발광/강조 효과를 적용한다. 무한 반복 루프.
/// 【사용 위치】 아이콘 강조, 버튼 하이라이트, 걱정등불, 아이템 글로우 등 범용
/// 【트리거】 playOnEnable=true 시 OnEnable에서 자동 재생, 또는 외부에서 Play()/Stop() 호출
/// 【의존성】 DOTween(DG.Tweening), RectTransform(스케일), CanvasGroup(알파)
/// </summary>
public class GlowEffect : MonoBehaviour
{
    public enum ScaleAxis
    {
        None,       // 스케일 애니메이션 없음
        X,          // X축만
        Y,          // Y축만
        XY          // X, Y 동시
    }

    [Header("스케일 설정")]
    [SerializeField] private ScaleAxis scaleAxis = ScaleAxis.XY;
    [SerializeField] private float scaleMin = 1f;
    [SerializeField] private float scaleMax = 1.2f;

    [Header("알파 설정")]
    [SerializeField] private bool useAlpha = true;
    [SerializeField] private float alphaMin = 0.5f;
    [SerializeField] private float alphaMax = 1f;

    [Header("애니메이션")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease easeType = Ease.InOutSine;

    [Header("자동 시작")]
    public bool playOnEnable = true;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector3 _baseScale;
    private Sequence _sequence;
    private bool _baseScaleCaptured;

    private void OnEnable()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        // 최초 1회만 베이스 스케일 캡처
        if (_rectTransform != null && !_baseScaleCaptured)
        {
            _baseScale = _rectTransform.localScale;
            _baseScaleCaptured = true;
        }

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    /// <summary>
    /// Glow 애니메이션 시작
    /// </summary>
    public void Play()
    {
        Stop();

        _sequence = DOTween.Sequence();

        float halfDuration = duration / 2f;

        bool hasScale = scaleAxis != ScaleAxis.None && _rectTransform != null;
        bool hasAlpha = useAlpha && _canvasGroup != null;

        if (hasScale)
        {
            Vector3 minScale = GetTargetScale(scaleMin);
            Vector3 maxScale = GetTargetScale(scaleMax);
            _rectTransform.localScale = minScale;

            // 전반부: min → max
            _sequence.Append(_rectTransform.DOScale(maxScale, halfDuration).SetEase(easeType));
            if (hasAlpha)
            {
                _canvasGroup.alpha = alphaMin;
                _sequence.Join(_canvasGroup.DOFade(alphaMax, halfDuration).SetEase(easeType));
            }

            // 후반부: max → min
            _sequence.Append(_rectTransform.DOScale(minScale, halfDuration).SetEase(easeType));
            if (hasAlpha)
                _sequence.Join(_canvasGroup.DOFade(alphaMin, halfDuration).SetEase(easeType));
        }
        else if (hasAlpha)
        {
            // 알파만 단독
            _canvasGroup.alpha = alphaMin;
            _sequence.Append(_canvasGroup.DOFade(alphaMax, halfDuration).SetEase(easeType));
            _sequence.Append(_canvasGroup.DOFade(alphaMin, halfDuration).SetEase(easeType));
        }

        // 무한 반복 + 오브젝트 파괴 시 자동 Kill
        _sequence.SetLoops(-1, LoopType.Restart);
        _sequence.SetLink(gameObject);
    }

    /// <summary>
    /// Glow 애니메이션 중지
    /// </summary>
    public void Stop()
    {
        _sequence?.Kill();
        _sequence = null;

        // 원래 상태로 복원
        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 축에 따른 타겟 스케일 계산 (baseScale * value)
    /// </summary>
    private Vector3 GetTargetScale(float value)
    {
        switch (scaleAxis)
        {
            case ScaleAxis.X:
                return new Vector3(_baseScale.x * value, _baseScale.y, _baseScale.z);
            case ScaleAxis.Y:
                return new Vector3(_baseScale.x, _baseScale.y * value, _baseScale.z);
            case ScaleAxis.XY:
                return new Vector3(_baseScale.x * value, _baseScale.y * value, _baseScale.z);
            default:
                return _baseScale;
        }
    }

    /// <summary>
    /// 런타임에 설정 변경
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }

    public void SetAlphaRange(float min, float max)
    {
        alphaMin = min;
        alphaMax = max;
    }

    public void SetScaleRange(float min, float max)
    {
        scaleMin = min;
        scaleMax = max;
    }
}
