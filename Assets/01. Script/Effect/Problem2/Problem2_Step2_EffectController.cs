using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem2_Step2_EffectController - 문제2 스텝2(감정 조명 선택)의 시각 효과 관리자
///
/// 【역할】 감정 조명(라이트) 등장 시 스케일 0→peak→1 애니메이션과,
///          라벨(감정명) 등장 시 팝업 스프링 애니메이션을 관리한다.
///          PlaySlotReveal()로 라이트+라벨을 순차적으로 한번에 연출 가능.
/// 【사용 위치】 ProblemScene - Problem2 Step2 (감정 조명을 선택하는 스텝)
/// 【트리거】 Logic 클래스에서 PlayLightAppear(), PlayLabelAppear(), PlaySlotReveal() 호출
/// 【의존성】 EffectControllerBase(상속), DOTween
/// </summary>
public class Problem2_Step2_EffectController : EffectControllerBase
{
    [Header("===== 라이트 등장 애니메이션 =====")]
    [SerializeField] private float lightAppearDuration = 0.4f;
    [SerializeField] private float lightPeakScale = 1.2f;
    [SerializeField] private Ease lightAppearEase = Ease.OutBack;

    [Header("===== 라벨 등장 애니메이션 =====")]
    [SerializeField] private float labelAppearDuration = 0.3f;
    [SerializeField] private Ease labelAppearEase = Ease.OutBack;

    #region Public API

    /// <summary>
    /// 라이트 등장 애니메이션 (스케일 0 → peak → 1)
    /// </summary>
    public void PlayLightAppear(Transform target, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 초기 상태
        target.localScale = Vector3.zero;

        var seq = CreateSequence();
        seq.Append(target.DOScale(lightPeakScale, lightAppearDuration * 0.6f).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(1f, lightAppearDuration * 0.4f).SetEase(Ease.InOutQuad));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 라벨 등장 애니메이션 (팝업 스프링)
    /// </summary>
    public void PlayLabelAppear(Transform target, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        target.localScale = Vector3.zero;

        var seq = CreateSequence();
        seq.Append(target.DOScale(1f, labelAppearDuration).SetEase(labelAppearEase));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 슬롯 전체 활성화 애니메이션 (라이트 + 라벨 순차)
    /// </summary>
    public void PlaySlotReveal(Transform lightTarget, Transform labelTarget, Action onComplete = null)
    {
        var seq = CreateSequence();

        // 라이트 등장
        if (lightTarget != null)
        {
            lightTarget.localScale = Vector3.zero;
            seq.Append(lightTarget.DOScale(lightPeakScale, lightAppearDuration * 0.6f).SetEase(Ease.OutQuad));
            seq.Append(lightTarget.DOScale(1f, lightAppearDuration * 0.4f).SetEase(Ease.InOutQuad));
        }

        // 라벨 등장 (약간 딜레이 후)
        if (labelTarget != null)
        {
            labelTarget.localScale = Vector3.zero;
            labelTarget.gameObject.SetActive(true);
            seq.Append(labelTarget.DOScale(1f, labelAppearDuration).SetEase(labelAppearEase));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 즉시 리셋
    /// </summary>
    public void ResetImmediate(Transform lightTarget, Transform labelTarget)
    {
        KillCurrentSequence();

        if (lightTarget != null)
            lightTarget.localScale = Vector3.one;

        if (labelTarget != null)
        {
            labelTarget.localScale = Vector3.one;
            labelTarget.gameObject.SetActive(false);
        }
    }

    #endregion
}
