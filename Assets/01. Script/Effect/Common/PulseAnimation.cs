using UnityEngine;
using DG.Tweening;

/// <summary>
/// PulseAnimation - Scale/Alpha/Both 타입의 범용 펄스(맥박) 애니메이션 컴포넌트
///
/// 【역할】 DOTween Yoyo 루프를 사용하여 스케일 또는 알파(또는 둘 다)를
///          min↔max 사이에서 반복적으로 변화시킨다. 무한 반복 또는 지정 횟수만큼 재생 가능.
///          완료 시 원래 상태로 자동 복원.
/// 【사용 위치】 Problem2 Step1(NG 뱃지, 감정 조명 글로우), Problem2 Step2(lightGlowImage),
///              기타 강조가 필요한 모든 UI 요소
/// 【트리거】 playOnEnable=true 시 OnEnable에서 자동 재생, 또는 외부에서 Play()/PlayTimes() 호출
/// 【의존성】 DOTween(DG.Tweening), RectTransform(스케일), CanvasGroup(알파)
/// </summary>
public class PulseAnimation : MonoBehaviour
{
    public enum PulseType
    {
        Scale,      // localScale 변화
        Alpha,      // CanvasGroup alpha 변화
        Both        // Scale + Alpha 동시
    }

    [Header("===== 펄스 설정 =====")]
    [SerializeField] private PulseType pulseType = PulseType.Scale;

    [Header("Scale 설정")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 1.3f;

    [Header("Alpha 설정")]
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("타이밍")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private int loopCount = -1; // -1 = 무한, 0 이상 = 해당 횟수만큼

    // 내부 상태
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector3 _baseScale;
    private float _baseAlpha;
    private Sequence _sequence;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();

        // 기본값 저장
        if (_rectTransform != null)
            _baseScale = _rectTransform.localScale;

        if (_canvasGroup != null)
            _baseAlpha = _canvasGroup.alpha;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
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

    private void PlayInternal(int loops)
    {
        KillSequence();

        _sequence = DOTween.Sequence();

        float halfDuration = duration * 0.5f;

        // Scale 펄스
        if ((pulseType == PulseType.Scale || pulseType == PulseType.Both) && _rectTransform != null)
        {
            // min → max
            _rectTransform.localScale = _baseScale * minScale;
            var scaleTween = _rectTransform
                .DOScale(_baseScale * maxScale, halfDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(2, LoopType.Yoyo);

            _sequence.Append(scaleTween);
        }

        // Alpha 펄스
        if ((pulseType == PulseType.Alpha || pulseType == PulseType.Both) && _canvasGroup != null)
        {
            _canvasGroup.alpha = minAlpha;
            var alphaTween = _canvasGroup
                .DOFade(maxAlpha, halfDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(2, LoopType.Yoyo);

            if (pulseType == PulseType.Both)
                _sequence.Join(alphaTween);
            else
                _sequence.Append(alphaTween);
        }

        // 루프 설정
        if (loops != 0)
            _sequence.SetLoops(loops);

        // 완료 시 원래 상태로 복원
        _sequence.OnComplete(() =>
        {
            if (_rectTransform != null)
                _rectTransform.localScale = _baseScale;
            if (_canvasGroup != null)
                _canvasGroup.alpha = _baseAlpha;
        });
    }

    #region Public API

    public void Play()
    {
        int loops = loop ? -1 : (loopCount > 0 ? loopCount : 1);
        PlayInternal(loops);
    }

    public void Stop()
    {
        KillSequence();

        // 원래 상태로 복원
        if (_rectTransform != null)
            _rectTransform.localScale = _baseScale;

        if (_canvasGroup != null)
            _canvasGroup.alpha = _baseAlpha;
    }

    /// <summary>
    /// 특정 횟수만큼 펄스 재생 (NG 뱃지처럼 3번만 펄스)
    /// </summary>
    public void PlayTimes(int times)
    {
        PlayInternal(times);
    }

    #endregion
}
