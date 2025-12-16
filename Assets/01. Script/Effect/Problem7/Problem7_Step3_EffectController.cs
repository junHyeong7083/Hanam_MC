using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 7 - Step 3 명대사 녹음 이펙트 컨트롤러
/// - 대사 선택 애니메이션
/// - 녹음 중 애니메이션 (메가폰 흔들림, 음파, 글로우)
/// - 결과 화면 (캐릭터 등장, 스파클, 황금빛)
/// </summary>
public class Problem7_Step3_EffectController : EffectControllerBase
{
    [Header("===== 대사 선택 =====")]
    [SerializeField] private float dialogueHoverScale = 1.02f;
    [SerializeField] private float dialogueTapScale = 0.98f;
    [SerializeField] private float dialogueSelectDuration = 0.2f;

    [Header("===== 선택된 대사 글로우 =====")]
    [SerializeField] private float selectedGlowMinAlpha = 0.1f;
    [SerializeField] private float selectedGlowMaxAlpha = 0.3f;
    [SerializeField] private float selectedGlowDuration = 2f;

    [Header("===== 마이크 아이콘 펄스 =====")]
    [SerializeField] private float micPulseMinScale = 1f;
    [SerializeField] private float micPulseMaxScale = 1.2f;
    [SerializeField] private float micPulseDuration = 1.5f;

    [Header("===== 녹음 중 - 메가폰 =====")]
    [SerializeField] private RectTransform megaphoneRect;
    [SerializeField] private float megaphoneWobbleAngle = 8f;
    [SerializeField] private float megaphoneWobbleDuration = 0.6f;
    [SerializeField] private float megaphoneScaleMin = 1f;
    [SerializeField] private float megaphoneScaleMax = 1.1f;

    [Header("===== 녹음 중 - 음파 링 =====")]
    [SerializeField] private RectTransform[] soundWaveRings;
    [SerializeField] private float waveExpandScale = 3f;
    [SerializeField] private float waveExpandDuration = 2f;

    [Header("===== 녹음 중 - 글로우 =====")]
    [SerializeField] private RectTransform recordingGlowRect;
    [SerializeField] private CanvasGroup recordingGlowCanvasGroup;
    [SerializeField] private float recordingGlowMinScale = 1f;
    [SerializeField] private float recordingGlowMaxScale = 1.5f;
    [SerializeField] private float recordingGlowMinAlpha = 0.3f;
    [SerializeField] private float recordingGlowMaxAlpha = 0.6f;

    [Header("===== 녹음 중 - 텍스트 =====")]
    [SerializeField] private RectTransform dialogueTextRect;
    [SerializeField] private float textPulseMinScale = 1f;
    [SerializeField] private float textPulseMaxScale = 1.08f;
    [SerializeField] private float textPulseDuration = 1f;

    [Header("===== 녹음 중 - 카메라 쉐이크 =====")]
    [SerializeField] private RectTransform shakeTargetRect;
    [SerializeField] private float shakeStrength = 3f;
    [SerializeField] private float shakeDuration = 0.5f;

    [Header("===== 결과 화면 - 캐릭터 =====")]
    [SerializeField] private RectTransform resultCharacterRect;
    [SerializeField] private float characterSlideDistance = 50f;
    [SerializeField] private float characterAppearDuration = 1.5f;
    [SerializeField] private float characterBounceDistance = 10f;
    [SerializeField] private float characterBounceDuration = 2f;

    [Header("===== 결과 화면 - 황금빛 =====")]
    [SerializeField] private RectTransform goldenGlowRect;
    [SerializeField] private CanvasGroup goldenGlowCanvasGroup;
    [SerializeField] private float goldenGlowTargetScale = 2f;
    [SerializeField] private float goldenGlowTargetAlpha = 0.4f;

    [Header("===== 결과 화면 - 텍스트/버튼 =====")]
    [SerializeField] private RectTransform resultBadgeRect;
    [SerializeField] private CanvasGroup resultBadgeCanvasGroup;
    [SerializeField] private RectTransform resultTextRect;
    [SerializeField] private CanvasGroup resultTextCanvasGroup;
    [SerializeField] private RectTransform resultButtonRect;
    [SerializeField] private CanvasGroup resultButtonCanvasGroup;
    [SerializeField] private float resultSlideDistance = 20f;
    [SerializeField] private float resultFadeDuration = 0.5f;

