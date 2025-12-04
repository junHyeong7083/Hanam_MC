using UnityEngine;

/// <summary>
/// 캐릭터/아이콘 플로팅(둥둥) 애니메이션
/// - 위아래로 부드럽게 움직임
/// - 프리팹화하여 재사용
///
/// [사용처]
/// - 하남이 캐릭터 아이콘
/// - 도우미 캐릭터
/// - 떠다니는 UI 요소
/// </summary>
public class FloatingAnimation : MonoBehaviour
{
    [Header("===== 플로팅 설정 =====")]
    [SerializeField] private float floatDistance = 10f;   // 위아래 이동 거리
    [SerializeField] private float floatSpeed = 1.5f;     // 속도
    [SerializeField] private float randomOffset = 0f;     // 여러 개일 때 시간차 (0~1)

    [Header("회전 (선택)")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationAmount = 5f;   // 좌우 기울기 각도

    // 내부
    private RectTransform _rectTransform;
    private Vector2 _startPosition;
    private float _timeOffset;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startPosition = _rectTransform.anchoredPosition;

        // 랜덤 오프셋 적용 (여러 캐릭터가 동시에 안 움직이게)
        if (randomOffset > 0)
            _timeOffset = Random.Range(0f, randomOffset * Mathf.PI * 2f);
    }

    private void Update()
    {
        float time = Time.time * floatSpeed + _timeOffset;

        // 위아래 움직임
        float yOffset = Mathf.Sin(time) * floatDistance;
        _rectTransform.anchoredPosition = _startPosition + Vector2.up * yOffset;

        // 회전 (선택)
        if (enableRotation)
        {
            float rotation = Mathf.Sin(time * 0.5f) * rotationAmount;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 원위치
        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _startPosition;
            _rectTransform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 시작 위치 재설정 (위치 변경 후 호출)
    /// </summary>
    public void ResetStartPosition()
    {
        if (_rectTransform != null)
            _startPosition = _rectTransform.anchoredPosition;
    }
}
