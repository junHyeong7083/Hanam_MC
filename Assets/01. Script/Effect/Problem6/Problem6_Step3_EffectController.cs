using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 6 - Step 3 이완 훈련 이펙트 컨트롤러
/// React 기반 애니메이션:
/// - 따뜻한 오라 (Warm Aura): 여러 개의 부드러운 글로우 오브
/// - 호흡 원 애니메이션: 들이쉬기 scale [1, 1.5], 내쉬기 scale [1.5, 1]
/// - 배경 디밍 효과
/// - 단계 전환 애니메이션
/// - 완료 축하 이펙트
/// </summary>
public class Problem6_Step3_EffectController : EffectControllerBase
{
    [Header("===== 따뜻한 오라 (Warm Aura) =====")]
    [SerializeField] private RectTransform[] warmAuraOrbs;           // 3-5개 권장
    [SerializeField] private CanvasGroup[] warmAuraCanvasGroups;
    [SerializeField] private float auraMinScale = 0.8f;
    [SerializeField] private float auraMaxScale = 1.2f;
    [SerializeField] private float auraMinAlpha = 0.3f;
    [SerializeField] private float auraMaxAlpha = 0.6f;
    [SerializeField] private float auraPulseDuration = 3f;

    [Header("===== 배경 디밍 =====")]
    [SerializeField] private CanvasGroup backgroundDimCanvasGroup;
    [SerializeField] private float dimAlpha = 0.3f;
    [SerializeField] private float dimDuration = 0.5f;

    [Header("===== 호흡 원 애니메이션 =====")]
    [SerializeField] private RectTransform breathingCircleRect;
    [SerializeField] private CanvasGroup breathingCircleCanvasGroup;
    [SerializeField] private Image breathingCircleImage;
    [SerializeField] private float breathMinScale = 1f;              // React: scale 1
    [SerializeField] private float breathMaxScale = 1.5f;            // React: scale 1.5
    [SerializeField] private float breathInDuration = 4f;
    [SerializeField] private float breathOutDuration = 4f;
    [SerializeField] private float breathHoldDuration = 2f;
    [SerializeField] private Color breathInColor = new Color(0.4f, 0.8f, 1f, 0.8f);  // 하늘색
    [SerializeField] private Color breathOutColor = new Color(0.6f, 0.4f, 1f, 0.8f); // 보라색

    [Header("===== 단계 전환 =====")]
    [SerializeField] private RectTransform stepCardRect;
    [SerializeField] private CanvasGroup stepCardCanvasGroup;
    [SerializeField] private float stepTransitionDuration = 0.4f;
    [SerializeField] private float stepSlideDistance = 30f;

