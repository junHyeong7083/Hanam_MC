using UnityEngine;

/// <summary>
/// 터치 유도 펄스 애니메이션
/// - "터치하여 확인" 같은 프롬프트에 사용
/// - 스케일 펄스로 주목 유도
///
/// [사용처]
/// - Problem2 Step2: 필름 카드 터치 유도
/// - 버튼, 아이콘 등 인터랙션 유도
/// </summary>
public class TouchPromptPulse : MonoBehaviour
{
    [Header("===== 펄스 설정 =====")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 1.1f;

    private Vector3 _originalScale;
    private float _time;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        _time = 0f;
    }

    private void Update()
    {
        _time += Time.deltaTime;

        float normalizedTime = (_time % duration) / duration;
        float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;  // 0~1
        float scale = Mathf.Lerp(minScale, maxScale, wave);

        transform.localScale = _originalScale * scale;
    }

    private void OnDisable()
    {
        transform.localScale = _originalScale;
    }
}