    // 루프 트윈들
    private Tween _selectedGlowTween;
    private Tween _micPulseTween;
    private Tween _megaphoneWobbleTween;
    private Tween _megaphoneScaleTween;
    private Tween[] _waveRingTweens;
    private Tween _recordingGlowScaleTween;
    private Tween _recordingGlowAlphaTween;
    private Tween _textPulseTween;
    private Tween _shakeTween;
    private Tween _characterBounceTween;
    private Vector2 _shakeOriginalPos;

    #region Public API - 대사 선택

    /// <summary>
    /// 대사 호버
    /// </summary>
    public void PlayDialogueHover(RectTransform dialogueRect)
    {
        if (dialogueRect == null) return;
        dialogueRect.DOScale(dialogueHoverScale, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 대사 호버 해제
    /// </summary>
    public void PlayDialogueUnhover(RectTransform dialogueRect)
    {
        if (dialogueRect == null) return;
        dialogueRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 대사 선택
    /// </summary>
    public void PlayDialogueSelect(RectTransform dialogueRect, RectTransform checkmarkRect = null, Action onComplete = null)
    {
        if (dialogueRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        seq.Append(dialogueRect
            .DOScale(dialogueTapScale, dialogueSelectDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        seq.Append(dialogueRect
            .DOScale(1f, dialogueSelectDuration * 0.7f)
            .SetEase(Ease.OutBack));

        if (checkmarkRect != null)
        {
            checkmarkRect.gameObject.SetActive(true);
            checkmarkRect.localScale = Vector3.zero;
            seq.Insert(dialogueSelectDuration * 0.5f, checkmarkRect
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack, 2f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 선택된 대사 글로우 펄스 시작
    /// </summary>
    public void StartSelectedGlowPulse(CanvasGroup glowCanvasGroup)
    {
        if (glowCanvasGroup == null) return;

        StopSelectedGlowPulse();

        glowCanvasGroup.gameObject.SetActive(true);
        glowCanvasGroup.alpha = selectedGlowMinAlpha;

        _selectedGlowTween = glowCanvasGroup
            .DOFade(selectedGlowMaxAlpha, selectedGlowDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 선택된 대사 글로우 펄스 정지
    /// </summary>
    public void StopSelectedGlowPulse()
    {
        _selectedGlowTween?.Kill();
        _selectedGlowTween = null;
    }

    /// <summary>
    /// 마이크 아이콘 펄스 시작
    /// </summary>
    public void StartMicPulse(RectTransform micIconRect)
    {
        if (micIconRect == null) return;

        StopMicPulse();

        _micPulseTween = micIconRect
            .DOScale(micPulseMaxScale, micPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(Vector3.one * micPulseMinScale);
    }

    /// <summary>
    /// 마이크 아이콘 펄스 정지
    /// </summary>
    public void StopMicPulse()
    {
        _micPulseTween?.Kill();
        _micPulseTween = null;
    }

    #endregion

    #region Public API - 녹음 중

    /// <summary>
    /// 녹음 애니메이션 시작
    /// </summary>
    public void StartRecordingAnimation()
    {
        StopRecordingAnimation();

        // 메가폰 흔들림: rotate [-8, 8]
        if (megaphoneRect != null)
        {
            _megaphoneWobbleTween = megaphoneRect
                .DORotate(new Vector3(0, 0, megaphoneWobbleAngle), megaphoneWobbleDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(new Vector3(0, 0, -megaphoneWobbleAngle));

            _megaphoneScaleTween = megaphoneRect
                .DOScale(megaphoneScaleMax, megaphoneWobbleDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(Vector3.one * megaphoneScaleMin);
        }

        // 음파 링 확장
        if (soundWaveRings != null && soundWaveRings.Length > 0)
        {
            _waveRingTweens = new Tween[soundWaveRings.Length];

            for (int i = 0; i < soundWaveRings.Length; i++)
            {
                if (soundWaveRings[i] == null) continue;

                int index = i;
                soundWaveRings[index].gameObject.SetActive(true);

                var seq = DOTween.Sequence();
                seq.Append(soundWaveRings[index]
                    .DOScale(waveExpandScale, waveExpandDuration)
                    .SetEase(Ease.OutQuad)
                    .From(Vector3.zero));

                var cg = soundWaveRings[index].GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    seq.Join(cg.DOFade(0f, waveExpandDuration).From(1f));
                }

                seq.SetLoops(-1);
                seq.SetDelay(i * 0.3f);

                _waveRingTweens[index] = seq;
            }
        }

        // 녹음 글로우 펄스
        if (recordingGlowRect != null)
        {
            recordingGlowRect.gameObject.SetActive(true);
            _recordingGlowScaleTween = recordingGlowRect
                .DOScale(recordingGlowMaxScale, 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(Vector3.one * recordingGlowMinScale);
        }

        if (recordingGlowCanvasGroup != null)
        {
            _recordingGlowAlphaTween = recordingGlowCanvasGroup
                .DOFade(recordingGlowMinAlpha, 1.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(recordingGlowMaxAlpha);
        }

        // 대사 텍스트 펄스
        if (dialogueTextRect != null)
        {
            _textPulseTween = dialogueTextRect
                .DOScale(textPulseMaxScale, textPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(Vector3.one * textPulseMinScale);
        }

        // 카메라 쉐이크 (약하게 반복)
        if (shakeTargetRect != null)
        {
            _shakeOriginalPos = shakeTargetRect.anchoredPosition;
            _shakeTween = shakeTargetRect
                .DOShakeAnchorPos(shakeDuration, shakeStrength, 10, 90f, false, true, ShakeRandomnessMode.Harmonic)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    /// <summary>
    /// 녹음 애니메이션 정지
    /// </summary>
    public void StopRecordingAnimation()
    {
        _megaphoneWobbleTween?.Kill();
        _megaphoneScaleTween?.Kill();
        _recordingGlowScaleTween?.Kill();
        _recordingGlowAlphaTween?.Kill();
        _textPulseTween?.Kill();
        _shakeTween?.Kill();

        _megaphoneWobbleTween = null;
        _megaphoneScaleTween = null;
        _recordingGlowScaleTween = null;
        _recordingGlowAlphaTween = null;
        _textPulseTween = null;
        _shakeTween = null;

        if (_waveRingTweens != null)
        {
            for (int i = 0; i < _waveRingTweens.Length; i++)
            {
                _waveRingTweens[i]?.Kill();
                _waveRingTweens[i] = null;
            }
        }

        // 음파 링 숨김
        if (soundWaveRings != null)
        {
            foreach (var ring in soundWaveRings)
            {
                if (ring != null)
                    ring.gameObject.SetActive(false);
            }
        }

        // 녹음 글로우 숨김
        if (recordingGlowRect != null)
            recordingGlowRect.gameObject.SetActive(false);

        // 쉐이크 위치 복원
        if (shakeTargetRect != null)
            shakeTargetRect.anchoredPosition = _shakeOriginalPos;
    }

    #endregion

    #region Public API - 결과 화면

    /// <summary>
    /// 결과 화면 애니메이션
    /// </summary>
    public void PlayResultAnimation(Action onComplete = null)
    {
        StopRecordingAnimation();

        var seq = CreateSequence();

        // 1. 황금빛 확장
        if (goldenGlowRect != null && goldenGlowCanvasGroup != null)
        {
            goldenGlowRect.gameObject.SetActive(true);
            goldenGlowRect.localScale = Vector3.zero;
            goldenGlowCanvasGroup.alpha = 0f;

            seq.Append(goldenGlowRect
                .DOScale(goldenGlowTargetScale, 1f)
                .SetEase(Ease.OutQuad));

            seq.Join(goldenGlowCanvasGroup
                .DOFade(goldenGlowTargetAlpha, 1f));
        }

        // 2. 캐릭터 슬라이드 업 + 등장
        if (resultCharacterRect != null)
        {
            Vector2 basePos = resultCharacterRect.anchoredPosition;
            resultCharacterRect.anchoredPosition = basePos + Vector2.down * characterSlideDistance;

            var charCG = resultCharacterRect.GetComponent<CanvasGroup>();
            if (charCG != null) charCG.alpha = 0f;

            seq.Insert(0f, resultCharacterRect
                .DOAnchorPos(basePos, characterAppearDuration)
                .SetEase(Ease.OutQuad));

            if (charCG != null)
                seq.Insert(0f, charCG.DOFade(1f, characterAppearDuration * 0.5f));

            // 바운스 시작
            seq.InsertCallback(characterAppearDuration, () =>
            {
                _characterBounceTween = resultCharacterRect
                    .DOAnchorPosY(basePos.y + characterBounceDistance, characterBounceDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            });
        }

        // 3. 뱃지 등장
        if (resultBadgeRect != null && resultBadgeCanvasGroup != null)
        {
            Vector2 basePos = resultBadgeRect.anchoredPosition;
            resultBadgeRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            resultBadgeCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, resultBadgeRect
                .DOAnchorPos(basePos, resultFadeDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, resultBadgeCanvasGroup.DOFade(1f, resultFadeDuration));
        }

        // 4. 텍스트 등장
        if (resultTextRect != null && resultTextCanvasGroup != null)
        {
            Vector2 basePos = resultTextRect.anchoredPosition;
            resultTextRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            resultTextCanvasGroup.alpha = 0f;

            seq.Insert(0.7f, resultTextRect
                .DOAnchorPos(basePos, resultFadeDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.7f, resultTextCanvasGroup.DOFade(1f, resultFadeDuration));
        }

        // 5. 버튼 등장
        if (resultButtonRect != null && resultButtonCanvasGroup != null)
        {
            Vector2 basePos = resultButtonRect.anchoredPosition;
            resultButtonRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            resultButtonCanvasGroup.alpha = 0f;

            seq.Insert(1f, resultButtonRect
                .DOAnchorPos(basePos, resultFadeDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(1f, resultButtonCanvasGroup.DOFade(1f, resultFadeDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Reset

    public void ResetAll()
    {
        KillCurrentSequence();
        StopSelectedGlowPulse();
        StopMicPulse();
        StopRecordingAnimation();

        _characterBounceTween?.Kill();
        _characterBounceTween = null;

        // 메가폰 리셋
        if (megaphoneRect != null)
        {
            DOTween.Kill(megaphoneRect);
            megaphoneRect.localRotation = Quaternion.identity;
            megaphoneRect.localScale = Vector3.one;
        }

        // 황금빛 리셋
        if (goldenGlowRect != null)
        {
            DOTween.Kill(goldenGlowRect);
            goldenGlowRect.localScale = Vector3.zero;
            goldenGlowRect.gameObject.SetActive(false);
        }

        if (goldenGlowCanvasGroup != null)
        {
            DOTween.Kill(goldenGlowCanvasGroup);
            goldenGlowCanvasGroup.alpha = 0f;
        }

        // 결과 화면 리셋
        if (resultBadgeCanvasGroup != null)
        {
            DOTween.Kill(resultBadgeRect);
            DOTween.Kill(resultBadgeCanvasGroup);
            resultBadgeCanvasGroup.alpha = 0f;
        }

        if (resultTextCanvasGroup != null)
        {
            DOTween.Kill(resultTextRect);
            DOTween.Kill(resultTextCanvasGroup);
            resultTextCanvasGroup.alpha = 0f;
        }

        if (resultButtonCanvasGroup != null)
        {
            DOTween.Kill(resultButtonRect);
            DOTween.Kill(resultButtonCanvasGroup);
            resultButtonCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopSelectedGlowPulse();
        StopMicPulse();
        StopRecordingAnimation();
        _characterBounceTween?.Kill();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopSelectedGlowPulse();
        StopMicPulse();
        StopRecordingAnimation();
        _characterBounceTween?.Kill();
    }
}
