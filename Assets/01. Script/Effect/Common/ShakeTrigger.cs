using UnityEngine;
using DG.Tweening;

/// <summary>
/// ShakeTrigger - DOTween 기반 반복 떨림(진동) 효과 컴포넌트
///
/// 【역할】 StartShake() 호출 시 DOShakeAnchorPos로 오브젝트를 반복 흔들리게 하고,
///          StopShake() 호출 시 정지 후 원래 위치로 복귀한다.
///          한 번의 셰이크가 끝나면 OnComplete에서 다시 호출하여 무한 반복.
/// 【사용 위치】 터치 유도 힌트, 주목이 필요한 UI 요소, IntroElement 도착 후 효과 등
/// 【트리거】 외부에서 StartShake()/StopShake() 호출
/// 【의존성】 DOTween(DG.Tweening), RectTransform
/// </summary>
public class ShakeTrigger : MonoBehaviour
{
    [Header("===== 떨림 설정 =====")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 6f;
    [SerializeField] private int   shakeVibrato  = 12;
    [SerializeField] private float shakeRandomness = 90f;

    private RectTransform _rectTransform;
    private Tween _shakeTween;
    private bool _shaking;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        StopShake();
    }

    /// <summary>
    /// 반복 떨림 시작 (StopShake 호출 전까지 계속)
    /// </summary>
    public void StartShake()
    {
        if (_shaking) return;

        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null) return;

        _shaking = true;
        DoShakeLoop();
    }

    /// <summary>
    /// 떨림 정지
    /// </summary>
    public void StopShake()
    {
        _shaking = false;
        _shakeTween?.Kill(true); // complete=true → 원래 위치로 복귀
        _shakeTween = null;
    }

    private void DoShakeLoop()
    {
        if (!_shaking || _rectTransform == null) return;

        _shakeTween = _rectTransform.DOShakeAnchorPos(
            shakeDuration,
            shakeStrength,
            shakeVibrato,
            shakeRandomness,
            false,  // snapping
            false   // fadeOut=false → 세기 유지
        ).OnComplete(DoShakeLoop);
    }
}
