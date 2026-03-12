using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// CardFlip - X축 스케일 기반 카드 뒤집기 애니메이션 컴포넌트
///
/// 【역할】 X축 스케일을 1→0→1로 변화시켜 카드 뒤집기 효과 구현.
///          중간 지점(스케일 0)에서 앞면/뒷면 GameObject를 전환한다.
///          플립 후 선택적으로 페이드아웃→색상 변경→페이드인 시퀀스 실행 가능.
/// 【사용 위치】 Problem2 Step3(NG→OK 씬 카드 전환) 등 카드 뒤집기가 필요한 UI
/// 【트리거】 외부에서 Flip() 또는 PlayFlipRoutine() 코루틴 호출
/// 【의존성】 frontSide/backSide(앞뒤면 GameObject), CanvasGroup(페이드용, 자동 추가)
/// </summary>
public class CardFlip : MonoBehaviour
{
    [Header("===== 플립 설정 =====")]
    [SerializeField] private float flipDuration = 0.5f;       // 플립 전체 소요 시간 (전반부+후반부)
    [SerializeField] private GameObject frontSide;             // 앞면 (예: NG 카드)
    [SerializeField] private GameObject backSide;              // 뒷면 (예: OK 카드)

    [Header("===== 플립 후 페이드 효과 =====")]
    [SerializeField] private bool enableFadeAfterFlip = false; // 플립 완료 후 페이드 시퀀스 활성화
    [SerializeField] private float fadeOutDuration = 0.1f;     // 페이드아웃 소요 시간 (알파 1→0)
    [SerializeField] private float fadeInDuration = 0.3f;      // 페이드인 소요 시간 (알파 0→1)
    [SerializeField] private CanvasGroup canvasGroup;          // 페이드용 CanvasGroup (없으면 자동 생성)
    [SerializeField] private GameObject warmOverlay;           // 페이드인 완료 시 활성화할 따뜻한 오버레이

    [Header("===== 색상 변경 =====")]
    [SerializeField] private Image cardImage;                  // 색상 변경할 카드 배경 이미지
    [SerializeField] private Color normalColor = Color.white;  // 기본 색상 (플립 전)
    [SerializeField] private Color warmColor = new Color(1f, 0.95f, 0.9f, 1f);  // 따뜻한 색 (플립 후)

    [Header("이벤트")]
    [SerializeField] private UnityEvent onFlipComplete;        // 플립 애니메이션 완료 시 호출
    [SerializeField] private UnityEvent onFadeComplete;        // 페이드까지 모두 완료 후 호출

    // 내부 상태
    private bool _isFlipping;               // 플립 애니메이션 진행 중 여부
    private bool _showingFront = true;       // 현재 앞면 표시 중인지
    private float _flipTime;                // 플립 경과 시간

    // 페이드 상태 머신
    private enum FadeState { None, FadingOut, FadingIn }
    private FadeState _fadeState = FadeState.None;  // 현재 페이드 상태
    private float _fadeTime;                        // 페이드 경과 시간

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
