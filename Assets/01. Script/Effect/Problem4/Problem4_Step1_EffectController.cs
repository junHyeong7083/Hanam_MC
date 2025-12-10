using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem4 Step1: Effect Controller
/// - 인벤토리에서 '가위' 드래그하여 필름에 드롭하는 이펙트 시퀀스 관리
/// - 드롭 인디케이터, 활성화 애니메이션, 완료 스파클 등 타이밍 조율
/// - 로직과 애니메이션 분리를 위한 중앙 관리자
///
/// 흐름:
/// 1. 드래그 시작 → 드롭 인디케이터 표시 (DropZoneIndicator가 펄스 처리)
/// 2. 드롭 성공 → 필름 활성화 애니메이션 (스케일 업/다운)
/// 3. 활성화 완료 → 완료 스파클 표시
/// 4. 딜레이 후 → 완료 콜백
/// </summary>
public class Problem4_Step1_EffectController : EffectControllerBase
{
    [Header("===== 드롭 인디케이터 =====")]
    [SerializeField] private GameObject dropIndicatorRoot;

    [Header("===== 필름 비주얼 =====")]
    [SerializeField] private RectTransform filmVisualRoot;

    [Header("===== 활성화 애니메이션 =====")]
    [SerializeField] private float activateScale = 1.05f;
    [SerializeField] private float activateDuration = 0.6f;

    [Header("===== 완료 스파클 (옵션) =====")]
    [SerializeField] private GameObject completionSparkle;
    [SerializeField] private float sparkleDelay = 0.1f;

    [Header("===== 안내 텍스트 =====")]
    [SerializeField] private GameObject instructionRoot;
    [SerializeField] private float instructionFadeOutDuration = 0.2f;
    [SerializeField] private CanvasGroup instructionCanvasGroup;

    // 초기 스케일 저장
    private Vector3 _filmBaseScale;
    private bool _filmScaleSaved;

    #region Public API

    /// <summary>
    /// 드래그 시작 시 드롭 인디케이터 표시
    /// </summary>
    public void ShowDropIndicator()
    {
        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(true);
    }

    /// <summary>
    /// 드래그 종료 시 드롭 인디케이터 숨김
    /// </summary>
    public void HideDropIndicator()
    {
        if (dropIndicatorRoot != null)
            dropIndicatorRoot.SetActive(false);
    }

    /// <summary>
    /// 드롭 성공 시 활성화 시퀀스 시작
    /// </summary>
    public void PlayActivateSequence(Action onComplete = null)
    {
        if (IsAnimating) return;

        // 필름 기본 스케일 저장
        if (!_filmScaleSaved && filmVisualRoot != null)
        {
            _filmBaseScale = filmVisualRoot.localScale;
            _filmScaleSaved = true;
        }

        // 드롭 인디케이터 숨김
        HideDropIndicator();

        var seq = CreateSequence();

        // 1. 안내 텍스트 페이드아웃
        if (instructionCanvasGroup != null && instructionFadeOutDuration > 0f)
        {
            seq.Append(instructionCanvasGroup.DOFade(0f, instructionFadeOutDuration));
            seq.AppendCallback(HideInstructionImmediate);
        }
        else
        {
            seq.AppendCallback(HideInstructionImmediate);
        }

        // 2. 필름 스케일 업/다운 (펀치)
        if (filmVisualRoot != null)
        {
            float halfDuration = activateDuration * 0.5f;
            seq.Append(filmVisualRoot.DOScale(_filmBaseScale * activateScale, halfDuration).SetEase(Ease.OutQuad));
            seq.Append(filmVisualRoot.DOScale(_filmBaseScale, halfDuration).SetEase(Ease.InQuad));
        }

        // 3. 스파클 딜레이 후 표시
        if (sparkleDelay > 0f)
            seq.AppendInterval(sparkleDelay);

        seq.AppendCallback(ShowSparkleImmediate);

        // 완료 콜백
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 다음 단계로 넘어갈 때 리셋
    /// </summary>
    public void ResetForNextStep()
    {
        KillCurrentSequence();

        // 드롭 인디케이터 숨김
        HideDropIndicator();

        // 안내 텍스트 표시
        if (instructionRoot != null)
            instructionRoot.SetActive(true);

        if (instructionCanvasGroup != null)
            instructionCanvasGroup.alpha = 1f;

        // 필름 스케일 원래대로
        if (filmVisualRoot != null && _filmScaleSaved)
            filmVisualRoot.localScale = _filmBaseScale;

        // 스파클 숨김
        HideSparkle();
    }

    /// <summary>
    /// 안내 텍스트 즉시 숨김
    /// </summary>
    public void HideInstructionImmediate()
    {
        if (instructionRoot != null)
            instructionRoot.SetActive(false);

        if (instructionCanvasGroup != null)
            instructionCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 스파클 즉시 표시
    /// </summary>
    public void ShowSparkleImmediate()
    {
        if (completionSparkle != null)
        {
            completionSparkle.SetActive(true);

            // PopupSpring이 붙어있으면 자동 애니메이션
            var popupSpring = completionSparkle.GetComponent<PopupSpring>();
            if (popupSpring != null)
                popupSpring.Play();
        }
    }

    /// <summary>
    /// 스파클 숨김
    /// </summary>
    public void HideSparkle()
    {
        if (completionSparkle != null)
            completionSparkle.SetActive(false);
    }

    #endregion
}
