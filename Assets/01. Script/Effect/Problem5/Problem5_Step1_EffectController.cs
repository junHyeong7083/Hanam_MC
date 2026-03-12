using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem5_Step1_EffectController - 문제5 스텝1(클로즈업 팝업)의 이펙트 관리자
///
/// 【역할】 드래그&드롭 성공 후 클로즈업 이미지를 팝업으로 표시한다.
///          startScale→maxScale→finalScale 3단계 스케일 애니메이션으로 등장.
///          팝업 시 드롭 타겟을 자동으로 숨긴다.
/// 【사용 위치】 ProblemScene - Problem5 Step1 (인벤토리 아이템 드롭 후 클로즈업 스텝)
/// 【트리거】 Logic 클래스에서 PlayCloseUpPopup() 호출
/// 【의존성】 EffectControllerBase(상속), DOTween, closeUpRoot, dropTargetRoot
/// </summary>
public class Problem5_Step1_EffectController : EffectControllerBase
{
    [Header("===== 클로즈업 팝업 연출 =====")]
    [SerializeField] private RectTransform closeUpRoot;
    [SerializeField] private float popupStartScale = 0.3f;
    [SerializeField] private float popupMaxScale = 1.1f;
    [SerializeField] private float popupFinalScale = 1.0f;
    [SerializeField] private float popupGrowDuration = 0.3f;
    [SerializeField] private float popupShrinkDuration = 0.2f;

    [Header("===== 드롭 타겟 (팝업 시 숨김) =====")]
    [SerializeField] private GameObject dropTargetRoot;

    #region Public API

    /// <summary>
    /// 클로즈업 팝업 애니메이션 재생
    /// - dropTarget 숨김
    /// - closeUpRoot: startScale → maxScale → finalScale
    /// </summary>
    public void PlayCloseUpPopup(Action onComplete = null)
    {
        // 드롭 타겟 숨김
        if (dropTargetRoot != null)
            dropTargetRoot.SetActive(false);

        if (closeUpRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        closeUpRoot.localScale = Vector3.one * popupStartScale;
        closeUpRoot.gameObject.SetActive(true);

        var seq = CreateSequence();

        // startScale → maxScale (빠르게 커짐)
        seq.Append(closeUpRoot.DOScale(popupMaxScale, popupGrowDuration).SetEase(Ease.OutBack));

        // maxScale → finalScale (살짝 줄어들며 안정)
        seq.Append(closeUpRoot.DOScale(popupFinalScale, popupShrinkDuration).SetEase(Ease.OutQuad));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 초기 상태로 리셋
    /// </summary>
    public void ResetToInitial()
    {
        KillCurrentSequence();

        if (closeUpRoot != null)
        {
            closeUpRoot.localScale = Vector3.zero;
            closeUpRoot.gameObject.SetActive(false);
        }

        if (dropTargetRoot != null)
            dropTargetRoot.SetActive(true);
    }

    #endregion
}
