using UnityEngine;
using DG.Tweening;

/// <summary>
/// 팝업 스프링 애니메이션
/// - scale 0에서 튀어나오는 효과
/// - 오버슈트 + 바운스
///
/// [사용처]
/// - Problem2 Step2: 감정 라벨 팝업
/// - 정답 표시, 보상 등장
/// </summary>
public class PopupSpring : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float overshoot = 1.2f;  // 최대 스케일 (오버슈트)

    [Header("타이밍")]
    [SerializeField] private float delay = 0f;

    // 내부
    private Vector3 _targetScale;
    private Tween _tween;

    private void Awake()
    {
        _targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        PlayAnimation();
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void KillTween()
    {
        _tween?.Kill();
        _tween = null;
    }

    private void PlayAnimation()
    {
        KillTween();

        // DOTween의 OutBack은 overshoot 값을 지원 (기본값 1.70158)
        // overshoot 1.2는 약간의 오버슈트를 원하므로 OutBack 사용
        float customOvershoot = (overshoot - 1f) * 10f; // 0.2 → 2.0

        _tween = transform
            .DOScale(_targetScale, duration)
            .SetDelay(delay)
            .SetEase(Ease.OutBack, customOvershoot);
    }

    /// <summary>
    /// 외부에서 재생
    /// </summary>
    public void Play()
    {
        transform.localScale = Vector3.zero;
        PlayAnimation();
    }

    /// <summary>
    /// 딜레이 설정
    /// </summary>
    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }
}
