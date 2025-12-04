using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 녹음 중 펄스 애니메이션
/// - 마이크 버튼 주변 파동 효과
/// - 활성화되면 펄스 시작
///
/// [구조]
/// - micRoot (이 스크립트)
///   - micEffectImage (pulseRing) <- 확장되는 펄스 링, 버튼 뒤에 배치
///   - button (buttonImage) <- 실제 버튼 이미지
///
/// [사용처]
/// - Problem2 Step3: 마이크 녹음 버튼
/// - 음성 입력 UI
/// </summary>
public class RecordingPulse : MonoBehaviour
{
    [Header("===== 펄스 링 설정 =====")]
    [SerializeField] private Image pulseRing;           // 펄스 링 이미지 (버튼 뒤 배치)
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float maxScale = 1.5f;     // 최대 스케일
    [SerializeField] private Color pulseColor = new Color(1f, 0.54f, 0.24f, 0.7f);  // #FF8A3D

    [Header("버튼 색상")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = new Color(0.49f, 0.35f, 0.27f, 1f);   // #7C5A46
    [SerializeField] private Color recordingColor = new Color(1f, 0.54f, 0.24f, 1f);   // #FF8A3D

    // 내부
    private bool _isRecording;
    private float _time;
    private RectTransform _pulseRingRect;

    private void Awake()
    {
        if (pulseRing != null)
        {
            _pulseRingRect = pulseRing.GetComponent<RectTransform>();
        }

        ResetToNormal();
    }

    private void OnEnable()
    {
        // micRoot가 SetActive(true) 될 때 상태 리셋
        ResetToNormal();
    }

    /// <summary>
    /// 초기 상태로 리셋 (public으로 외부에서 호출 가능)
    /// </summary>
    public void ResetToNormal()
    {
        _isRecording = false;
        _time = 0f;

        if (pulseRing != null)
        {
            pulseRing.gameObject.SetActive(false);
            if (_pulseRingRect != null)
                _pulseRingRect.localScale = Vector3.one;
        }

        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    private void Update()
    {
        if (!_isRecording || pulseRing == null) return;

        _time += Time.deltaTime;
        float normalizedTime = (_time % pulseDuration) / pulseDuration;

        // 스케일: 1 → maxScale
        float scale = Mathf.Lerp(1f, maxScale, normalizedTime);
        _pulseRingRect.localScale = Vector3.one * scale;

        // 알파: 0.7 → 0
        float alpha = Mathf.Lerp(pulseColor.a, 0f, normalizedTime);
        Color c = pulseColor;
        c.a = alpha;
        pulseRing.color = c;
    }

    /// <summary>
    /// 녹음 상태 설정
    /// </summary>
    public void SetRecording(bool recording)
    {
        _isRecording = recording;
        _time = 0f;

        if (pulseRing != null)
        {
            pulseRing.gameObject.SetActive(recording);
            if (recording)
            {
                _pulseRingRect.localScale = Vector3.one;
            }
        }

        if (buttonImage != null)
            buttonImage.color = recording ? recordingColor : normalColor;
    }

    /// <summary>
    /// 녹음 토글
    /// </summary>
    public void ToggleRecording()
    {
        SetRecording(!_isRecording);
    }

    public bool IsRecording => _isRecording;
}
