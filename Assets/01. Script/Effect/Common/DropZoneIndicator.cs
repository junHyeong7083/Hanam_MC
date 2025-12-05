using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드롭 영역 시각적 표시
/// - 이 스크립트가 붙은 오브젝트 자체를 애니메이션
/// - 알파 + 스케일 펄스 지원
/// - 드롭 체크는 InventoryDropTargetStepBase에서 처리
///
/// [사용처]
/// - Problem2 Step1: UIDropBoxArea.outline 오브젝트에 부착
/// - Problem3 Step1: 시나리오 책 드롭 존
/// - 모든 드래그-드롭 스텝의 타겟 영역
/// </summary>
[RequireComponent(typeof(Image))]
public class DropZoneIndicator : MonoBehaviour
{
    [Header("===== 알파 펄스 =====")]
    [SerializeField] private bool enableAlphaPulse = true;
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float minAlpha = 0.5f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("===== 스케일 펄스 =====")]
    [SerializeField] private bool enableScalePulse = false;
    [SerializeField] private float scaleMin = 1f;
    [SerializeField] private float scaleMax = 1.1f;

    // 내부
    private Image _image;
    private Color _baseColor;
    private Vector3 _baseScale;
    private float _time;

    private void Awake()
    {
        _image = GetComponent<Image>();

        if (_image != null)
            _baseColor = _image.color;

        _baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        _time = 0f;
    }

    private void Update()
    {
        if (_image == null) return;
        if (!enableAlphaPulse && !enableScalePulse) return;

        _time += Time.deltaTime;
        float normalizedTime = (_time % pulseDuration) / pulseDuration;
        float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;

        // 알파 펄스
        if (enableAlphaPulse)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
            Color c = _baseColor;
            c.a = alpha;
            _image.color = c;
        }

        // 스케일 펄스
        if (enableScalePulse)
        {
            float scale = Mathf.Lerp(scaleMin, scaleMax, wave);
            transform.localScale = _baseScale * scale;
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 원래 상태 복원
        if (_image != null)
            _image.color = _baseColor;

        transform.localScale = _baseScale;
    }
}
