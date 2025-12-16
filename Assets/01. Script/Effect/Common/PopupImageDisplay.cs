using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 팝업 이미지 표시 컴포넌트 (재사용 가능)
/// - 이미지 표시 + 스케일 애니메이션(min→max→final) + 일정 시간 후 자동 숨김
/// </summary>
public class PopupImageDisplay : MonoBehaviour
{
    [Header("===== 대상 =====")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("===== 스케일 애니메이션 =====")]
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 1.1f;
    [SerializeField] private float finalScale = 1.0f;
    [SerializeField] private float growDuration = 0.3f;
    [SerializeField] private float shrinkDuration = 0.2f;

    [Tooltip("체크하면 min→max 단계 건너뛰고 max→final만 재생")]
    [SerializeField] private bool skipGrowAnimation = false;

    [Header("===== 표시 시간 =====")]
    [Tooltip("0이면 자동 숨김 없음")]
    [SerializeField] private float displayDuration = 2f;

    [Tooltip("체크하면 자동 숨김 없이 계속 표시됨")]
    [SerializeField] private bool stayVisible = false;

    [Header("===== 페이드 (옵션) =====")]
    [SerializeField] private bool useFade = false;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    [Header("===== 자동 시작 =====")]
    [SerializeField] private bool playOnEnable = false;

    private Sequence _sequence;
    private bool _isShowing;
    private Vector3 _baseScale;
    private bool _baseScaleCaptured;

    public bool IsShowing => _isShowing;
    public RectTransform TargetRect => targetRect;

    private void OnEnable()
    {
        // 최초 1회만 베이스 스케일 캡처
        if (targetRect != null && !_baseScaleCaptured)
        {
            _baseScale = targetRect.localScale;
            _baseScaleCaptured = true;
        }

        if (playOnEnable)
            Show();
    }

    #region Public API

    /// <summary>
    /// 팝업 표시: minScale → maxScale → finalScale → (displayDuration 후 숨김)
    /// </summary>
    public void Show(Action onComplete = null)
    {
        if (targetRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        KillSequence();
        _isShowing = true;

        // 베이스 스케일이 없으면 현재 스케일 사용
        Vector3 baseScale = _baseScaleCaptured ? _baseScale : targetRect.localScale;

        // 초기 상태: skipGrowAnimation이면 maxScale에서 시작, 아니면 minScale에서 시작
        float startScaleRatio = skipGrowAnimation ? maxScale : minScale;
        targetRect.localScale = baseScale * startScaleRatio;
        targetRect.gameObject.SetActive(true);

        // 알파 초기화 (useFade면 0으로 시작)
        if (canvasGroup != null)
            canvasGroup.alpha = useFade ? 0f : 1f;

        _sequence = DOTween.Sequence();

        // 페이드인 (옵션) - fadeInDuration이 0이면 즉시 1로 설정
        if (useFade && canvasGroup != null)
        {
            if (fadeInDuration > 0f)
                _sequence.Append(canvasGroup.DOFade(1f, fadeInDuration));
            else
                canvasGroup.alpha = 1f; // 즉시 보이게
        }

        // 스케일 애니메이션 (baseScale 기준으로 계산)
        Vector3 maxScaleVec = baseScale * maxScale;
        Vector3 finalScaleVec = baseScale * finalScale;

        if (skipGrowAnimation)
        {
            // max → final만 재생
            _sequence.Append(targetRect.DOScale(finalScaleVec, shrinkDuration).SetEase(Ease.OutQuad));
        }
        else
        {
            // min → max → final 순서로 재생
            _sequence.Append(targetRect.DOScale(maxScaleVec, growDuration).SetEase(Ease.OutBack));
            _sequence.Append(targetRect.DOScale(finalScaleVec, shrinkDuration).SetEase(Ease.OutQuad));
        }

        // displayDuration > 0 이면 대기 시간 추가
        if (displayDuration > 0f)
        {
            _sequence.AppendInterval(displayDuration);
        }

        // 완료 처리
        _sequence.OnComplete(() =>
        {
            _isShowing = false;

            // stayVisible=true가 아닌 경우 숨김 처리
            if (!stayVisible)
            {
                HideImmediate();
                // 전체 GameObject 비활성화 (targetRect만 숨기면 부모는 남아있음)
                gameObject.SetActive(false);
            }

            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 팝업 숨기기 (페이드 아웃 또는 즉시)
    /// </summary>
    public void Hide(Action onComplete = null)
    {
        if (targetRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        KillSequence();

        if (useFade && canvasGroup != null && fadeOutDuration > 0f)
        {
            _sequence = DOTween.Sequence();
            _sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration));
            _sequence.OnComplete(() =>
            {
                HideImmediate();
                onComplete?.Invoke();
            });
        }
        else
        {
            HideImmediate();
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 즉시 숨기기
    /// </summary>
    public void HideImmediate()
    {
        KillSequence();
        _isShowing = false;

        if (targetRect != null)
        {
            // 원래 스케일로 복원 (다음 Show() 호출 시 정상 동작을 위해)
            if (_baseScaleCaptured)
                targetRect.localScale = _baseScale;
            targetRect.gameObject.SetActive(false);
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 초기 상태로 리셋
    /// </summary>
    public void ResetToInitial()
    {
        HideImmediate();
    }

    #endregion

    #region Private

    private void KillSequence()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    #endregion
}
