using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem4 Step3: Effect Controller
/// - 반박 질문 화면의 이펙트 시퀀스 관리
/// - 스텝 등장 애니메이션 (필름 카드 + 질문 패널)
/// - 완료 시나리오 카드 등장 (슬라이드 업 + 글로우)
/// - 완료 스파클
/// </summary>
public class Problem4_Step3_EffectController : MonoBehaviour
{
    [Header("===== 스텝 등장 - 필름 카드 =====")]
    [SerializeField] private RectTransform filmCardRect;
    [SerializeField] private CanvasGroup filmCardCanvasGroup;
    [SerializeField] private float filmCardSlideDistance = 30f;
    [SerializeField] private float filmCardAppearDuration = 0.5f;
    [SerializeField] private float filmCardAppearDelay = 0.3f;

    [Header("===== 스텝 등장 - 질문 패널 =====")]
    [SerializeField] private RectTransform questionPanelRect;
    [SerializeField] private CanvasGroup questionPanelCanvasGroup;
    [SerializeField] private float questionSlideDistance = 30f;
    [SerializeField] private float questionAppearDuration = 0.4f;
    [SerializeField] private float questionAppearDelay = 0.5f;

    [Header("===== 스텝 등장 - 입력 패널 =====")]
    [SerializeField] private RectTransform inputPanelRect;
    [SerializeField] private CanvasGroup inputPanelCanvasGroup;
    [SerializeField] private float inputSlideDistance = 30f;
    [SerializeField] private float inputAppearDuration = 0.4f;
    [SerializeField] private float inputAppearDelay = 0.6f;

    [Header("===== 완료 시나리오 카드 =====")]
    [SerializeField] private RectTransform scenarioCardRect;
    [SerializeField] private CanvasGroup scenarioCardCanvasGroup;
    [SerializeField] private Image scenarioGlowImage;
    [SerializeField] private float scenarioSlideDistance = 50f;
    [SerializeField] private float scenarioAppearDuration = 0.5f;
    [SerializeField] private float scenarioStartScale = 0.9f;
    [SerializeField] private float glowPulseDuration = 2f;
    [SerializeField] private float glowMinAlpha = 0.3f;
    [SerializeField] private float glowMaxAlpha = 0.6f;

    // 상태
    private bool _isAnimating;
    private float _elapsed;
    private AnimPhase _phase;
    private Action _onCompleteCallback;

    // 등장 애니메이션용
    private Vector2 _filmCardDefaultPos;
    private Vector2 _questionPanelDefaultPos;
    private Vector2 _inputPanelDefaultPos;
    private Vector2 _scenarioCardDefaultPos;
    private bool _defaultPosSaved;

    // 글로우 펄스
    private bool _glowPulsing;
    private float _glowElapsed;

    private enum AnimPhase
    {
        Idle,
        // 스텝 등장
        StepAppear_FilmCard,
        StepAppear_QuestionPanel,
        StepAppear_InputPanel,
        // 시나리오 카드
        ScenarioCardAppear
    }

    public bool IsAnimating => _isAnimating;

    private void Awake()
    {
        SaveDefaultPositions();
    }

    private void Update()
    {
        if (_isAnimating)
        {
            _elapsed += Time.deltaTime;
            ProcessAnimation();
        }

        if (_glowPulsing)
        {
            ProcessGlowPulse();
        }
    }

    #region Public API

    /// <summary>
    /// 기본 위치 저장
    /// </summary>
    public void SaveDefaultPositions()
    {
        if (_defaultPosSaved) return;

        if (filmCardRect != null)
            _filmCardDefaultPos = filmCardRect.anchoredPosition;
        if (questionPanelRect != null)
            _questionPanelDefaultPos = questionPanelRect.anchoredPosition;
        if (inputPanelRect != null)
            _inputPanelDefaultPos = inputPanelRect.anchoredPosition;
        if (scenarioCardRect != null)
            _scenarioCardDefaultPos = scenarioCardRect.anchoredPosition;

        _defaultPosSaved = true;
    }

