using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BadgeGlowPulse - 뱃지 주변에 확장되는 링 형태의 펄스 이펙트
///
/// 【역할】 OK/성공 뱃지 주변에 반복적으로 확장되었다 사라지는 링 펄스 효과 제공
///          스케일 1 → maxRingScale로 확장하면서 알파가 0으로 감소하는 패턴 반복
/// 【사용 위치】 Problem2 Step3(OK 뱃지), 성공/완료 표시 등
/// 【트리거】 오브젝트 SetActive(true) 시 OnEnable에서 자동 시작 (startDelay 후)
/// 【의존성】 pulseRing(Image) - 확장되는 링 이미지를 인스펙터에서 연결
/// </summary>
public class BadgeGlowPulse : MonoBehaviour
{
    [Header("===== 펄스 링 =====")]
    [SerializeField] private Image pulseRing;             // 펄스 링으로 사용할 Image (뱃지 뒤에 배치)
    [SerializeField] private float pulseDuration = 1.5f;  // 한 펄스 주기 (초)
    [SerializeField] private float maxRingScale = 2f;     // 링이 퍼지는 최대 스케일

    [Header("색상")]
    [SerializeField] private Color ringColor = new Color(0.13f, 0.77f, 0.37f, 0.7f);  // 펄스 링 색상 (초록)

    [Header("딜레이")]
    [SerializeField] private float startDelay = 0.5f;     // 활성화 후 펄스 시작까지 대기 시간

    // 내부 상태
    private RectTransform _pulseRingRect;  // 펄스 링의 RectTransform (스케일 조절용)
    private float _time;                   // 경과 시간 (딜레이 포함)
    private bool _started;                 // 딜레이 완료 후 펄스 시작 플래그

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
