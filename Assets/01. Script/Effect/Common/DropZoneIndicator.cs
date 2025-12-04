using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드롭 영역 시각적 표시 (간단 버전)
/// - 이 스크립트가 붙은 오브젝트 자체를 애니메이션
/// - UIDropBoxArea의 outline 오브젝트에 직접 부착
/// - SetActive()는 외부(UIDropBoxArea)에서 처리, 여기선 애니메이션만
///
/// [사용처]
/// - Problem2 Step1: UIDropBoxArea.outline 오브젝트에 부착
/// - 모든 드래그-드롭 스텝의 타겟 영역
/// </summary>
[RequireComponent(typeof(Image))]
public class DropZoneIndicator : MonoBehaviour
{
    [Header("===== 애니메이션 =====")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 1f;

    // 내부
    private Image _image;
    private Color _baseColor;
    private float _time;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (_image != null)
            _baseColor = _image.color;
    }

    private void OnEnable()
    {
        _time = 0f;
    }

    private void Update()
    {
        if (!enablePulse || _image == null) return;

        _time += Time.deltaTime;
        float normalizedTime = (_time % pulseDuration) / pulseDuration;
        float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);

        Color c = _baseColor;
        c.a = alpha;
        _image.color = c;
    }

    private void OnDisable()
    {
        // 비활성화 시 원래 색상 복원
        if (_image != null)
            _image.color = _baseColor;
    }
}
