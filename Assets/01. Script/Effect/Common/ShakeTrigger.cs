using UnityEngine;
using DG.Tweening;

/// <summary>
/// 덜덜 떨림 효과 컴포넌트
/// - StartShake()로 시작, StopShake()로 정지
/// - 터치 전까지 계속 반복 떨림
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
