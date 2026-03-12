using UnityEngine;
using DG.Tweening;

/// <summary>
/// TouchPromptPulse - 스케일 펄스로 터치/클릭을 유도하는 프롬프트 애니메이션 컴포넌트
///
/// 【역할】 DOTween Yoyo 루프로 오브젝트 스케일을 min↔max 사이에서 반복 변화시켜
///          "여기를 터치/클릭하세요" 시각 힌트 제공. OnDisable 시 원래 스케일 복원.
/// 【사용 위치】 Problem2 Step2(필름 카드 터치 유도), 버튼/아이콘 인터랙션 유도
/// 【트리거】 OnEnable 시 자동 재생
/// 【의존성】 DOTween(DG.Tweening), Transform
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
