using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem3 Step2: Effect Controller
/// - 로직에서 이벤트를 받아 이펙트 시퀀스를 관리
/// - 펜 애니메이션, 텍스트 페이드, 스파클 등 타이밍 조율
///
/// 흐름:
/// 1. 대기: 펜 originPos에서 알파 펄스
/// 2. 버튼 선택 → PlayRewriteSequence() 호출
/// 3. 딜레이 후 펜 이동 시작 (startPoint → endPoint)
/// 4. 펜 이동 완료 → 펜 숨김, 스파클 표시, 텍스트 페이드인
/// 5. 다음 문항 → 스파클 숨김, 펜 표시 + originPos
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
    [SerializeField] private float optionSelectDelay = 0.5f;
    [SerializeField] private float penMoveDuration = 0.8f;  // PenWriteAnimation의 moveDuration과 맞춰야 함
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
        PenMoving,      // 펜 이동 + 텍스트 페이드아웃
        ShowSparkle,    // 펜 숨김 + 스파클 표시 + 텍스트 페이드인
    }

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

            case RewritePhase.PenMoving:
                ProcessPenMovingPhase();
                break;

            case RewritePhase.ShowSparkle:
                ProcessShowSparklePhase();
                break;
        }
    }

    #region Public API

    /// <summary>
    /// 재작성 애니메이션 시퀀스 시작
    /// </summary>
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

        // 스파클 숨김
        if (completionSparkle != null)
        {
            completionSparkle.ResetTrigger();
            completionSparkle.gameObject.SetActive(false);
        }

        // 펜 다시 표시 + 대기 상태로
        if (penWriteAnimation != null)
        {
            penWriteAnimation.gameObject.SetActive(true);
            penWriteAnimation.ResetToIdle();
        }
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

        // 펜 표시, 스파클 숨김
        if (penWriteAnimation != null)
        {
            penWriteAnimation.gameObject.SetActive(true);
            penWriteAnimation.ResetToIdle();
        }

        if (completionSparkle != null)
        {
            completionSparkle.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Phase Processing

    private void ProcessDelayPhase()
    {
        if (_elapsed >= optionSelectDelay)
        {
            // 펜 이동 애니메이션 시작
            if (penWriteAnimation != null)
                penWriteAnimation.Play();

            _phase = RewritePhase.PenMoving;
            _elapsed = 0f;
        }
    }

    private void ProcessPenMovingPhase()
    {
        // 펜 이동 중에 텍스트 페이드아웃 (펜 이동의 앞부분에서)
        float fadeOutT = Mathf.Clamp01(_elapsed / fadeOutDuration);
        if (sentenceCanvasGroup != null && fadeOutT <= 1f)
        {
            sentenceCanvasGroup.alpha = 1f - fadeOutT;
        }

        // 펜 이동 완료 시점
        if (_elapsed >= penMoveDuration)
        {
            // 펜 숨김
            if (penWriteAnimation != null)
                penWriteAnimation.gameObject.SetActive(false);

            // 텍스트 교체
            if (sentenceText != null)
            {
                sentenceText.text = _pendingRewrittenText;
                sentenceText.color = rewrittenTextColor;
            }

            if (sentenceCanvasGroup != null)
                sentenceCanvasGroup.alpha = 0f;

            // 스파클 표시
            if (completionSparkle != null)
            {
                completionSparkle.gameObject.SetActive(true);
                completionSparkle.Show();
            }

            _phase = RewritePhase.ShowSparkle;
            _elapsed = 0f;
        }
    }

    private void ProcessShowSparklePhase()
    {
        // 텍스트 페이드인
        float fadeInT = Mathf.Clamp01(_elapsed / fadeInDuration);

        if (sentenceCanvasGroup != null)
            sentenceCanvasGroup.alpha = fadeInT;

        if (fadeInT >= 1f)
        {
            // 완료
            if (sentenceCanvasGroup != null)
                sentenceCanvasGroup.alpha = 1f;

            _isAnimating = false;
            _phase = RewritePhase.Idle;

            // 콜백 호출
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        }
    }

    #endregion
}
