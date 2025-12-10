using UnityEngine;
using DG.Tweening;

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
    private Sequence _sequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startPosition = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();

        // 비활성화 시 원위치
        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _startPosition;
            _rectTransform.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void KillSequence()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    private void PlayAnimation()
    {
        KillSequence();

        // 랜덤 오프셋 적용 (여러 캐릭터가 동시에 안 움직이게)
        float delayOffset = 0f;
        if (randomOffset > 0)
            delayOffset = Random.Range(0f, randomOffset);

        // floatSpeed를 duration으로 변환 (speed 1.5 = 약 2초 / 1.5 = 1.33초 주기)
        float cycleDuration = 2f / floatSpeed;

        _sequence = DOTween.Sequence();

        if (delayOffset > 0f)
            _sequence.AppendInterval(delayOffset);

        // 위아래 움직임 (위로 → 아래로 → 위로)
        Vector2 upPos = _startPosition + Vector2.up * floatDistance;
        Vector2 downPos = _startPosition + Vector2.down * floatDistance;

        _sequence.Append(_rectTransform
            .DOAnchorPos(upPos, cycleDuration * 0.5f)
            .SetEase(Ease.InOutSine));
        _sequence.Append(_rectTransform
            .DOAnchorPos(downPos, cycleDuration)
            .SetEase(Ease.InOutSine));
        _sequence.Append(_rectTransform
            .DOAnchorPos(_startPosition, cycleDuration * 0.5f)
            .SetEase(Ease.InOutSine));

        // 회전 (선택)
        if (enableRotation)
        {
            _sequence.Join(_rectTransform
                .DORotate(new Vector3(0, 0, rotationAmount), cycleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(2, LoopType.Yoyo));
        }

        _sequence.SetLoops(-1);
    }

    private void StopAnimation()
    {
        KillSequence();
    }

    /// <summary>
    /// 시작 위치 재설정 (위치 변경 후 호출)
    /// </summary>
    public void ResetStartPosition()
    {
        if (_rectTransform != null)
        {
            StopAnimation();
            _startPosition = _rectTransform.anchoredPosition;
            PlayAnimation();
        }
    }
}
