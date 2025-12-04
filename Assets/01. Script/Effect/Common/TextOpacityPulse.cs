using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 텍스트 투명도 펄스 애니메이션
/// - 안내 텍스트가 깜빡이며 주목을 끄는 효과
/// - CanvasGroup 없이 Text 컴포넌트 직접 제어
///
/// [사용처]
/// - Problem2 Step1: "마음 렌즈를 필름 위로 드래그하세요" 안내 텍스트
/// - 모든 Step의 안내/힌트 텍스트
/// </summary>
public class TextOpacityPulse : MonoBehaviour
{
    [Header("===== 펄스 설정 =====")]
    [SerializeField] private float minOpacity = 0.5f;
    [SerializeField] private float maxOpacity = 1f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool playOnEnable = true;

    // 내부 상태
    private Text _text;
    private Graphic _graphic; // Image, Text 등 모든 UI 요소 지원
    private Color _baseColor;
    private bool _isPlaying;
    private float _time;

    private void Awake()
    {
        _text = GetComponent<Text>();
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
        if (!_isPlaying || _graphic == null) return;

        _time += Time.deltaTime;
        float normalizedTime = (_time % duration) / duration;

        // 0 → 1 → 0 사이클
        float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(minOpacity, maxOpacity, wave);

        Color c = _baseColor;
        c.a = alpha;
        _graphic.color = c;
    }

    #region Public API

    public void Play()
    {
        _isPlaying = true;
        _time = 0f;
    }

    public void Stop()
    {
        _isPlaying = false;

        // 원래 색상 복원
        if (_graphic != null)
            _graphic.color = _baseColor;
    }

    #endregion
}
