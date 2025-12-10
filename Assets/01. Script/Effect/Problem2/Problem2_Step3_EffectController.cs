using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem2 Step3: Effect Controller
/// - 버튼 페이드/비활성화 효과
/// - 마이크 버튼 등장 효과
/// - 완료 시 이펙트
/// </summary>
public class Problem2_Step3_EffectController : EffectControllerBase
{
    [Header("===== 버튼 비활성화 =====")]
    [SerializeField] private float buttonFadeDuration = 0.3f;
    [SerializeField] private float buttonDisabledAlpha = 0.3f;

    [Header("===== 마이크 버튼 등장 =====")]
    [SerializeField] private float micAppearDuration = 0.3f;
    [SerializeField] private Ease micAppearEase = Ease.OutBack;

    [Header("===== 완료 이펙트 =====")]
    [SerializeField] private float completePunchScale = 0.1f;
    [SerializeField] private float completePunchDuration = 0.3f;

    #region Public API

    /// <summary>
    /// 마이크 버튼 등장 애니메이션
    /// </summary>
    public void PlayMicAppear(GameObject micRoot, Action onComplete = null)
    {
        if (micRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        micRoot.SetActive(true);
        var rt = micRoot.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.zero;

            var seq = CreateSequence();
            seq.Append(rt.DOScale(1f, micAppearDuration).SetEase(micAppearEase));
            seq.OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 선택되지 않은 버튼들 페이드 아웃
    /// </summary>
    public void PlayButtonsFadeOut(Button[] buttons, int selectedIndex, Action onComplete = null)
    {
        if (buttons == null || buttons.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            if (i == selectedIndex) continue; // 선택된 건 유지

            var btn = buttons[i];
            btn.interactable = false;

            // 버튼의 모든 Graphic 페이드
            var graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g != null)
                {
                    seq.Join(g.DOFade(buttonDisabledAlpha, buttonFadeDuration));
                }
            }
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 모든 버튼 비활성화 + 페이드
    /// </summary>
    public void PlayAllButtonsDisable(Button[] buttons, Action onComplete = null)
    {
        if (buttons == null || buttons.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            btn.interactable = false;

            var graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g != null)
                {
                    seq.Join(g.DOFade(buttonDisabledAlpha, buttonFadeDuration));
                }
            }
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 카드 완료 시 스케일 펀치 이펙트
    /// </summary>
    public void PlayCompletePunch(RectTransform target, Action onComplete = null)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();
        seq.Append(target.DOPunchScale(Vector3.one * completePunchScale, completePunchDuration, 1, 0.5f));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 버튼 상태 즉시 리셋
    /// </summary>
    public void ResetButtons(Button[] buttons)
    {
        KillCurrentSequence();

        if (buttons == null) return;

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            btn.interactable = true;

            var graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                if (g != null)
                {
                    var c = g.color;
                    c.a = 1f;
                    g.color = c;
                }
            }
        }
    }

    #endregion
}