    // 호흡 루프 트윈
    private Sequence _breathingSequence;
    private Tween[] _auraPulseTweens;
    private Tween[] _auraAlphaTweens;
    private Vector3 _breathingBaseScale;
    private Vector2 _stepCardBasePos;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }


    public void SaveInitialState()
    {
        if (_initialized) return;

        if (breathingCircleRect != null)
            _breathingBaseScale = breathingCircleRect.localScale;

        if (stepCardRect != null)
            _stepCardBasePos = stepCardRect.anchoredPosition;

        _initialized = true;
    }

    #region Warm Aura & Background Dim

    /// <summary>
    /// 따뜻한 오라 시작 (React: WarmAuraAnimation)
    /// 여러 개의 부드러운 글로우 오브가 펄스
    /// </summary>
    public void StartWarmAura()
    {
        if (warmAuraOrbs == null || warmAuraOrbs.Length == 0) return;

        StopWarmAura();

        int count = warmAuraOrbs.Length;
        _auraPulseTweens = new Tween[count];
        _auraAlphaTweens = new Tween[count];

        for (int i = 0; i < count; i++)
        {
            if (warmAuraOrbs[i] == null) continue;

            // 활성화
            warmAuraOrbs[i].gameObject.SetActive(true);

            // 랜덤 오프셋으로 자연스럽게
            float delay = i * 0.5f;
            float randomDuration = auraPulseDuration + UnityEngine.Random.Range(-0.5f, 0.5f);

            // 스케일 펄스: [0.8, 1.2] 반복
            warmAuraOrbs[i].localScale = Vector3.one * auraMinScale;
            _auraPulseTweens[i] = warmAuraOrbs[i]
                .DOScale(auraMaxScale, randomDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);

            // 알파 펄스: [0.3, 0.6] 반복
            if (warmAuraCanvasGroups != null && i < warmAuraCanvasGroups.Length && warmAuraCanvasGroups[i] != null)
            {
                warmAuraCanvasGroups[i].alpha = auraMinAlpha;
                _auraAlphaTweens[i] = warmAuraCanvasGroups[i]
                    .DOFade(auraMaxAlpha, randomDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(delay);
            }
        }
    }

    /// <summary>
    /// 따뜻한 오라 정지
    /// </summary>
    public void StopWarmAura()
    {
        KillAuraTweens();

        if (warmAuraOrbs != null)
        {
            foreach (var orb in warmAuraOrbs)
            {
                if (orb != null)
                    orb.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 배경 디밍 시작 (호흡 시작 시)
    /// </summary>
    public void ShowBackgroundDim(Action onComplete = null)
    {
        if (backgroundDimCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        backgroundDimCanvasGroup.gameObject.SetActive(true);
        backgroundDimCanvasGroup.alpha = 0f;
        backgroundDimCanvasGroup
            .DOFade(dimAlpha, dimDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 배경 디밍 해제
    /// </summary>
    public void HideBackgroundDim(Action onComplete = null)
    {
        if (backgroundDimCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        backgroundDimCanvasGroup
            .DOFade(0f, dimDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                backgroundDimCanvasGroup.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    #endregion

    #region Breathing Animation

    /// <summary>
    /// 호흡 애니메이션 시작
    /// </summary>
    public void StartBreathingAnimation()
    {
        SaveInitialState();

        if (breathingCircleRect == null) return;

        KillBreathingAnimation();

        // 호흡 원 활성화
        breathingCircleRect.gameObject.SetActive(true);
        breathingCircleRect.localScale = _breathingBaseScale * breathMinScale;

        if (breathingCircleCanvasGroup != null)
            breathingCircleCanvasGroup.alpha = 1f;

        _breathingSequence = DOTween.Sequence();

        // 들이쉬기: 작 → 크, 색상 변화
        _breathingSequence.Append(breathingCircleRect
            .DOScale(_breathingBaseScale * breathMaxScale, breathInDuration)
            .SetEase(Ease.InOutSine));

        if (breathingCircleImage != null)
            _breathingSequence.Join(breathingCircleImage.DOColor(breathInColor, breathInDuration));

        // 참기
        if (breathHoldDuration > 0)
            _breathingSequence.AppendInterval(breathHoldDuration);

        // 내쉬기: 크 → 작, 색상 변화
        _breathingSequence.Append(breathingCircleRect
            .DOScale(_breathingBaseScale * breathMinScale, breathOutDuration)
            .SetEase(Ease.InOutSine));

        if (breathingCircleImage != null)
            _breathingSequence.Join(breathingCircleImage.DOColor(breathOutColor, breathOutDuration));

        // 참기
        if (breathHoldDuration > 0)
            _breathingSequence.AppendInterval(breathHoldDuration);

        // 무한 반복
        _breathingSequence.SetLoops(-1);
    }

    /// <summary>
    /// 호흡 애니메이션 정지
    /// </summary>
    public void StopBreathingAnimation()
    {
        KillBreathingAnimation();

        if (breathingCircleRect != null)
            breathingCircleRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 호흡 애니메이션 일시정지
    /// </summary>
    public void PauseBreathingAnimation()
    {
        _breathingSequence?.Pause();
    }

    /// <summary>
    /// 호흡 애니메이션 재개
    /// </summary>
    public void ResumeBreathingAnimation()
    {
        _breathingSequence?.Play();
    }

    #endregion

    #region Step Transition

    /// <summary>
    /// 단계 전환 애니메이션 (현재 단계 나가고 → 다음 단계 들어옴)
    /// </summary>
    public void PlayStepTransition(Action onHalfway = null, Action onComplete = null)
    {
        if (stepCardRect == null)
        {
            onHalfway?.Invoke();
            onComplete?.Invoke();
            return;
        }

        SaveInitialState();

        var seq = CreateSequence();

        // 나가기: 왼쪽으로 슬라이드 + 페이드 아웃
        seq.Append(stepCardRect
            .DOAnchorPos(_stepCardBasePos + Vector2.left * stepSlideDistance, stepTransitionDuration * 0.5f)
            .SetEase(Ease.InQuad));

        if (stepCardCanvasGroup != null)
            seq.Join(stepCardCanvasGroup.DOFade(0f, stepTransitionDuration * 0.5f));

        // 중간 콜백 (텍스트 교체용)
        seq.AppendCallback(() =>
        {
            // 오른쪽에서 시작하도록 위치 변경
            stepCardRect.anchoredPosition = _stepCardBasePos + Vector2.right * stepSlideDistance;
            onHalfway?.Invoke();
        });

        // 들어오기: 오른쪽에서 원래 위치로 슬라이드 + 페이드 인
        seq.Append(stepCardRect
            .DOAnchorPos(_stepCardBasePos, stepTransitionDuration * 0.5f)
            .SetEase(Ease.OutQuad));

        if (stepCardCanvasGroup != null)
            seq.Join(stepCardCanvasGroup.DOFade(1f, stepTransitionDuration * 0.5f));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Reset

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        KillBreathingAnimation();
        KillAuraTweens();
        SaveInitialState();

        // 따뜻한 오라 숨김
        if (warmAuraOrbs != null)
        {
            foreach (var orb in warmAuraOrbs)
            {
                if (orb != null)
                    orb.gameObject.SetActive(false);
            }
        }

        // 배경 디밍 숨김
        if (backgroundDimCanvasGroup != null)
        {
            DOTween.Kill(backgroundDimCanvasGroup);
            backgroundDimCanvasGroup.alpha = 0f;
            backgroundDimCanvasGroup.gameObject.SetActive(false);
        }

        // 호흡 원 숨김
        if (breathingCircleRect != null)
        {
            breathingCircleRect.gameObject.SetActive(false);
            breathingCircleRect.localScale = _breathingBaseScale;
        }

        // 단계 카드 위치 복원
        if (stepCardRect != null)
            stepCardRect.anchoredPosition = _stepCardBasePos;

        if (stepCardCanvasGroup != null)
            stepCardCanvasGroup.alpha = 1f;
    }

    #endregion

    #region Private Helpers

    private void KillBreathingAnimation()
    {
        _breathingSequence?.Kill();
        _breathingSequence = null;

        if (breathingCircleRect != null)
            DOTween.Kill(breathingCircleRect);

        if (breathingCircleImage != null)
            DOTween.Kill(breathingCircleImage);
    }

    private void KillAuraTweens()
    {
        if (_auraPulseTweens != null)
        {
            for (int i = 0; i < _auraPulseTweens.Length; i++)
            {
                _auraPulseTweens[i]?.Kill();
                _auraPulseTweens[i] = null;
            }
        }

        if (_auraAlphaTweens != null)
        {
            for (int i = 0; i < _auraAlphaTweens.Length; i++)
            {
                _auraAlphaTweens[i]?.Kill();
                _auraAlphaTweens[i] = null;
            }
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        KillBreathingAnimation();
        KillAuraTweens();
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillBreathingAnimation();
        KillAuraTweens();
    }
}