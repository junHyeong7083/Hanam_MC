using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 카드 플립 애니메이션
/// - X축 스케일 0으로 줄였다가 다시 펴는 방식
/// - 중간에 앞/뒤 면 전환
/// - 플립 후 페이드 아웃/인 옵션
///
/// [사용처]
/// - Problem2 Step3: NG→OK 씬 카드 전환
/// </summary>
public class CardFlip : MonoBehaviour
{
    [Header("===== 플립 설정 =====")]
    [SerializeField] private float flipDuration = 0.5f;
    [SerializeField] private GameObject frontSide;  // NG 카드
    [SerializeField] private GameObject backSide;   // OK 카드

    [Header("===== 플립 후 페이드 효과 =====")]
    [SerializeField] private bool enableFadeAfterFlip = false;
    [SerializeField] private float fadeOutDuration = 0.1f;   // 알파 0 되는 시간
    [SerializeField] private float fadeInDuration = 0.3f;    // 알파 1 되는 시간
    [SerializeField] private CanvasGroup canvasGroup;        // 페이드용 (없으면 자동 생성)
    [SerializeField] private GameObject warmOverlay;         // 알파 0일 때 활성화

    [Header("===== 색상 변경 =====")]
    [SerializeField] private Image cardImage;                // 색상 변경할 카드 이미지
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warmColor = new Color(1f, 0.95f, 0.9f, 1f);  // 따뜻한 색

    [Header("이벤트")]
    [SerializeField] private UnityEvent onFlipComplete;
    [SerializeField] private UnityEvent onFadeComplete;  // 페이드까지 완료 후

    // 내부
    private bool _isFlipping;
    private bool _showingFront = true;
    private float _flipTime;

    // 페이드 상태
    private enum FadeState { None, FadingOut, FadingIn }
    private FadeState _fadeState = FadeState.None;
    private float _fadeTime;

    private void Awake()
    {
        // 초기 상태: 앞면만 표시
        if (frontSide != null) frontSide.SetActive(true);
        if (backSide != null) backSide.SetActive(false);
        if (warmOverlay != null) warmOverlay.SetActive(false);
        if (cardImage != null) cardImage.color = normalColor;

        // 페이드 효과 사용 시 CanvasGroup 확인
        if (enableFadeAfterFlip && canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 플립 시작
    /// </summary>
    public void Flip()
    {
        if (_isFlipping) return;

        _isFlipping = true;
        _flipTime = 0f;
    }

    private void Update()
    {
        // 플립 애니메이션
        if (_isFlipping)
        {
            UpdateFlip();
            return;
        }

        // 페이드 애니메이션
        if (_fadeState != FadeState.None)
        {
            UpdateFade();
        }
    }

    private void UpdateFlip()
    {
        _flipTime += Time.deltaTime;
        float halfDuration = flipDuration * 0.5f;

        if (_flipTime < halfDuration)
        {
            // 전반부: 스케일 1 → 0
            float t = _flipTime / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, EaseInQuad(t));
            transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        else if (_flipTime < flipDuration)
        {
            // 중간: 면 전환
            if (_showingFront)
            {
                _showingFront = false;
                if (frontSide != null) frontSide.SetActive(false);
                if (backSide != null) backSide.SetActive(true);
            }

            // 후반부: 스케일 0 → 1
            float t = (_flipTime - halfDuration) / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, EaseOutQuad(t));
            transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        else
        {
            // 플립 완료
            transform.localScale = Vector3.one;
            _isFlipping = false;
            onFlipComplete?.Invoke();

            // 페이드 효과 시작
            if (enableFadeAfterFlip)
            {
                StartFadeOut();
            }
        }
    }

    private void UpdateFade()
    {
        _fadeTime += Time.deltaTime;

        switch (_fadeState)
        {
            case FadeState.FadingOut:
                if (_fadeTime < fadeOutDuration)
                {
                    float t = _fadeTime / fadeOutDuration;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                }
                else
                {
                    // 알파 0 도달
                    canvasGroup.alpha = 0f;

                    // 색상 변경 (알파 0일 때)
                    if (cardImage != null)
                        cardImage.color = warmColor;

                    // 바로 페이드 인 시작
                    _fadeState = FadeState.FadingIn;
                    _fadeTime = 0f;
                }
                break;

            case FadeState.FadingIn:
                if (_fadeTime < fadeInDuration)
                {
                    float t = _fadeTime / fadeInDuration;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                }
                else
                {
                    // 알파 1 도달 - warm overlay 활성화
                    canvasGroup.alpha = 1f;
                    _fadeState = FadeState.None;

                    if (warmOverlay != null)
                        warmOverlay.SetActive(true);

                    onFadeComplete?.Invoke();
                }
                break;
        }
    }

    private void StartFadeOut()
    {
        _fadeState = FadeState.FadingOut;
        _fadeTime = 0f;
    }

    /// <summary>
    /// 원래 상태로 리셋 (앞면, 흰색)
    /// </summary>
    public void ResetToFront()
    {
        _isFlipping = false;
        _showingFront = true;
        _fadeState = FadeState.None;
        transform.localScale = Vector3.one;

        if (frontSide != null) frontSide.SetActive(true);
        if (backSide != null) backSide.SetActive(false);
        if (warmOverlay != null) warmOverlay.SetActive(false);
        if (cardImage != null) cardImage.color = normalColor;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Warm overlay 수동 설정
    /// </summary>
    public void SetWarmOverlay(bool active)
    {
        if (warmOverlay != null)
            warmOverlay.SetActive(active);
    }

    /// <summary>
    /// Warm 색상 설정
    /// </summary>
    public void SetWarmColor(Color color)
    {
        warmColor = color;
    }

    public bool IsFlipping => _isFlipping;
    public bool IsFading => _fadeState != FadeState.None;

    /// <summary>
    /// 플립 + 페이드 완료까지 대기하는 코루틴 (기존 UICardFlip 호환)
    /// </summary>
    public IEnumerator PlayFlipRoutine()
    {
        Flip();

        // 플립 완료 대기
        while (_isFlipping)
            yield return null;

        // 페이드 완료 대기 (enableFadeAfterFlip이 true인 경우)
        while (_fadeState != FadeState.None)
            yield return null;
    }

    private float EaseInQuad(float t) => t * t;
    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
