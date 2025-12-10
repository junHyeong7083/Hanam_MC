using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem4 Step2: Effect Controller
/// - 필름 편집 화면의 이펙트 시퀀스 관리
/// - 카드 등장/컷/통과 애니메이션
/// - 색상 복원 애니메이션 (흑백 → 컬러)
/// - 프레임 스냅 + jitter로 오래된 필름 느낌
///
/// 흐름:
/// 1. 카드 등장: 좌→우 슬라이드 + 스냅 + jitter
/// 2. 컷: 가위 이동 → 카드 분리 → 떨어짐
/// 3. 통과: 우측 이동 + 페이드
/// 4. 완료: 전체 필름 흑백→컬러 전환
/// </summary>
public class Problem4_Step2_EffectController : EffectControllerBase
{
    [Header("===== 필름 카드 =====")]
    [SerializeField] private RectTransform filmCardRect;
    [SerializeField] private CanvasGroup filmCardCanvasGroup;
    [SerializeField] private GameObject filmCardRoot;

    [Header("===== 카드 등장 =====")]
    [SerializeField] private RectTransform appearStartPoint;
    [SerializeField] private float appearDuration = 0.4f;
    [SerializeField] private float snapJitterAmount = 3f;
    [SerializeField] private float snapJitterDuration = 0.08f;
    [SerializeField] private int snapJitterCount = 2;

    [Header("===== 가위 애니메이션 =====")]
    [SerializeField] private RectTransform scissorsRect;
    [SerializeField] private float scissorsMoveDuration = 0.3f;
    [SerializeField] private Vector2 scissorsOffset = new Vector2(0f, -150f);

    [Header("===== 카드 분리 (컷) =====")]
    [SerializeField] private RectTransform cardLeftRect;
    [SerializeField] private RectTransform cardRightRect;
    [SerializeField] private CanvasGroup cardLeftCanvas;
    [SerializeField] private CanvasGroup cardRightCanvas;
    [SerializeField] private float splitDuration = 0.5f;
    [SerializeField] private float splitHorizontalOffset = 80f;
    [SerializeField] private float splitFallDistance = 200f;
    [SerializeField] private float splitRotateAngle = 15f;

    [Header("===== 통과 애니메이션 =====")]
    [SerializeField] private RectTransform passTargetPoint;
    [SerializeField] private float passMoveDuration = 0.35f;

    [Header("===== 색상 복원 =====")]
    [SerializeField] private Image[] filmImages;
    [SerializeField] private Color grayscaleColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color restoredColor = Color.white;
    [SerializeField] private float colorRestoreDuration = 1.2f;
    [SerializeField] private float colorRestoreDelay = 0.3f;

    [Header("===== 완료 스파클 =====")]
    [SerializeField] private GameObject completionSparkleRoot;

    [Header("===== 에러 효과 =====")]
    [SerializeField] private float errorShakeDuration = 0.4f;
    [SerializeField] private float errorShakeAmount = 10f;

    // 카드 기본 위치
    private Vector2 _cardDefaultPos;
    private bool _defaultPosSaved;

    private void Awake()
    {
        SaveDefaultPosition();
    }

    #region Public API

    /// <summary>
    /// 카드 기본 위치 저장
    /// </summary>
    public void SaveDefaultPosition()
    {
        if (filmCardRect != null && !_defaultPosSaved)
        {
            _cardDefaultPos = filmCardRect.anchoredPosition;
            _defaultPosSaved = true;
        }
    }

