using UnityEngine;
using DG.Tweening;

/// <summary>
/// PopupSpring - 스케일 0에서 스프링 바운스로 튀어나오는 팝업 애니메이션 컴포넌트
///
/// 【역할】 오브젝트를 scale 0에서 시작하여 DOTween OutBack 이징으로 목표 스케일까지
///          오버슈트(지정된 비율만큼 초과 확대 후 복귀)하며 등장시킨다.
/// 【사용 위치】 Problem2 Step2(감정 라벨 팝업), 정답 표시, 보상 등장, 완료 스파클 등
/// 【트리거】 OnEnable 시 자동 재생, 또는 외부에서 Play() 호출
/// 【의존성】 DOTween(DG.Tweening)
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
