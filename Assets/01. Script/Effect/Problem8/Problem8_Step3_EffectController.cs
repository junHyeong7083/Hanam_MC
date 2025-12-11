using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 8 - Step 3 첫 장면 결정 이펙트 컨트롤러
/// - 인트로 카드 등장
/// - 액션 선택 애니메이션 (호버, 탭, 선택 글로우)
/// - 녹음 버튼 + 녹음 중 애니메이션
/// - 결과 화면 (비디오 아이콘, 메시지들)
/// </summary>
public class Problem8_Step3_EffectController : EffectControllerBase
{
    [Header("===== 인트로 카드 =====")]
    [SerializeField] private RectTransform instructionCardRect;
    [SerializeField] private CanvasGroup instructionCardCanvasGroup;
    [SerializeField] private RectTransform speechBubbleRect;
    [SerializeField] private CanvasGroup speechBubbleCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 액션 선택 =====")]
    [SerializeField] private float actionHoverScale = 1.02f;
    [SerializeField] private float actionHoverX = 10f;
    [SerializeField] private float actionTapScale = 0.98f;
    [SerializeField] private float actionSelectDuration = 0.2f;

    [Header("===== 선택된 액션 글로우 =====")]
    [SerializeField] private float selectedGlowMinAlpha = 0.1f;
    [SerializeField] private float selectedGlowMaxAlpha = 0.3f;
    [SerializeField] private float selectedGlowDuration = 2f;

    [Header("===== 선택된 이모지 펄스 =====")]
    [SerializeField] private float emojiPulseMinScale = 1f;
    [SerializeField] private float emojiPulseMaxScale = 1.2f;
    [SerializeField] private float emojiPulseDuration = 1.5f;

    [Header("===== 마이크 버튼 =====")]
    [SerializeField] private RectTransform micButtonRect;
    [SerializeField] private CanvasGroup micButtonCanvasGroup;
    [SerializeField] private RectTransform micPromptRect;
    [SerializeField] private CanvasGroup micPromptCanvasGroup;

    [Header("===== 녹음 중 =====")]
    [SerializeField] private GameObject recordingRoot;
    [SerializeField] private RectTransform recordingMicRect;
    [SerializeField] private CanvasGroup recordingCanvasGroup;
    [SerializeField] private Image recordingDotImage;
    [SerializeField] private Text listeningText;           // "듣고 있어요..." 텍스트
    [SerializeField] private Text selectedActionText;      // 선택한 액션 텍스트
    [SerializeField] private RectTransform recordingTextRect;
    [SerializeField] private float recordingMicPulseScale = 1.1f;

    [Header("===== 결과 화면 =====")]
    [SerializeField] private RectTransform resultCardRect;
    [SerializeField] private CanvasGroup resultCardCanvasGroup;

    [Header("===== 결과 - 비디오 아이콘 =====")]
    [SerializeField] private RectTransform videoIconRect;
    [SerializeField] private float iconBounceDistance = 10f;
    [SerializeField] private float iconBounceDuration = 2f;

    [Header("===== 결과 - 메시지들 =====")]
    [SerializeField] private RectTransform successMessageRect;
    [SerializeField] private CanvasGroup successMessageCanvasGroup;
    [SerializeField] private RectTransform badgeRect;
    [SerializeField] private CanvasGroup badgeCanvasGroup;
    [SerializeField] private RectTransform promiseCardRect;
    [SerializeField] private CanvasGroup promiseCardCanvasGroup;
    [SerializeField] private RectTransform storyboardUpdateRect;
    [SerializeField] private CanvasGroup storyboardUpdateCanvasGroup;
    [SerializeField] private float resultSlideDistance = 20f;

    // 루프 트윈들
    private Tween _selectedGlowTween;
    private Tween _emojiPulseTween;
    private Tween _micPromptTween;
    private Tween _recordingMicTween;
    private Tween _recordingDotTween;
    private Tween _recordingTextTween;
    private Tween _iconBounceTween;

