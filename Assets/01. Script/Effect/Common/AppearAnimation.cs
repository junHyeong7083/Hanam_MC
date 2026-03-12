using UnityEngine;
using DG.Tweening;

/// <summary>
/// AppearAnimation - UI 오브젝트 등장 시 슬라이드 + 페이드인 + 스케일 애니메이션을 재생하는 컴포넌트
///
/// 【역할】 오브젝트가 활성화될 때 지정된 방향에서 슬라이드하며 나타나는 등장 연출 제공
///          슬라이드/페이드/스케일 각각을 개별 토글로 조합 가능
/// 【사용 위치】 Problem2 Step2(필름 카드 순차 등장), Problem3 Step1(캐릭터/말풍선/책),
///              버튼, 카드, UI 요소 등 다양한 등장 연출에 범용 사용
/// 【트리거】 OnEnable 시 자동 재생, 또는 외부에서 Replay() 호출
/// 【의존성】 DOTween(DG.Tweening), RectTransform, CanvasGroup(페이드 사용 시 자동 추가)
/// </summary>
public class AppearAnimation : MonoBehaviour
{
    /// <summary>슬라이드 진입 방향 (아래/위/왼쪽/오른쪽)</summary>
    public enum SlideDirection { Bottom, Top, Left, Right }

    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float delay = 0f;           // 재생 전 대기 시간 (순차 등장 시 사용)
    [SerializeField] private float duration = 0.4f;       // 전체 애니메이션 재생 시간

    [Header("위치")]
    [SerializeField] private bool enableSlide = true;     // 슬라이드 이동 활성화 여부
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Bottom;  // 어느 방향에서 슬라이드할지
    [SerializeField] private float slideDistance = 50f;    // 슬라이드 이동 거리 (px)

    [Header("페이드")]
    [SerializeField] private bool enableFade = true;      // 알파 페이드인 활성화 여부

    [Header("스케일")]
    [SerializeField] private bool enableScale = false;    // 스케일 애니메이션 활성화 여부
    [SerializeField] private float startScale = 0.8f;     // 시작 스케일 (1.0이 원래 크기)

    [Header("Easing")]
    [SerializeField] private Ease easeType = Ease.OutQuad; // 애니메이션 이징 타입

    // 내부 상태
    private RectTransform _rectTransform;     // 이 오브젝트의 RectTransform
    private CanvasGroup _canvasGroup;         // 페이드용 CanvasGroup (자동 추가)
    private Vector2 _targetPosition;          // 애니메이션 목표 위치 (원래 위치)
    private Sequence _sequence;               // 현재 실행 중인 DOTween Sequence

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        // CanvasGroup 자동 추가 (페이드용)
        if (enableFade)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        // 시작 상태 설정
        _targetPosition = _rectTransform.anchoredPosition;

        if (enableSlide)
            _rectTransform.anchoredPosition = _targetPosition + GetSlideOffset();

        if (enableFade && _canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (enableScale)
            _rectTransform.localScale = Vector3.one * startScale;

        PlayAnimation();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void KillSequence()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    /// <summary>
    /// 슬라이드 방향에 따른 오프셋 계산
    /// </summary>
    private Vector2 GetSlideOffset()
    {
        switch (slideFrom)
        {
            case SlideDirection.Bottom: return Vector2.down * slideDistance;
            case SlideDirection.Top: return Vector2.up * slideDistance;
            case SlideDirection.Left: return Vector2.left * slideDistance;
            case SlideDirection.Right: return Vector2.right * slideDistance;
            default: return Vector2.down * slideDistance;
        }
    }

    /// <summary>
    /// DOTween Sequence를 구성하여 등장 애니메이션을 재생한다.
    /// 딜레이 → 슬라이드 → 페이드 → 스케일 순서로 Append/Join 한다.
    /// </summary>
    private void PlayAnimation()
    {
        KillSequence();

        _sequence = DOTween.Sequence();

        // 딜레이
        if (delay > 0f)
            _sequence.AppendInterval(delay);

        // 위치 애니메이션
        if (enableSlide)
            _sequence.Append(_rectTransform.DOAnchorPos(_targetPosition, duration).SetEase(easeType));

        // 페이드 애니메이션
        if (enableFade && _canvasGroup != null)
        {
            if (enableSlide)
                _sequence.Join(_canvasGroup.DOFade(1f, duration));
            else
                _sequence.Append(_canvasGroup.DOFade(1f, duration).SetEase(easeType));
        }

        // 스케일 애니메이션
        if (enableScale)
        {
            if (enableSlide || enableFade)
                _sequence.Join(_rectTransform.DOScale(1f, duration).SetEase(easeType));
            else
                _sequence.Append(_rectTransform.DOScale(1f, duration).SetEase(easeType));
        }
    }

    /// <summary>
    /// 외부에서 딜레이 설정 (순차 등장용)
    /// </summary>
    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }

    /// <summary>
    /// 애니메이션 재시작
    /// </summary>
    public void Replay()
    {
        OnEnable();
    }
}
