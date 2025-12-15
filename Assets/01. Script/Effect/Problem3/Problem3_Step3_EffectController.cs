using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem3 Step3: Effect Controller
/// - 객관식 문제의 이펙트 시퀀스 관리
/// - 힌트 페이드, 정답 효과, 문제 등장 애니메이션 등
/// - 로직과 애니메이션 분리를 위한 중앙 관리자
/// </summary>
public class Problem3_Step3_EffectController : EffectControllerBase
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

    [Header("===== 정답 시 드롭 애니메이션 =====")]
    [SerializeField] private RectTransform dropImage;
    [SerializeField] private float dropStartOffsetY = 500f;   // 화면 위에서 시작할 오프셋
    [SerializeField] private float dropDuration = 0.6f;
    [SerializeField] private Ease dropEase = Ease.OutBounce;

    private Vector2 _dropImageOriginalPos;
    private bool _dropImagePosInitialized;

    // 힌트용 별도 시퀀스 (문제 등장과 독립적으로 동작)
    private Sequence _hintSequence;

    /// <summary>
    /// 힌트 애니메이션 중인지 여부
    /// </summary>
    public bool IsHintAnimating => _hintSequence != null && _hintSequence.IsActive() && _hintSequence.IsPlaying();

    #region Public API

    /// <summary>
    /// 힌트 표시 후 자동 페이드아웃
    /// </summary>
    public void PlayHintSequence(string hintText, Action onComplete = null)
    {
        if (IsHintAnimating) return;

        // 기존 힌트 시퀀스 종료
        _hintSequence?.Kill();

        // 힌트 텍스트 설정
        if (hintLabel != null)
            hintLabel.text = hintText;

        // 힌트 루트 활성화
        if (hintRoot != null)
            hintRoot.SetActive(true);

        // 알파 1로 시작
        if (hintCanvasGroup != null)
            hintCanvasGroup.alpha = 1f;

        // 시퀀스 생성
        _hintSequence = DOTween.Sequence();

        // 1. 표시 시간 대기
        _hintSequence.AppendInterval(hintShowDuration);

        // 2. 페이드아웃
        if (hintCanvasGroup != null)
            _hintSequence.Append(hintCanvasGroup.DOFade(0f, hintFadeDuration));

        // 3. 완료 시 숨김
        _hintSequence.OnComplete(() =>
        {
            if (hintRoot != null)
                hintRoot.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 힌트 즉시 숨김
    /// </summary>
    public void HideHintImmediate()
    {
        _hintSequence?.Kill();
        _hintSequence = null;

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
        if (correctSparkleEffect == null) return;

        // 타겟 버튼이 있으면 그 위치로 이동
        if (targetButton != null)
        {
            correctSparkleEffect.position = targetButton.position;
        }

        correctSparkleEffect.gameObject.SetActive(true);

        // 자동 숨김 (duration 후)
        if (correctEffectDuration > 0f)
        {
            DOVirtual.DelayedCall(correctEffectDuration, HideCorrectEffect);
        }
    }

    /// <summary>
    /// 정답 시 이미지 드롭 애니메이션 재생
    /// - 위에서 아래로 떨어지는 효과
    /// </summary>
    public void PlayDropAnimation(Action onComplete = null)
    {
        if (dropImage == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 원래 위치 저장 (최초 1회)
        if (!_dropImagePosInitialized)
        {
            _dropImageOriginalPos = dropImage.anchoredPosition;
            _dropImagePosInitialized = true;
        }

        // 시작 위치 설정 (원래 위치 + 오프셋)
        var startPos = _dropImageOriginalPos + new Vector2(0f, dropStartOffsetY);
        dropImage.anchoredPosition = startPos;

        // 활성화
        dropImage.gameObject.SetActive(true);

        // 드롭 애니메이션
        dropImage.DOAnchorPosY(_dropImageOriginalPos.y, dropDuration)
            .SetEase(dropEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 드롭 이미지 숨기기 및 위치 리셋
    /// </summary>
    public void HideDropImage()
    {
        if (dropImage != null)
        {
            dropImage.gameObject.SetActive(false);
            if (_dropImagePosInitialized)
            {
                dropImage.anchoredPosition = _dropImageOriginalPos;
            }
        }
    }

    /// <summary>
    /// 문제 등장 페이드인
    /// </summary>
    public void PlayQuestionAppear(Action onComplete = null)
    {
        if (questionCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        questionCanvasGroup.alpha = 0f;

        var seq = CreateSequence();
        seq.Append(questionCanvasGroup.DOFade(1f, questionFadeInDuration));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 문제 즉시 표시
    /// </summary>
    public void ShowQuestionImmediate()
    {
        KillCurrentSequence();

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
        HideDropImage();
    }

    #endregion

    #region Private

    private void HideCorrectEffect()
    {
        if (correctSparkleEffect != null)
            correctSparkleEffect.gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _hintSequence?.Kill();
        _hintSequence = null;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _hintSequence?.Kill();
        _hintSequence = null;
    }

    #endregion
}
