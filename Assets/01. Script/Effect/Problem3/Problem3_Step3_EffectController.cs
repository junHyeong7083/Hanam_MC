using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem3 Step3: Effect Controller
/// - 객관식 문제의 이펙트 시퀀스 관리
/// - 힌트 페이드, 정답 효과, 문제 등장 애니메이션 등
/// - 로직과 애니메이션 분리를 위한 중앙 관리자
/// </summary>
public class Problem3_Step3_EffectController : MonoBehaviour
{
    [Header("===== 힌트 UI =====")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private Text hintLabel;
    [SerializeField] private CanvasGroup hintCanvasGroup;

    [Header("===== 힌트 타이밍 =====")]
    [SerializeField] private float hintShowDuration = 1.5f;
    [SerializeField] private float hintFadeDuration = 0.4f;

    [Header("===== 정답 효과 =====")]
    [SerializeField] private RectTransform correctSparkleEffect;
    [SerializeField] private float correctEffectDuration = 0.5f;

    [Header("===== 문제 등장 (옵션) =====")]
    [SerializeField] private CanvasGroup questionCanvasGroup;
    [SerializeField] private float questionFadeInDuration = 0.3f;

    // 상태
    private bool _isHintAnimating;
    private float _hintElapsed;
    private HintPhase _hintPhase;
    private Action _onHintComplete;

    private bool _isQuestionAnimating;
    private float _questionElapsed;

    private enum HintPhase
    {
        Idle,
        Showing,
        FadingOut
    }

    /// <summary>
    /// 현재 힌트 애니메이션 중인지 여부
    /// </summary>
    public bool IsHintAnimating => _isHintAnimating;

    private void Update()
    {
        if (_isHintAnimating)
        {
            ProcessHintAnimation();
        }

        if (_isQuestionAnimating)
        {
            ProcessQuestionFadeIn();
        }
    }

    #region Public API

    /// <summary>
    /// 힌트 표시 후 자동 페이드아웃
    /// </summary>
    public void PlayHintSequence(string hintText, Action onComplete = null)
    {
        if (_isHintAnimating) return;

        _onHintComplete = onComplete;

        // 힌트 텍스트 설정
        if (hintLabel != null)
            hintLabel.text = hintText;

        // 힌트 루트 활성화
        if (hintRoot != null)
            hintRoot.SetActive(true);

        // 알파 1로 시작
        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 1f;

        // 애니메이션 시작
        _isHintAnimating = true;
        _hintElapsed = 0f;
        _hintPhase = HintPhase.Showing;
    }

    /// <summary>
    /// 힌트 즉시 숨김
    /// </summary>
    public void HideHintImmediate()
    {
        _isHintAnimating = false;
        _hintPhase = HintPhase.Idle;

        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 0f;

        if (hintRoot != null)
            hintRoot.SetActive(false);
    }

    /// <summary>
    /// 정답 선택 시 효과 재생 (선택된 버튼 위치에 표시)
    /// </summary>
    public void PlayCorrectEffect(RectTransform targetButton = null)
    {
        if (correctSparkleEffect != null)
        {
            // 타겟 버튼이 있으면 그 위치로 이동
            if (targetButton != null)
            {
                correctSparkleEffect.position = targetButton.position;
            }

            correctSparkleEffect.gameObject.SetActive(true);

            // 자동 숨김 (duration 후)
            if (correctEffectDuration > 0f)
            {
                Invoke(nameof(HideCorrectEffect), correctEffectDuration);
            }
        }
    }

    /// <summary>
    /// 문제 등장 페이드인
    /// </summary>
    public void PlayQuestionAppear()
    {
        if (questionCanvasGroup == null) return;

        questionCanvasGroup.alpha = 0f;
        _isQuestionAnimating = true;
        _questionElapsed = 0f;
    }

    /// <summary>
    /// 문제 즉시 표시
    /// </summary>
    public void ShowQuestionImmediate()
    {
        _isQuestionAnimating = false;

        if (questionCanvasGroup != null)
            questionCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 다음 문제로 넘어갈 때 리셋
    /// </summary>
    public void ResetForNextQuestion()
    {
        HideHintImmediate();
        HideCorrectEffect();
    }

    #endregion

    #region Animation Processing

    private void ProcessHintAnimation()
    {
        _hintElapsed += Time.deltaTime;

        switch (_hintPhase)
        {
            case HintPhase.Showing:
                // 표시 시간 대기
                if (_hintElapsed >= hintShowDuration)
                {
                    _hintPhase = HintPhase.FadingOut;
                    _hintElapsed = 0f;
                }
                break;

            case HintPhase.FadingOut:
                // 페이드아웃
                float t = Mathf.Clamp01(_hintElapsed / hintFadeDuration);

                if (hintCanvasGroup != null)
                    hintCanvasGroup.alpha = 1f - t;

                if (t >= 1f)
                {
                    // 완료
                    HideHintImmediate();
                    _onHintComplete?.Invoke();
                    _onHintComplete = null;
                }
                break;
        }
    }

    private void ProcessQuestionFadeIn()
    {
        _questionElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_questionElapsed / questionFadeInDuration);

        if (questionCanvasGroup != null)
            questionCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            _isQuestionAnimating = false;
            if (questionCanvasGroup != null)
                questionCanvasGroup.alpha = 1f;
        }
    }

    private void HideCorrectEffect()
    {
        if (correctSparkleEffect != null)
            correctSparkleEffect.gameObject.SetActive(false);
    }

    #endregion
}