    /// <summary>
    /// 스텝 등장 애니메이션 (필름 카드 → 질문 패널 → 입력 패널 순차 등장)
    /// </summary>
    public void PlayStepAppearAnimation(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveDefaultPositions();

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        // 초기 상태: 모두 숨김 + 위치 오프셋
        if (filmCardRect != null)
        {
            filmCardRect.anchoredPosition = _filmCardDefaultPos + new Vector2(0f, -filmCardSlideDistance);
            if (filmCardCanvasGroup != null)
                filmCardCanvasGroup.alpha = 0f;
        }

        if (questionPanelRect != null)
        {
            questionPanelRect.anchoredPosition = _questionPanelDefaultPos + new Vector2(-questionSlideDistance, 0f);
            if (questionPanelCanvasGroup != null)
                questionPanelCanvasGroup.alpha = 0f;
        }

        if (inputPanelRect != null)
        {
            inputPanelRect.anchoredPosition = _inputPanelDefaultPos + new Vector2(inputSlideDistance, 0f);
            if (inputPanelCanvasGroup != null)
                inputPanelCanvasGroup.alpha = 0f;
        }

        _phase = AnimPhase.StepAppear_FilmCard;
    }

    /// <summary>
    /// 완료 시나리오 카드 등장 (슬라이드 업 + 스케일 + 글로우)
    /// </summary>
    public void PlayScenarioCardAppear(Action onComplete = null)
    {
        if (_isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveDefaultPositions();

        _onCompleteCallback = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        // 초기 상태
        if (scenarioCardRect != null)
        {
            scenarioCardRect.anchoredPosition = _scenarioCardDefaultPos + new Vector2(0f, -scenarioSlideDistance);
            scenarioCardRect.localScale = Vector3.one * scenarioStartScale;
            scenarioCardRect.gameObject.SetActive(true);
        }

        if (scenarioCardCanvasGroup != null)
            scenarioCardCanvasGroup.alpha = 0f;

        if (scenarioGlowImage != null)
        {
            var c = scenarioGlowImage.color;
            c.a = 0f;
            scenarioGlowImage.color = c;
        }

        _phase = AnimPhase.ScenarioCardAppear;
    }

    /// <summary>
    /// 시나리오 카드 숨김
    /// </summary>
    public void HideScenarioCard()
    {
        _glowPulsing = false;

        if (scenarioCardRect != null)
            scenarioCardRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 모든 이펙트 리셋
    /// </summary>
    public void ResetAll()
    {
        _isAnimating = false;
        _glowPulsing = false;
        _phase = AnimPhase.Idle;
        _elapsed = 0f;

        HideScenarioCard();

        // 위치 복원
        if (filmCardRect != null && _defaultPosSaved)
        {
            filmCardRect.anchoredPosition = _filmCardDefaultPos;
            if (filmCardCanvasGroup != null)
                filmCardCanvasGroup.alpha = 1f;
        }

        if (questionPanelRect != null && _defaultPosSaved)
        {
            questionPanelRect.anchoredPosition = _questionPanelDefaultPos;
            if (questionPanelCanvasGroup != null)
                questionPanelCanvasGroup.alpha = 1f;
        }

        if (inputPanelRect != null && _defaultPosSaved)
        {
            inputPanelRect.anchoredPosition = _inputPanelDefaultPos;
            if (inputPanelCanvasGroup != null)
                inputPanelCanvasGroup.alpha = 1f;
        }
    }

    #endregion

    #region Animation Processing

    private void ProcessAnimation()
    {
        switch (_phase)
        {
            case AnimPhase.StepAppear_FilmCard:
                ProcessFilmCardAppear();
                break;
            case AnimPhase.StepAppear_QuestionPanel:
                ProcessQuestionPanelAppear();
                break;
            case AnimPhase.StepAppear_InputPanel:
                ProcessInputPanelAppear();
                break;
            case AnimPhase.ScenarioCardAppear:
                ProcessScenarioCardAppear();
                break;
        }
    }

    private void ProcessFilmCardAppear()
    {
        // 딜레이 대기
        if (_elapsed < filmCardAppearDelay)
            return;

        float localElapsed = _elapsed - filmCardAppearDelay;
        float t = Mathf.Clamp01(localElapsed / filmCardAppearDuration);
        float eased = EaseOutQuad(t);

        if (filmCardRect != null)
        {
            Vector2 startPos = _filmCardDefaultPos + new Vector2(0f, -filmCardSlideDistance);
            filmCardRect.anchoredPosition = Vector2.Lerp(startPos, _filmCardDefaultPos, eased);
        }

        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = eased;

        if (t >= 1f)
        {
            _phase = AnimPhase.StepAppear_QuestionPanel;
            // elapsed는 계속 누적
        }
    }

    private void ProcessQuestionPanelAppear()
    {
        // 딜레이 대기
        if (_elapsed < questionAppearDelay)
            return;

        float localElapsed = _elapsed - questionAppearDelay;
        float t = Mathf.Clamp01(localElapsed / questionAppearDuration);
        float eased = EaseOutQuad(t);

        if (questionPanelRect != null)
        {
            Vector2 startPos = _questionPanelDefaultPos + new Vector2(-questionSlideDistance, 0f);
            questionPanelRect.anchoredPosition = Vector2.Lerp(startPos, _questionPanelDefaultPos, eased);
        }

        if (questionPanelCanvasGroup != null)
            questionPanelCanvasGroup.alpha = eased;

        if (t >= 1f)
        {
            _phase = AnimPhase.StepAppear_InputPanel;
        }
    }

    private void ProcessInputPanelAppear()
    {
        // 딜레이 대기
        if (_elapsed < inputAppearDelay)
            return;

        float localElapsed = _elapsed - inputAppearDelay;
        float t = Mathf.Clamp01(localElapsed / inputAppearDuration);
        float eased = EaseOutQuad(t);

        if (inputPanelRect != null)
        {
            Vector2 startPos = _inputPanelDefaultPos + new Vector2(inputSlideDistance, 0f);
            inputPanelRect.anchoredPosition = Vector2.Lerp(startPos, _inputPanelDefaultPos, eased);
        }

        if (inputPanelCanvasGroup != null)
            inputPanelCanvasGroup.alpha = eased;

        if (t >= 1f)
        {
            CompleteAnimation();
        }
    }

    private void ProcessScenarioCardAppear()
    {
        float t = Mathf.Clamp01(_elapsed / scenarioAppearDuration);
        float eased = EaseOutBack(t);

        if (scenarioCardRect != null)
        {
            // 슬라이드 업
            Vector2 startPos = _scenarioCardDefaultPos + new Vector2(0f, -scenarioSlideDistance);
            scenarioCardRect.anchoredPosition = Vector2.Lerp(startPos, _scenarioCardDefaultPos, EaseOutQuad(t));

            // 스케일
            float scale = Mathf.Lerp(scenarioStartScale, 1f, eased);
            scenarioCardRect.localScale = Vector3.one * scale;
        }

        if (scenarioCardCanvasGroup != null)
            scenarioCardCanvasGroup.alpha = EaseOutQuad(t);

        // 글로우 페이드인
        if (scenarioGlowImage != null)
        {
            var c = scenarioGlowImage.color;
            c.a = Mathf.Lerp(0f, glowMinAlpha, t);
            scenarioGlowImage.color = c;
        }

        if (t >= 1f)
        {
            // 글로우 펄스 시작
            _glowPulsing = true;
            _glowElapsed = 0f;

            CompleteAnimation();
        }
    }

    private void ProcessGlowPulse()
    {
        if (scenarioGlowImage == null) return;

        _glowElapsed += Time.deltaTime;

        // 사인파로 부드러운 펄스
        float t = (_glowElapsed % glowPulseDuration) / glowPulseDuration;
        float pulse = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f; // 0~1 범위

        var c = scenarioGlowImage.color;
        c.a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, pulse);
        scenarioGlowImage.color = c;
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

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    #endregion
}
