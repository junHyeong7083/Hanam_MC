using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem3 Step2: Effect Controller
/// - 로직에서 이벤트를 받아 이펙트 시퀀스를 관리
/// - 펜 애니메이션, 텍스트 페이드, 스파클 등 타이밍 조율
/// - 로직과 애니메이션 분리를 위한 중앙 관리자
/// </summary>
public class Problem3_Step2_EffectController : MonoBehaviour
{
    [Header("===== 텍스트 페이드 대상 =====")]
    [SerializeField] private CanvasGroup sentenceCanvasGroup;
    [SerializeField] private Text sentenceText;

    [Header("===== 이펙트 컴포넌트 참조 =====")]
    [SerializeField] private PenWriteAnimation penWriteAnimation;
    [SerializeField] private CompletionSparkle completionSparkle;

    [Header("===== 타이밍 설정 =====")]
    [SerializeField] private float optionSelectDelay = 0.5f;   // 옵션 선택 후 딜레이
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("===== 색상 설정 =====")]
    [SerializeField] private Color originalTextColor = new Color(0.24f, 0.18f, 0.14f);
    [SerializeField] private Color rewrittenTextColor = new Color(1f, 0.54f, 0.24f);

    // 상태
    private bool _isAnimating;
    private float _elapsed;
    private RewritePhase _phase;
    private string _pendingRewrittenText;
    private Action _onCompleteCallback;

    private enum RewritePhase
    {
        Idle,
        Delay,
        FadeOut,
        FadeIn
    }

    /// <summary>
    /// 현재 애니메이션 중인지 여부
    /// </summary>
    public bool IsAnimating => _isAnimating;

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;

        switch (_phase)
        {
            case RewritePhase.Delay:
                ProcessDelayPhase();
                break;

            case RewritePhase.FadeOut:
                ProcessFadeOutPhase();
                break;

            case RewritePhase.FadeIn:
                ProcessFadeInPhase();
                break;
        }
    }

    #region Public API

    /// <summary>
    /// 재작성 애니메이션 시퀀스 시작
    /// </summary>
    /// <param name="rewrittenText">변경될 텍스트</param>
    /// <param name="onComplete">완료 콜백</param>
    public void PlayRewriteSequence(string rewrittenText, Action onComplete = null)
    {
        if (_isAnimating) return;

        _pendingRewrittenText = rewrittenText;
        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;
        _phase = RewritePhase.Delay;
    }

    /// <summary>
    /// 다음 스텝으로 넘어갈 때 리셋
    /// </summary>
    public void ResetForNextStep()
    {
        _isAnimating = false;
        _phase = RewritePhase.Idle;
        _elapsed = 0f;
        _pendingRewrittenText = null;
        _onCompleteCallback = null;

        // 텍스트 색상 원래대로
        if (sentenceText != null)
            sentenceText.color = originalTextColor;

        // 캔버스 그룹 알파 원래대로
        if (sentenceCanvasGroup != null)
            sentenceCanvasGroup.alpha = 1f;

        // 스파클 리셋
        if (completionSparkle != null)
            completionSparkle.ResetTrigger();
    }

    /// <summary>
    /// 원본 텍스트 즉시 표시 (스텝 진입 시)
    /// </summary>
    public void ShowOriginalTextImmediate(string originalText)
    {
        if (sentenceText != null)
        {
            sentenceText.text = originalText;
            sentenceText.color = originalTextColor;
        }

        if (sentenceCanvasGroup != null)
            sentenceCanvasGroup.alpha = 1f;
    }

    #endregion

    #region Phase Processing

    private void ProcessDelayPhase()
    {
        if (_elapsed >= optionSelectDelay)
        {
            // 펜 애니메이션 시작
            if (penWriteAnimation != null)
                penWriteAnimation.Play();

            // 페이드아웃 시작
            _phase = RewritePhase.FadeOut;
            _elapsed = 0f;
        }
    }

    private void ProcessFadeOutPhase()
    {
        float t = Mathf.Clamp01(_elapsed / fadeOutDuration);

        if (sentenceCanvasGroup != null)
            sentenceCanvasGroup.alpha = 1f - t;

        if (t >= 1f)
        {
            // 페이드아웃 완료 - 텍스트 교체
            if (sentenceCanvasGroup != null)
                sentenceCanvasGroup.alpha = 0f;

            if (sentenceText != null)
            {
                sentenceText.text = _pendingRewrittenText;
                sentenceText.color = rewrittenTextColor;
            }

            // 페이드인 시작
            _phase = RewritePhase.FadeIn;
            _elapsed = 0f;
        }
    }

    private void ProcessFadeInPhase()
    {
        float t = Mathf.Clamp01(_elapsed / fadeInDuration);

        if (sentenceCanvasGroup != null)
            sentenceCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            // 완료
            if (sentenceCanvasGroup != null)
                sentenceCanvasGroup.alpha = 1f;

            _isAnimating = false;
            _phase = RewritePhase.Idle;

            // 스파클은 CanvasGroup 알파 감지로 자동 실행됨 (CompletionSparkle)

            // 콜백 호출
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        }
    }

    #endregion

    #region Easing (필요시 사용)

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    #endregion
}
