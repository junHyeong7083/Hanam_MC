using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SparkleEffect - 개별 스파클 오브젝트에 부착하여 반짝임 애니메이션을 적용하는 컴포넌트
///
/// 【역할】 Update에서 수동으로 opacity/scale/rotation을 동시에 애니메이션한다.
///          패턴: 0→1→0 (페이드인→페이드아웃), 회전은 0→360 선형.
///          여러 개를 배치하고 delay를 다르게 설정하면 순차적 반짝임 효과 구현 가능.
/// 【사용 위치】 Problem2 Step1(마음 렌즈 스파클 아이콘), 인벤토리 아이템 강조, 보상 획득 이펙트
/// 【트리거】 playOnEnable=true 시 OnEnable에서 자동 재생, 또는 외부에서 Play()/Stop() 호출
/// 【의존성】 RectTransform(스케일/회전), Graphic(Image/Text 등 - 알파 제어)
/// </summary>
public class SparkleEffect : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float delay = 0f; // 시작 딜레이 (여러 스파클 순차 효과용)
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;

    [Header("Scale")]
    [SerializeField] private float minScale = 0f;
    [SerializeField] private float maxScale = 1f;

    [Header("Rotation")]
    [SerializeField] private float startRotation = 0f;
    [SerializeField] private float endRotation = 360f;

    [Header("Opacity")]
    [SerializeField] private float minOpacity = 0f;
    [SerializeField] private float maxOpacity = 1f;

    // 내부 상태
    private RectTransform _rectTransform;
    private Graphic _graphic; // Image, Text 등
    private Color _baseColor;
    private bool _isPlaying;
    private float _time;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _graphic = GetComponent<Graphic>();
        if (_graphic != null)
            _baseColor = _graphic.color;
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

        // 딜레이 처리
        float effectiveTime = _time - delay;
        if (effectiveTime < 0f)
        {
            // 딜레이 중에는 숨김
            SetVisualState(0f, 0f, startRotation);
            return;
        }

        float normalizedTime = (effectiveTime % duration) / duration;

        // 0 → 1 → 0 패턴 (페이드인 → 페이드아웃)
        // TS: opacity: [0, 1, 0], scale: [0, 1, 0]
        float wave;
        if (normalizedTime < 0.5f)
        {
            // 0 → 1
            wave = normalizedTime * 2f;
        }
        else
        {
            // 1 → 0
            wave = (1f - normalizedTime) * 2f;
        }

        // rotation은 0 → 360 선형
        float rotation = Mathf.Lerp(startRotation, endRotation, normalizedTime);

        float scale = Mathf.Lerp(minScale, maxScale, wave);
        float opacity = Mathf.Lerp(minOpacity, maxOpacity, wave);

        SetVisualState(scale, opacity, rotation);

        // 루프 체크
        if (!loop && effectiveTime >= duration)
        {
            Stop();
        }
    }

    private void SetVisualState(float scale, float opacity, float rotation)
    {
        if (_rectTransform != null)
        {
            _rectTransform.localScale = Vector3.one * scale;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        if (_graphic != null)
        {
            Color c = _baseColor;
            c.a = opacity;
            _graphic.color = c;
        }
    }

    #region Public API

    public void Play()
    {
        _isPlaying = true;
        _time = 0f;
        SetVisualState(0f, 0f, startRotation);
    }

    public void Stop()
    {
        _isPlaying = false;
        SetVisualState(0f, 0f, startRotation);
    }

    /// <summary>
    /// 딜레이 설정 (여러 스파클 순차 효과용)
    /// </summary>
    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }

    #endregion
}
