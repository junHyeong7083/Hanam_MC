using System;
using UnityEngine;
using UnityEngine.UI;

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
public class Problem4_Step2_EffectController : MonoBehaviour
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
    [SerializeField] private Vector2 scissorsOffset = new Vector2(-150f, 0f);

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
    [SerializeField] private Image[] filmImages;  // 흑백→컬러 전환할 이미지들
    [SerializeField] private Color grayscaleColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color restoredColor = Color.white;
    [SerializeField] private float colorRestoreDuration = 1.2f;
    [SerializeField] private float colorRestoreDelay = 0.3f;

    [Header("===== 완료 스파클 =====")]
    [SerializeField] private GameObject completionSparkleRoot;

    [Header("===== 에러 효과 =====")]
    [SerializeField] private float errorShakeDuration = 0.4f;
    [SerializeField] private float errorShakeAmount = 10f;

    // 상태
    private bool _isAnimating;
    private float _elapsed;
    private AnimPhase _phase;
    private Action _onCompleteCallback;

    // 카드 기본 위치
    private Vector2 _cardDefaultPos;
    private bool _defaultPosSaved;

    // 애니메이션 중간 데이터
    private Vector2 _animStartPos;
    private Vector2 _animEndPos;
    private Vector2 _splitCenterPos;

    private enum AnimPhase
    {
        Idle,
        // 등장
        AppearSlide,
        AppearSnap,
        // 컷
        ScissorsMove,
        CardSplit,
        // 통과
        PassMove,
        // 색상 복원
        ColorRestoreDelay,
        ColorRestoring,
        // 에러
        ErrorShake
    }

    public bool IsAnimating => _isAnimating;

    private void Awake()
    {
        // 카드 기본 위치 저장
        if (filmCardRect != null && !_defaultPosSaved)
        {
            _cardDefaultPos = filmCardRect.anchoredPosition;
            _defaultPosSaved = true;
        }
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;

        switch (_phase)
        {
            case AnimPhase.AppearSlide:
                ProcessAppearSlide();
                break;
            case AnimPhase.AppearSnap:
                ProcessAppearSnap();
                break;
            case AnimPhase.ScissorsMove:
                ProcessScissorsMove();
                break;
            case AnimPhase.CardSplit:
                ProcessCardSplit();
                break;
            case AnimPhase.PassMove:
                ProcessPassMove();
                break;
            case AnimPhase.ColorRestoreDelay:
                ProcessColorRestoreDelay();
                break;
            case AnimPhase.ColorRestoring:
                ProcessColorRestoring();
                break;
            case AnimPhase.ErrorShake:
                ProcessErrorShake();
                break;
        }
    }

    #region Public API

    /// <summary>
    /// 카드 기본 위치 저장 (Logic에서 호출)
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
        // 이미 애니메이션 중이면 콜백만 호출
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        // 시작 위치 설정
        if (appearStartPoint != null)
            _animStartPos = appearStartPoint.anchoredPosition;
        else
            _animStartPos = _cardDefaultPos + new Vector2(-500f, 0f);

        _animEndPos = _cardDefaultPos;

        // 카드 초기화
        if (filmCardRoot != null)
            filmCardRoot.SetActive(true);

        if (filmCardRect != null)
            filmCardRect.anchoredPosition = _animStartPos;

        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = 0f;

        _phase = AnimPhase.AppearSlide;
    }

    /// <summary>
    /// 컷 애니메이션 (가위 + 분리)
    /// </summary>
    public void PlayCutAnimation(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        // 가위 시작 위치
        if (scissorsRect != null && filmCardRect != null)
        {
            scissorsRect.gameObject.SetActive(true);
            _animStartPos = filmCardRect.anchoredPosition + scissorsOffset;
            _animEndPos = filmCardRect.anchoredPosition;
            scissorsRect.anchoredPosition = _animStartPos;
        }

        _phase = AnimPhase.ScissorsMove;
    }

    /// <summary>
    /// 통과 애니메이션 (우측 이동)
    /// </summary>
    public void PlayPassAnimation(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        if (filmCardRect != null)
        {
            _animStartPos = filmCardRect.anchoredPosition;

            if (passTargetPoint != null)
                _animEndPos = passTargetPoint.anchoredPosition;
            else
                _animEndPos = _animStartPos + new Vector2(400f, 0f);
        }

        _phase = AnimPhase.PassMove;
    }

    /// <summary>
    /// 색상 복원 애니메이션 (흑백 → 컬러)
    /// </summary>
    public void PlayColorRestoreAnimation(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;
        _phase = AnimPhase.ColorRestoreDelay;
    }

    /// <summary>
    /// 에러 흔들림 효과
    /// </summary>
    public void PlayErrorShake(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        if (filmCardRect != null)
            _animStartPos = filmCardRect.anchoredPosition;

        _phase = AnimPhase.ErrorShake;
    }

    /// <summary>
    /// 다음 카드로 리셋
    /// </summary>
    public void ResetForNextCard()
    {
        _isAnimating = false;
        _phase = AnimPhase.Idle;
        _elapsed = 0f;

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

            // PopupSpring 있으면 재생
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

    #region Animation Processing

    private void ProcessAppearSlide()
    {
        float t = Mathf.Clamp01(_elapsed / appearDuration);
        float eased = EaseOutQuad(t);

        if (filmCardRect != null)
            filmCardRect.anchoredPosition = Vector2.Lerp(_animStartPos, _animEndPos, eased);

        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = eased;

        if (t >= 1f)
        {
            // 스냅 단계로
            _phase = AnimPhase.AppearSnap;
            _elapsed = 0f;
        }
    }

    private void ProcessAppearSnap()
    {
        // Jitter 효과 (프레임 스냅 느낌)
        float totalJitterTime = snapJitterDuration * snapJitterCount;
        float t = _elapsed / totalJitterTime;

        if (filmCardRect != null && t < 1f)
        {
            // 감쇠하는 jitter
            float decay = 1f - t;
            float jitterX = Mathf.Sin(_elapsed * 50f) * snapJitterAmount * decay;
            float jitterY = Mathf.Cos(_elapsed * 40f) * snapJitterAmount * 0.5f * decay;

            filmCardRect.anchoredPosition = _cardDefaultPos + new Vector2(jitterX, jitterY);
        }

        if (t >= 1f)
        {
            // 최종 위치 고정
            if (filmCardRect != null)
                filmCardRect.anchoredPosition = _cardDefaultPos;

            CompleteAnimation();
        }
    }

    private void ProcessScissorsMove()
    {
        float t = Mathf.Clamp01(_elapsed / scissorsMoveDuration);
        float eased = EaseOutQuad(t);

        if (scissorsRect != null)
            scissorsRect.anchoredPosition = Vector2.Lerp(_animStartPos, _animEndPos, eased);

        if (t >= 1f)
        {
            // 카드 분리 시작
            _splitCenterPos = filmCardRect != null ? filmCardRect.anchoredPosition : _cardDefaultPos;

            // 메인 카드 숨김
            if (filmCardRoot != null)
                filmCardRoot.SetActive(false);

            // 분리 카드 표시
            if (cardLeftRect != null)
            {
                cardLeftRect.gameObject.SetActive(true);
                cardLeftRect.anchoredPosition = _splitCenterPos;
                cardLeftRect.localRotation = Quaternion.identity;
            }
            if (cardRightRect != null)
            {
                cardRightRect.gameObject.SetActive(true);
                cardRightRect.anchoredPosition = _splitCenterPos;
                cardRightRect.localRotation = Quaternion.identity;
            }
            if (cardLeftCanvas != null)
                cardLeftCanvas.alpha = 1f;
            if (cardRightCanvas != null)
                cardRightCanvas.alpha = 1f;

            _phase = AnimPhase.CardSplit;
            _elapsed = 0f;
        }
    }

    private void ProcessCardSplit()
    {
        float t = Mathf.Clamp01(_elapsed / splitDuration);
        float eased = EaseInQuad(t);  // 가속하면서 떨어짐

        float alpha = 1f - eased;

        if (cardLeftRect != null)
        {
            Vector2 pos = _splitCenterPos + new Vector2(
                -splitHorizontalOffset * eased,
                -splitFallDistance * eased
            );
            cardLeftRect.anchoredPosition = pos;
            cardLeftRect.localRotation = Quaternion.Euler(0f, 0f, -splitRotateAngle * eased);
        }

        if (cardRightRect != null)
        {
            Vector2 pos = _splitCenterPos + new Vector2(
                splitHorizontalOffset * eased,
                -splitFallDistance * eased
            );
            cardRightRect.anchoredPosition = pos;
            cardRightRect.localRotation = Quaternion.Euler(0f, 0f, splitRotateAngle * eased);
        }

        if (cardLeftCanvas != null)
            cardLeftCanvas.alpha = alpha;
        if (cardRightCanvas != null)
            cardRightCanvas.alpha = alpha;

        if (t >= 1f)
        {
            // 분리 카드 숨김
            if (cardLeftRect != null)
                cardLeftRect.gameObject.SetActive(false);
            if (cardRightRect != null)
                cardRightRect.gameObject.SetActive(false);

            // 가위 숨김
            if (scissorsRect != null)
                scissorsRect.gameObject.SetActive(false);

            CompleteAnimation();
        }
    }

    private void ProcessPassMove()
    {
        float t = Mathf.Clamp01(_elapsed / passMoveDuration);
        float eased = EaseOutQuad(t);

        if (filmCardRect != null)
            filmCardRect.anchoredPosition = Vector2.Lerp(_animStartPos, _animEndPos, eased);

        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = 1f - eased;

        if (t >= 1f)
        {
            CompleteAnimation();
        }
    }

    private void ProcessColorRestoreDelay()
    {
        if (_elapsed >= colorRestoreDelay)
        {
            _phase = AnimPhase.ColorRestoring;
            _elapsed = 0f;
        }
    }

    private void ProcessColorRestoring()
    {
        float t = Mathf.Clamp01(_elapsed / colorRestoreDuration);
        float eased = EaseOutQuad(t);

        // 모든 필름 이미지 색상 전환
        if (filmImages != null)
        {
            Color currentColor = Color.Lerp(grayscaleColor, restoredColor, eased);

            foreach (var img in filmImages)
            {
                if (img != null)
                    img.color = currentColor;
            }
        }

        if (t >= 1f)
        {
            // 스파클 표시
            ShowCompletionSparkle();
            CompleteAnimation();
        }
    }

    private void ProcessErrorShake()
    {
        float t = Mathf.Clamp01(_elapsed / errorShakeDuration);

        if (filmCardRect != null)
        {
            // 감쇠하는 좌우 흔들림
            float decay = 1f - t;
            float shakeX = Mathf.Sin(_elapsed * 40f) * errorShakeAmount * decay;

            filmCardRect.anchoredPosition = _animStartPos + new Vector2(shakeX, 0f);
        }

        if (t >= 1f)
        {
            // 원래 위치로
            if (filmCardRect != null)
                filmCardRect.anchoredPosition = _animStartPos;

            CompleteAnimation();
        }
    }

    private void CompleteAnimation()
    {
        _isAnimating = false;
        _phase = AnimPhase.Idle;

        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }

    #endregion

    #region Easing

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private float EaseInQuad(float t) => t * t;

    #endregion
}
