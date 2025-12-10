using UnityEngine;
using DG.Tweening;

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
    private Tween _tween;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        PlayAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
        transform.localScale = _originalScale;
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

        // 시작 스케일 설정
        transform.localScale = _originalScale * minScale;

        // min → max 펄스 (Yoyo로 왕복)
        _tween = transform
            .DOScale(_originalScale * maxScale, duration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopAnimation()
    {
        KillTween();
    }
}
