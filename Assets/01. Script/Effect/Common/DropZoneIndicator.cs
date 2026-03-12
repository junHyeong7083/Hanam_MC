using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DropZoneIndicator - 드래그-드롭 타겟 영역의 시각적 펄스 표시 컴포넌트
///
/// 【역할】 드롭 가능 영역을 알파 및/또는 스케일 펄스로 시각적으로 강조한다.
///          사인파 기반으로 min↔max 사이를 부드럽게 반복하여 "여기에 드롭하세요" 느낌 전달.
///          드롭 판정 자체는 InventoryDropTargetStepBase에서 별도 처리.
/// 【사용 위치】 Problem2 Step1(UIDropBoxArea.outline), Problem3 Step1(시나리오 책 드롭 존),
///              모든 드래그-드롭 스텝의 타겟 영역 (outline 오브젝트에 부착)
/// 【트리거】 OnEnable 시 자동 시작, OnDisable 시 원래 상태 복원
/// 【의존성】 Image(RequireComponent), RectTransform
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
