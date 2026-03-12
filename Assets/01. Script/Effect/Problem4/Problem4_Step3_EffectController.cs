using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem4_Step3_EffectController - 문제4 스텝3(질문 응답)의 이펙트 관리자
///
/// 【역할】 질문 카드의 필름 스타일 좌우 이동 애니메이션(Right→Center 등장, Center→Left 퇴장)과,
///          스텝 전체의 순차 등장 애니메이션(필름 카드→입력 패널)을 관리한다.
///          질문 카드는 별도 시퀀스로 메인 시퀀스와 독립 동작.
/// 【사용 위치】 ProblemScene - Problem4 Step3 (질문에 대한 응답을 입력하는 스텝)
/// 【트리거】 Logic 클래스에서 PlayQuestionEnter/Exit(), PlayStepAppearAnimation() 호출
/// 【의존성】 EffectControllerBase(상속), DOTween, questionCardRect, filmCardRect, inputPanelRect 등
/// </summary>
public class Problem4_Step3_EffectController : EffectControllerBase
{
    [Header("===== 질문 카드 필름 애니메이션 =====")]
    [SerializeField] private RectTransform questionCardRect;
    [SerializeField] private CanvasGroup questionCardCanvasGroup;
    [SerializeField] private RectTransform leftPoint;
    [SerializeField] private RectTransform centerPoint;
    [SerializeField] private RectTransform rightPoint;
    [SerializeField] private float questionMoveDuration = 0.4f;

    [Header("===== 스텝 등장 - 필름 카드 =====")]
    [SerializeField] private RectTransform filmCardRect;
    [SerializeField] private CanvasGroup filmCardCanvasGroup;
    [SerializeField] private float filmCardSlideDistance = 30f;
    [SerializeField] private float filmCardAppearDuration = 0.5f;
    [SerializeField] private float filmCardAppearDelay = 0.3f;

    [Header("===== 스텝 등장 - 입력 패널 =====")]
    [SerializeField] private RectTransform inputPanelRect;
    [SerializeField] private CanvasGroup inputPanelCanvasGroup;
    [SerializeField] private float inputSlideDistance = 30f;
    [SerializeField] private float inputAppearDuration = 0.4f;
    [SerializeField] private float inputAppearDelay = 0.6f;

    // 기본 위치
    private Vector2 _filmCardDefaultPos;
    private Vector2 _inputPanelDefaultPos;
    private bool _defaultPosSaved;

    // 질문 카드 애니메이션용 별도 시퀀스
    private Sequence _questionSequence;

    private void Awake()
    {
        SaveDefaultPositions();
    }

    #region Public API

    public void SaveDefaultPositions()
    {
        if (_defaultPosSaved) return;

        if (filmCardRect != null)
            _filmCardDefaultPos = filmCardRect.anchoredPosition;
        if (inputPanelRect != null)
            _inputPanelDefaultPos = inputPanelRect.anchoredPosition;

        _defaultPosSaved = true;
    }

    /// <summary>
    /// 질문 카드 등장: Right → Center, alpha 0→1
    /// </summary>
    public void PlayQuestionEnter(Action onComplete = null)
    {
        KillQuestionSequence();

        if (questionCardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2 startPos = rightPoint != null
            ? rightPoint.anchoredPosition
            : questionCardRect.anchoredPosition + new Vector2(500f, 0f);

        Vector2 endPos = centerPoint != null
            ? centerPoint.anchoredPosition
            : Vector2.zero;

        questionCardRect.anchoredPosition = startPos;
        if (questionCardCanvasGroup != null)
            questionCardCanvasGroup.alpha = 0f;

        _questionSequence = DOTween.Sequence();
        _questionSequence.Append(questionCardRect.DOAnchorPos(endPos, questionMoveDuration).SetEase(Ease.OutQuad));
        if (questionCardCanvasGroup != null)
            _questionSequence.Join(questionCardCanvasGroup.DOFade(1f, questionMoveDuration));

        _questionSequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 질문 카드 퇴장: Center → Left, alpha 1→0
    /// </summary>
    public void PlayQuestionExit(Action onComplete = null)
    {
        KillQuestionSequence();

        if (questionCardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2 endPos = leftPoint != null
            ? leftPoint.anchoredPosition
            : questionCardRect.anchoredPosition + new Vector2(-500f, 0f);

        _questionSequence = DOTween.Sequence();
        _questionSequence.Append(questionCardRect.DOAnchorPos(endPos, questionMoveDuration).SetEase(Ease.InQuad));
        if (questionCardCanvasGroup != null)
            _questionSequence.Join(questionCardCanvasGroup.DOFade(0f, questionMoveDuration));

        _questionSequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 스텝 등장 애니메이션 (필름 카드 → 입력 패널 순차 등장)
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

        if (filmCardRect != null)
        {
            filmCardRect.anchoredPosition = _filmCardDefaultPos + new Vector2(0f, -filmCardSlideDistance);
            if (filmCardCanvasGroup != null)
                filmCardCanvasGroup.alpha = 0f;
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

        // 2. 입력 패널 등장
        float inputDelay = inputAppearDelay - filmCardAppearDelay - filmCardAppearDuration;
        if (inputDelay > 0f)
            seq.AppendInterval(inputDelay);

        if (inputPanelRect != null)
        {
            seq.Append(inputPanelRect.DOAnchorPos(_inputPanelDefaultPos, inputAppearDuration).SetEase(Ease.OutQuad));
            if (inputPanelCanvasGroup != null)
                seq.Join(inputPanelCanvasGroup.DOFade(1f, inputAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 모든 이펙트 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        KillQuestionSequence();

        if (filmCardRect != null && _defaultPosSaved)
        {
            filmCardRect.anchoredPosition = _filmCardDefaultPos;
            if (filmCardCanvasGroup != null)
                filmCardCanvasGroup.alpha = 1f;
        }

        if (inputPanelRect != null && _defaultPosSaved)
        {
            inputPanelRect.anchoredPosition = _inputPanelDefaultPos;
            if (inputPanelCanvasGroup != null)
                inputPanelCanvasGroup.alpha = 1f;
        }
    }

    #endregion

    #region Internal

    private void KillQuestionSequence()
    {
        _questionSequence?.Kill();
        _questionSequence = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        KillQuestionSequence();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillQuestionSequence();
    }

    #endregion
}
