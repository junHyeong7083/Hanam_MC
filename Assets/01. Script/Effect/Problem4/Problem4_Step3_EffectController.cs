using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem4 Step3: Effect Controller
/// - 반박 질문 화면의 이펙트 시퀀스 관리
/// - 스텝 등장 애니메이션 (필름 카드 + 질문 패널)
/// - 완료 시나리오 카드 등장 (슬라이드 업 + 글로우)
/// - 완료 스파클
/// </summary>
public class Problem4_Step3_EffectController : EffectControllerBase
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

    // 기본 위치
    private Vector2 _filmCardDefaultPos;
    private Vector2 _questionPanelDefaultPos;
    private Vector2 _inputPanelDefaultPos;
    private Vector2 _scenarioCardDefaultPos;
    private bool _defaultPosSaved;

    // 글로우 펄스용 별도 Tween
    private Tween _glowPulseTween;

    private void Awake()
    {
        SaveDefaultPositions();
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
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveDefaultPositions();

        var seq = CreateSequence();

        // 초기 상태 설정
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

        // 1. 필름 카드 등장
        seq.AppendInterval(filmCardAppearDelay);
        if (filmCardRect != null)
        {
            seq.Append(filmCardRect.DOAnchorPos(_filmCardDefaultPos, filmCardAppearDuration).SetEase(Ease.OutQuad));
            if (filmCardCanvasGroup != null)
                seq.Join(filmCardCanvasGroup.DOFade(1f, filmCardAppearDuration));
        }

        // 2. 질문 패널 등장 (딜레이 계산)
        float questionDelayFromFilm = questionAppearDelay - filmCardAppearDelay - filmCardAppearDuration;
        if (questionDelayFromFilm > 0f)
            seq.AppendInterval(questionDelayFromFilm);

        if (questionPanelRect != null)
        {
            seq.Append(questionPanelRect.DOAnchorPos(_questionPanelDefaultPos, questionAppearDuration).SetEase(Ease.OutQuad));
            if (questionPanelCanvasGroup != null)
                seq.Join(questionPanelCanvasGroup.DOFade(1f, questionAppearDuration));
        }

        // 3. 입력 패널 등장 (딜레이 계산)
        float inputDelayFromQuestion = inputAppearDelay - questionAppearDelay - questionAppearDuration;
        if (inputDelayFromQuestion > 0f)
            seq.AppendInterval(inputDelayFromQuestion);

        if (inputPanelRect != null)
        {
            seq.Append(inputPanelRect.DOAnchorPos(_inputPanelDefaultPos, inputAppearDuration).SetEase(Ease.OutQuad));
            if (inputPanelCanvasGroup != null)
                seq.Join(inputPanelCanvasGroup.DOFade(1f, inputAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 완료 시나리오 카드 등장 (슬라이드 업 + 스케일 + 글로우)
    /// </summary>
    public void PlayScenarioCardAppear(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveDefaultPositions();

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

        var seq = CreateSequence();

        // 슬라이드 업 + 페이드인
        if (scenarioCardRect != null)
        {
            seq.Append(scenarioCardRect.DOAnchorPos(_scenarioCardDefaultPos, scenarioAppearDuration).SetEase(Ease.OutQuad));
            seq.Join(scenarioCardRect.DOScale(1f, scenarioAppearDuration).SetEase(Ease.OutBack));
        }

        if (scenarioCardCanvasGroup != null)
            seq.Join(scenarioCardCanvasGroup.DOFade(1f, scenarioAppearDuration).SetEase(Ease.OutQuad));

        // 글로우 페이드인
        if (scenarioGlowImage != null)
            seq.Join(scenarioGlowImage.DOFade(glowMinAlpha, scenarioAppearDuration));

        // 글로우 펄스 시작
        seq.AppendCallback(StartGlowPulse);

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 시나리오 카드 숨김
    /// </summary>
    public void HideScenarioCard()
    {
        StopGlowPulse();

        if (scenarioCardRect != null)
            scenarioCardRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 모든 이펙트 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        StopGlowPulse();

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

    #region Glow Pulse

    private void StartGlowPulse()
    {
        if (scenarioGlowImage == null) return;

        StopGlowPulse();

        // 무한 펄스: min → max → min 반복
        _glowPulseTween = scenarioGlowImage
            .DOFade(glowMaxAlpha, glowPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopGlowPulse()
    {
        _glowPulseTween?.Kill();
        _glowPulseTween = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopGlowPulse();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopGlowPulse();
    }

    #endregion
}