    #region Public API - 인트로

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 인스트럭션 카드 슬라이드 업 + 페이드
        if (instructionCardRect != null && instructionCardCanvasGroup != null)
        {
            Vector2 basePos = instructionCardRect.anchoredPosition;
            instructionCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            instructionCardCanvasGroup.alpha = 0f;

            seq.Append(instructionCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(instructionCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 2. 말풍선 슬라이드 (딜레이 0.3초)
        if (speechBubbleRect != null && speechBubbleCanvasGroup != null)
        {
            Vector2 basePos = speechBubbleRect.anchoredPosition;
            speechBubbleRect.anchoredPosition = basePos + Vector2.left * 20f;
            speechBubbleCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, speechBubbleRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, speechBubbleCanvasGroup.DOFade(1f, introAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 액션 버튼들 순차 등장
    /// </summary>
    public void PlayActionButtonsAppear(RectTransform[] actionRects, CanvasGroup[] actionCanvasGroups, Action onComplete = null)
    {
        if (actionRects == null || actionRects.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        for (int i = 0; i < actionRects.Length; i++)
        {
            if (actionRects[i] == null) continue;

            Vector2 basePos = actionRects[i].anchoredPosition;
            actionRects[i].anchoredPosition = basePos + Vector2.left * 30f;

            if (actionCanvasGroups != null && i < actionCanvasGroups.Length && actionCanvasGroups[i] != null)
                actionCanvasGroups[i].alpha = 0f;

            float delay = 0.5f + i * 0.1f;

            seq.Insert(delay, actionRects[i]
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));

            if (actionCanvasGroups != null && i < actionCanvasGroups.Length && actionCanvasGroups[i] != null)
                seq.Insert(delay, actionCanvasGroups[i].DOFade(1f, introAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 액션 선택

    /// <summary>
    /// 액션 호버
    /// </summary>
    public void PlayActionHover(RectTransform actionRect)
    {
        if (actionRect == null) return;
        actionRect.DOScale(actionHoverScale, 0.1f).SetEase(Ease.OutQuad);
        actionRect.DOAnchorPosX(actionRect.anchoredPosition.x + actionHoverX, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 액션 호버 해제
    /// </summary>
    public void PlayActionUnhover(RectTransform actionRect, Vector2 originalPos)
    {
        if (actionRect == null) return;
        actionRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
        actionRect.DOAnchorPos(originalPos, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 액션 탭
    /// </summary>
    public void PlayActionTap(RectTransform actionRect)
    {
        if (actionRect == null) return;
        actionRect.DOScale(actionTapScale, 0.05f).SetEase(Ease.OutQuad)
            .OnComplete(() => actionRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 액션 선택 (체크마크 등장)
    /// </summary>
    public void PlayActionSelect(RectTransform checkmarkRect, Action onComplete = null)
    {
        if (checkmarkRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        checkmarkRect.gameObject.SetActive(true);
        checkmarkRect.localScale = Vector3.zero;

        checkmarkRect
            .DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack, 2f)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 선택된 액션 글로우 펄스 시작
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
    /// 선택된 액션 글로우 펄스 정지
    /// </summary>
    public void StopSelectedGlowPulse()
    {
        _selectedGlowTween?.Kill();
        _selectedGlowTween = null;
    }

    /// <summary>
    /// 선택된 이모지 펄스 시작
    /// </summary>
    public void StartEmojiPulse(RectTransform emojiRect)
    {
        if (emojiRect == null) return;

        StopEmojiPulse();

        _emojiPulseTween = emojiRect
            .DOScale(emojiPulseMaxScale, emojiPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(Vector3.one * emojiPulseMinScale);
    }

    /// <summary>
    /// 선택된 이모지 펄스 정지
    /// </summary>
    public void StopEmojiPulse()
    {
        _emojiPulseTween?.Kill();
        _emojiPulseTween = null;
    }

    #endregion

    #region Public API - 마이크 버튼

    /// <summary>
    /// 마이크 버튼 등장
    /// </summary>
    public void ShowMicButton(Action onComplete = null)
    {
        if (micButtonRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        micButtonRect.gameObject.SetActive(true);

        var seq = DOTween.Sequence();

        // 버튼 등장
        Vector2 basePos = micButtonRect.anchoredPosition;
        micButtonRect.anchoredPosition = basePos + Vector2.down * 20f;

        if (micButtonCanvasGroup != null)
            micButtonCanvasGroup.alpha = 0f;

        seq.Append(micButtonRect
            .DOAnchorPos(basePos, introAppearDuration)
            .SetEase(Ease.OutQuad));

        if (micButtonCanvasGroup != null)
            seq.Join(micButtonCanvasGroup.DOFade(1f, introAppearDuration));

        // 프롬프트 텍스트 펄스 시작
        seq.OnComplete(() =>
        {
            StartMicPromptPulse();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 마이크 프롬프트 텍스트 펄스 시작
    /// </summary>
    public void StartMicPromptPulse()
    {
        if (micPromptCanvasGroup == null) return;

        StopMicPromptPulse();

        _micPromptTween = micPromptCanvasGroup
            .DOFade(1f, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(0.7f);
    }

    /// <summary>
    /// 마이크 프롬프트 텍스트 펄스 정지
    /// </summary>
    public void StopMicPromptPulse()
    {
        _micPromptTween?.Kill();
        _micPromptTween = null;
    }

    #endregion

    #region Public API - 녹음 중

    /// <summary>
    /// 녹음 시작 애니메이션 (선택한 액션 텍스트 포함)
    /// </summary>
    public void StartRecordingAnimation(string actionText = null)
    {
        StopRecordingAnimation();

        // 녹음 루트 활성화
        if (recordingRoot != null)
            recordingRoot.SetActive(true);

        // 선택한 액션 텍스트 설정
        if (selectedActionText != null && !string.IsNullOrEmpty(actionText))
            selectedActionText.text = $"\"{actionText}\"";

        // 녹음 화면 등장 애니메이션
        if (recordingCanvasGroup != null)
        {
            recordingCanvasGroup.alpha = 0f;
            recordingCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        }

        // 녹음 마이크 펄스
        if (recordingMicRect != null)
        {
            recordingMicRect.localScale = Vector3.one * 0.8f;
            recordingMicRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            _recordingMicTween = recordingMicRect
                .DOScale(recordingMicPulseScale, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(0.3f);
        }

        // 녹음 점 깜빡임
        if (recordingDotImage != null)
        {
            _recordingDotTween = recordingDotImage
                .DOFade(0.3f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(1f);
        }

        // 텍스트 펄스
        if (recordingTextRect != null)
        {
            _recordingTextTween = recordingTextRect
                .DOScale(1.05f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// 녹음 정지 애니메이션
    /// </summary>
    public void StopRecordingAnimation()
    {
        _recordingMicTween?.Kill();
        _recordingDotTween?.Kill();
        _recordingTextTween?.Kill();

        _recordingMicTween = null;
        _recordingDotTween = null;
        _recordingTextTween = null;

        // 녹음 루트 비활성화
        if (recordingRoot != null)
            recordingRoot.SetActive(false);

        // 마이크 스케일 리셋
        if (recordingMicRect != null)
        {
            DOTween.Kill(recordingMicRect);
            recordingMicRect.localScale = Vector3.one;
        }
    }

    #endregion

    #region Public API - 결과 화면

    /// <summary>
    /// 결과 화면 애니메이션
    /// </summary>
    public void PlayResultAnimation(Action onComplete = null)
    {
        StopRecordingAnimation();
        StopSelectedGlowPulse();
        StopEmojiPulse();
        StopMicPromptPulse();

        var seq = CreateSequence();

        // 1. 결과 카드 등장
        if (resultCardRect != null && resultCardCanvasGroup != null)
        {
            resultCardRect.localScale = Vector3.one * 0.9f;
            resultCardCanvasGroup.alpha = 0f;

            seq.Append(resultCardRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Join(resultCardCanvasGroup.DOFade(1f, 0.5f));
        }

        // 2. 비디오 아이콘 스프링 등장
        if (videoIconRect != null)
        {
            videoIconRect.localScale = Vector3.zero;
            videoIconRect.localRotation = Quaternion.Euler(0, 0, -180);

            seq.Insert(0.3f, videoIconRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutBack, 2f));
            seq.Insert(0.3f, videoIconRect
                .DORotate(Vector3.zero, 0.5f)
                .SetEase(Ease.OutBack));

            // 바운스 시작
            seq.InsertCallback(0.8f, StartIconBounce);
        }

        // 3. 성공 메시지 (딜레이 0.5초)
        if (successMessageRect != null && successMessageCanvasGroup != null)
        {
            Vector2 basePos = successMessageRect.anchoredPosition;
            successMessageRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            successMessageCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, successMessageRect
                .DOAnchorPos(basePos, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, successMessageCanvasGroup.DOFade(1f, 0.5f));
        }

        // 4. 뱃지 (딜레이 0.7초)
        if (badgeRect != null && badgeCanvasGroup != null)
        {
            Vector2 basePos = badgeRect.anchoredPosition;
            badgeRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            badgeCanvasGroup.alpha = 0f;

            seq.Insert(0.7f, badgeRect
                .DOAnchorPos(basePos, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.7f, badgeCanvasGroup.DOFade(1f, 0.5f));
        }

        // 5. 약속 카드 (딜레이 1초)
        if (promiseCardRect != null && promiseCardCanvasGroup != null)
        {
            promiseCardRect.localScale = Vector3.one * 0.8f;
            promiseCardCanvasGroup.alpha = 0f;

            seq.Insert(1f, promiseCardRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Insert(1f, promiseCardCanvasGroup.DOFade(1f, 0.5f));
        }

        // 6. 스토리보드 업데이트 텍스트 (딜레이 1.5초)
        if (storyboardUpdateRect != null && storyboardUpdateCanvasGroup != null)
        {
            Vector2 basePos = storyboardUpdateRect.anchoredPosition;
            storyboardUpdateRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            storyboardUpdateCanvasGroup.alpha = 0f;

            seq.Insert(1.5f, storyboardUpdateRect
                .DOAnchorPos(basePos, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Insert(1.5f, storyboardUpdateCanvasGroup.DOFade(1f, 0.5f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 아이콘 바운스 시작
    /// </summary>
    private void StartIconBounce()
    {
        if (videoIconRect == null) return;

        StopIconBounce();

        Vector2 basePos = videoIconRect.anchoredPosition;
        _iconBounceTween = videoIconRect
            .DOAnchorPosY(basePos.y + iconBounceDistance, iconBounceDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 아이콘 바운스 정지
    /// </summary>
    private void StopIconBounce()
    {
        _iconBounceTween?.Kill();
        _iconBounceTween = null;
    }

    #endregion

    #region Reset

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        StopSelectedGlowPulse();
        StopEmojiPulse();
        StopMicPromptPulse();
        StopRecordingAnimation();
        StopIconBounce();

        // 인트로 카드 리셋
        if (instructionCardCanvasGroup != null)
        {
            DOTween.Kill(instructionCardRect);
            DOTween.Kill(instructionCardCanvasGroup);
            instructionCardCanvasGroup.alpha = 0f;
        }

        if (speechBubbleCanvasGroup != null)
        {
            DOTween.Kill(speechBubbleRect);
            DOTween.Kill(speechBubbleCanvasGroup);
            speechBubbleCanvasGroup.alpha = 0f;
        }

        // 마이크 버튼 리셋
        if (micButtonRect != null)
        {
            DOTween.Kill(micButtonRect);
            micButtonRect.gameObject.SetActive(false);
        }

        // 녹음 리셋
        if (recordingMicRect != null)
        {
            DOTween.Kill(recordingMicRect);
            recordingMicRect.localScale = Vector3.one;
        }

        // 결과 화면 리셋
        if (resultCardCanvasGroup != null)
        {
            DOTween.Kill(resultCardRect);
            DOTween.Kill(resultCardCanvasGroup);
            resultCardCanvasGroup.alpha = 0f;
        }

        if (videoIconRect != null)
        {
            DOTween.Kill(videoIconRect);
            videoIconRect.localScale = Vector3.zero;
        }

        if (successMessageCanvasGroup != null)
        {
            DOTween.Kill(successMessageRect);
            DOTween.Kill(successMessageCanvasGroup);
            successMessageCanvasGroup.alpha = 0f;
        }

        if (badgeCanvasGroup != null)
        {
            DOTween.Kill(badgeRect);
            DOTween.Kill(badgeCanvasGroup);
            badgeCanvasGroup.alpha = 0f;
        }

        if (promiseCardCanvasGroup != null)
        {
            DOTween.Kill(promiseCardRect);
            DOTween.Kill(promiseCardCanvasGroup);
            promiseCardCanvasGroup.alpha = 0f;
        }

        if (storyboardUpdateCanvasGroup != null)
        {
            DOTween.Kill(storyboardUpdateRect);
            DOTween.Kill(storyboardUpdateCanvasGroup);
            storyboardUpdateCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopSelectedGlowPulse();
        StopEmojiPulse();
        StopMicPromptPulse();
        StopRecordingAnimation();
        StopIconBounce();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopSelectedGlowPulse();
        StopEmojiPulse();
        StopMicPromptPulse();
        StopRecordingAnimation();
        StopIconBounce();
    }
}
