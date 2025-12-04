using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뱃지 글로우 펄스 (OK 뱃지 등)
/// - 뱃지 주변에 확장되는 링 펄스
/// - SetActive 시 자동 시작
///
/// [사용처]
/// - Problem2 Step3: OK 뱃지
/// - 성공/완료 표시
/// </summary>
public class BadgeGlowPulse : MonoBehaviour
{
    [Header("===== 펄스 링 =====")]
    [SerializeField] private Image pulseRing;
    [SerializeField] private float pulseDuration = 1.5f;
    [SerializeField] private float maxRingScale = 2f;    // 링이 퍼지는 최대 스케일

    [Header("색상")]
    [SerializeField] private Color ringColor = new Color(0.13f, 0.77f, 0.37f, 0.7f);  // 초록색

    [Header("딜레이")]
    [SerializeField] private float startDelay = 0.5f;

    // 내부
    private RectTransform _pulseRingRect;
    private float _time;
    private bool _started;

    private void Awake()
    {
        if (pulseRing != null)
        {
            _pulseRingRect = pulseRing.GetComponent<RectTransform>();
            pulseRing.color = ringColor;
        }
    }

    private void OnEnable()
    {
        _time = -startDelay;
        _started = false;

        if (_pulseRingRect != null)
            _pulseRingRect.localScale = Vector3.one;
    }

    private void Update()
    {
        if (pulseRing == null) return;

        _time += Time.deltaTime;

        if (_time < 0) return;  // 딜레이 중

        if (!_started)
        {
            _started = true;
        }

        float normalizedTime = (_time % pulseDuration) / pulseDuration;

        // 스케일: 1 → maxRingScale
        float scale = Mathf.Lerp(1f, maxRingScale, normalizedTime);
        _pulseRingRect.localScale = Vector3.one * scale;

        // 알파: 시작값 → 0
        float alpha = Mathf.Lerp(ringColor.a, 0f, normalizedTime);
        Color c = ringColor;
        c.a = alpha;
        pulseRing.color = c;
    }

    /// <summary>
    /// 색상 변경
    /// </summary>
    public void SetColor(Color color)
    {
        ringColor = color;
        if (pulseRing != null)
            pulseRing.color = color;
    }
}