    /// <summary>
    /// 카드 등장 애니메이션 (좌→우 슬라이드 + 스냅 + jitter)
    /// </summary>
    public void PlayAppearAnimation(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveDefaultPosition();

        Vector2 startPos = appearStartPoint != null
            ? appearStartPoint.anchoredPosition
            : _cardDefaultPos + new Vector2(-500f, 0f);

        // 카드 초기화
        if (filmCardRoot != null)
            filmCardRoot.SetActive(true);

        if (filmCardRect != null)
            filmCardRect.anchoredPosition = startPos;

        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = 0f;

        var seq = CreateSequence();

        // 1. 슬라이드 + 페이드인
        if (filmCardRect != null)
            seq.Append(filmCardRect.DOAnchorPos(_cardDefaultPos, appearDuration).SetEase(Ease.OutQuad));

        if (filmCardCanvasGroup != null)
            seq.Join(filmCardCanvasGroup.DOFade(1f, appearDuration));

        // 2. 스냅 지터
        if (filmCardRect != null)
        {
            float totalJitterTime = snapJitterDuration * snapJitterCount;
            seq.Append(filmCardRect.DOShakeAnchorPos(totalJitterTime, snapJitterAmount, 10, 90f, false, true, ShakeRandomnessMode.Harmonic));
            seq.AppendCallback(() => filmCardRect.anchoredPosition = _cardDefaultPos);
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 컷 애니메이션 (가위 + 분리)
    /// </summary>
    public void PlayCutAnimation(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();
        Vector2 cardPos = filmCardRect != null ? filmCardRect.anchoredPosition : _cardDefaultPos;

        // 1. 가위 등장 + 이동
        if (scissorsRect != null)
        {
            scissorsRect.gameObject.SetActive(true);
            scissorsRect.anchoredPosition = cardPos + scissorsOffset;

            seq.Append(scissorsRect.DOAnchorPos(cardPos, scissorsMoveDuration).SetEase(Ease.OutQuad));
        }

        // 2. 메인 카드 숨김 + 분리 카드 표시
        seq.AppendCallback(() =>
        {
            if (filmCardRoot != null)
                filmCardRoot.SetActive(false);

            if (cardLeftRect != null)
            {
                cardLeftRect.gameObject.SetActive(true);
                cardLeftRect.anchoredPosition = cardPos;
                cardLeftRect.localRotation = Quaternion.identity;
            }
            if (cardRightRect != null)
            {
                cardRightRect.gameObject.SetActive(true);
                cardRightRect.anchoredPosition = cardPos;
                cardRightRect.localRotation = Quaternion.identity;
            }
            if (cardLeftCanvas != null) cardLeftCanvas.alpha = 1f;
            if (cardRightCanvas != null) cardRightCanvas.alpha = 1f;
        });

        // 3. 카드 분리 애니메이션
        if (cardLeftRect != null)
        {
            Vector2 leftEndPos = cardPos + new Vector2(-splitHorizontalOffset, -splitFallDistance);
            seq.Append(cardLeftRect.DOAnchorPos(leftEndPos, splitDuration).SetEase(Ease.InQuad));
            seq.Join(cardLeftRect.DORotate(new Vector3(0f, 0f, -splitRotateAngle), splitDuration));
        }

        if (cardRightRect != null)
        {
            Vector2 rightEndPos = cardPos + new Vector2(splitHorizontalOffset, -splitFallDistance);
            seq.Join(cardRightRect.DOAnchorPos(rightEndPos, splitDuration).SetEase(Ease.InQuad));
            seq.Join(cardRightRect.DORotate(new Vector3(0f, 0f, splitRotateAngle), splitDuration));
        }

        if (cardLeftCanvas != null)
            seq.Join(cardLeftCanvas.DOFade(0f, splitDuration));
        if (cardRightCanvas != null)
            seq.Join(cardRightCanvas.DOFade(0f, splitDuration));

        // 4. 정리
        seq.AppendCallback(() =>
        {
            if (cardLeftRect != null) cardLeftRect.gameObject.SetActive(false);
            if (cardRightRect != null) cardRightRect.gameObject.SetActive(false);
            if (scissorsRect != null) scissorsRect.gameObject.SetActive(false);
        });

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 통과 애니메이션 (우측 이동)
    /// </summary>
    public void PlayPassAnimation(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2 startPos = filmCardRect != null ? filmCardRect.anchoredPosition : _cardDefaultPos;
        Vector2 endPos = passTargetPoint != null
            ? passTargetPoint.anchoredPosition
            : startPos + new Vector2(400f, 0f);

        var seq = CreateSequence();

        if (filmCardRect != null)
            seq.Append(filmCardRect.DOAnchorPos(endPos, passMoveDuration).SetEase(Ease.OutQuad));

        if (filmCardCanvasGroup != null)
            seq.Join(filmCardCanvasGroup.DOFade(0f, passMoveDuration));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 색상 복원 애니메이션 (흑백 → 컬러)
    /// </summary>
    public void PlayColorRestoreAnimation(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();

        // 1. 딜레이
        if (colorRestoreDelay > 0f)
            seq.AppendInterval(colorRestoreDelay);

        // 2. 색상 전환
        if (filmImages != null && filmImages.Length > 0)
        {
            foreach (var img in filmImages)
            {
                if (img != null)
                    seq.Join(img.DOColor(restoredColor, colorRestoreDuration).SetEase(Ease.OutQuad));
            }
        }

        // 3. 스파클 표시
        seq.AppendCallback(ShowCompletionSparkle);

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 에러 흔들림 효과
    /// </summary>
    public void PlayErrorShake(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();

        if (filmCardRect != null)
        {
            Vector2 originalPos = filmCardRect.anchoredPosition;
            seq.Append(filmCardRect.DOShakeAnchorPos(errorShakeDuration, new Vector2(errorShakeAmount, 0f), 10, 0f, false, true));
            seq.AppendCallback(() => filmCardRect.anchoredPosition = originalPos);
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 다음 카드로 리셋
    /// </summary>
    public void ResetForNextCard()
    {
        KillCurrentSequence();

        // 가위 숨김
        if (scissorsRect != null)
            scissorsRect.gameObject.SetActive(false);

        // 분리 카드 숨김
        if (cardLeftRect != null)
            cardLeftRect.gameObject.SetActive(false);
        if (cardRightRect != null)
            cardRightRect.gameObject.SetActive(false);

        // 메인 카드 복원
        if (filmCardRoot != null)
            filmCardRoot.SetActive(true);
        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = 1f;
        if (filmCardRect != null)
            filmCardRect.anchoredPosition = _cardDefaultPos;
    }

    /// <summary>
    /// 초기 흑백 상태로 설정
    /// </summary>
    public void SetGrayscale()
    {
        if (filmImages == null) return;

        foreach (var img in filmImages)
        {
            if (img != null)
                img.color = grayscaleColor;
        }
    }

    /// <summary>
    /// 즉시 컬러로 설정
    /// </summary>
    public void SetColorImmediate()
    {
        if (filmImages == null) return;

        foreach (var img in filmImages)
        {
            if (img != null)
                img.color = restoredColor;
        }
    }

    /// <summary>
    /// 스파클 표시
    /// </summary>
    public void ShowCompletionSparkle()
    {
        if (completionSparkleRoot != null)
        {
            completionSparkleRoot.SetActive(true);

            var springs = completionSparkleRoot.GetComponentsInChildren<PopupSpring>(true);
            foreach (var spring in springs)
            {
                spring.Play();
            }
        }
    }

    /// <summary>
    /// 스파클 숨김
    /// </summary>
    public void HideCompletionSparkle()
    {
        if (completionSparkleRoot != null)
            completionSparkleRoot.SetActive(false);
    }

    #endregion
}
