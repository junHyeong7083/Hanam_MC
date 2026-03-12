using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem5_Step3_EffectController - 문제5 스텝3(NPC 대화)의 이펙트 관리자
///
/// 【역할】 정답 선택 시 NPC 캐릭터 스케일 펄스(1→1.05→1) 후
///          NPC 응답 버블을 슬라이드 업 + 페이드인으로 등장시키는 순차 시퀀스 관리.
/// 【사용 위치】 ProblemScene - Problem5 Step3 (NPC 반응을 확인하는 스텝)
/// 【트리거】 Logic 클래스에서 PlayNpcResponseAppear() 호출
/// 【의존성】 EffectControllerBase(상속), DOTween, npcCharacterRect, npcResponseRect/CanvasGroup
/// </summary>
public class Problem5_Step3_EffectController : EffectControllerBase
{
    [Header("===== NPC 캐릭터 =====")]
    [SerializeField] private RectTransform npcCharacterRect;
    [SerializeField] private float characterPulseDuration = 0.5f;
    [SerializeField] private float characterPulseScale = 1.05f;

    [Header("===== NPC 응답 버블 =====")]
    [SerializeField] private RectTransform npcResponseRect;
    [SerializeField] private CanvasGroup npcResponseCanvasGroup;
    [SerializeField] private float responseSlideUpDistance = 20f;
    [SerializeField] private float responseFadeInDuration = 0.3f;

    // 초기값 저장
    private Vector3 _npcCharacterBaseScale;
    private Vector2 _npcResponseBasePos;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }

    #region Public API

    /// <summary>
    /// 초기 상태 저장
    /// </summary>
    public void SaveInitialState()
    {
        if (_initialized) return;

        if (npcCharacterRect != null)
            _npcCharacterBaseScale = npcCharacterRect.localScale;

        if (npcResponseRect != null)
            _npcResponseBasePos = npcResponseRect.anchoredPosition;

        _initialized = true;
    }

    /// <summary>
    /// NPC 반응 등장 애니메이션 재생 (정답 선택 시 호출)
    /// </summary>
    public void PlayNpcResponseAppear(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveInitialState();

        // 응답 버블 초기 상태 (아래에서 시작, 투명)
        if (npcResponseRect != null)
            npcResponseRect.anchoredPosition = _npcResponseBasePos + Vector2.down * responseSlideUpDistance;

        if (npcResponseCanvasGroup != null)
            npcResponseCanvasGroup.alpha = 0f;

        var seq = CreateSequence();

        // 1. NPC 캐릭터 펄스 (1 → 1.05 → 1)
        if (npcCharacterRect != null)
        {
            seq.Append(npcCharacterRect
                .DOScale(_npcCharacterBaseScale * characterPulseScale, characterPulseDuration * 0.5f)
                .SetEase(Ease.OutSine));
            seq.Append(npcCharacterRect
                .DOScale(_npcCharacterBaseScale, characterPulseDuration * 0.5f)
                .SetEase(Ease.InSine));
        }
        else
        {
            // 캐릭터가 없으면 대기 시간만 추가
            seq.AppendInterval(characterPulseDuration);
        }

        // 2. 응답 버블 슬라이드 업 + 페이드 인
        if (npcResponseRect != null)
        {
            seq.Append(npcResponseRect
                .DOAnchorPos(_npcResponseBasePos, responseFadeInDuration)
                .SetEase(Ease.OutQuad));
        }

        if (npcResponseCanvasGroup != null)
            seq.Join(npcResponseCanvasGroup.DOFade(1f, responseFadeInDuration));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 스텝 진입 시 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();

        if (npcCharacterRect != null && _initialized)
            npcCharacterRect.localScale = _npcCharacterBaseScale;

        if (npcResponseRect != null && _initialized)
            npcResponseRect.anchoredPosition = _npcResponseBasePos;

        if (npcResponseCanvasGroup != null)
            npcResponseCanvasGroup.alpha = 1f;
    }

    #endregion
}
