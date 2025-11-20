using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마이크 버튼이 녹음 중일 때 살짝 펄스 + 색상 변경.
/// 버튼 오브젝트에 붙이고, OnClick에서 ToggleRecording() 호출.
/// </summary>
public class MicRecordingIndicator : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color idleColor = new Color(0.49f, 0.35f, 0.27f);   // #7C5A46 근사
    [SerializeField] private Color recordingColor = new Color(1f, 0.54f, 0.24f); // #FF8A3D 근사
    [SerializeField] private float pulseAmplitude = 0.05f;
    [SerializeField] private float pulseSpeed = 3f;

    private bool _recording;
    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        ApplyColor();
    }

    private void Update()
    {
        if (!_recording)
        {
            // 원래 크기로 천천히 복귀
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 10f * Time.deltaTime);
            return;
        }

        // 녹음 중일 때 펄스
        float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
        transform.localScale = _baseScale * s;
    }

    public void ToggleRecording()
    {
        _recording = !_recording;
        ApplyColor();
    }

    public void SetRecording(bool value)
    {
        _recording = value;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (backgroundImage != null)
            backgroundImage.color = _recording ? recordingColor : idleColor;
    }
}
