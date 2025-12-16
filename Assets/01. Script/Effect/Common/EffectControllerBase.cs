using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 모든 EffectController의 베이스 클래스
/// - DOTween Sequence 관리
/// - 인트로 연출 (슬라이드 애니메이션)
/// - 공통 상태 및 유틸리티 메서드 제공
/// </summary>
public abstract class EffectControllerBase : MonoBehaviour
{
    #region Intro Animation System

    public enum IntroAnimationType { Slide, Scale }
    public enum SlideDirection { Up, Down, Left, Right }

    [Serializable]
    public class IntroElement
    {
        public RectTransform target;
        public IntroAnimationType animationType = IntroAnimationType.Slide;
        public float delay = 0f;
        public float duration = 0.3f;

        // Slide 타입 전용
        public SlideDirection direction = SlideDirection.Up;
        public float distance = 50f;

        // Scale 타입 전용
        [Range(0f, 1f)]
        public float startScale = 0.3f;
    }

    [Header("===== 인트로 연출 =====")]
    [SerializeField] protected IntroElement[] introElements;
    [SerializeField] protected bool playOnEnable = true;

    // 인트로 관련 상태
    protected Vector2[] _introBasePositions;
    protected Vector3[] _introBaseScales;
    protected Sequence _introSequence;
    protected bool _introInitialized;

    #endregion

    // 현재 실행 중인 시퀀스
    protected Sequence _currentSequence;

    /// <summary>
    /// 애니메이션 진행 중 여부
    /// </summary>
    public bool IsAnimating => _currentSequence != null && _currentSequence.IsActive() && _currentSequence.IsPlaying();

    /// <summary>
    /// 현재 시퀀스 강제 종료
    /// </summary>
    protected void KillCurrentSequence()
    {
        if (_currentSequence != null)
        {
            _currentSequence.Kill();
            _currentSequence = null;
        }
    }

    /// <summary>
    /// 새 시퀀스 시작 (기존 시퀀스 자동 Kill)
    /// </summary>
    protected Sequence CreateSequence()
    {
        KillCurrentSequence();
        _currentSequence = DOTween.Sequence();
        return _currentSequence;
    }

    #region Intro Animation Methods

    protected virtual void OnEnable()
    {
        if (playOnEnable)
            PlayIntro();
    }

    /// <summary>
    /// 인트로 요소들의 원래 위치/스케일 저장
    /// </summary>
    protected void SaveIntroBasePositions()
    {
        if (_introInitialized) return;

        if (introElements != null && introElements.Length > 0)
        {
            _introBasePositions = new Vector2[introElements.Length];
            _introBaseScales = new Vector3[introElements.Length];
            for (int i = 0; i < introElements.Length; i++)
            {
                if (introElements[i]?.target != null)
                {
                    _introBasePositions[i] = introElements[i].target.anchoredPosition;
                    _introBaseScales[i] = introElements[i].target.localScale;
                }
            }
        }

        _introInitialized = true;
    }

    /// <summary>
    /// 인트로 연출 재생
    /// </summary>
    public void PlayIntro(Action onComplete = null)
    {
        SaveIntroBasePositions();

        if (introElements == null || introElements.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        KillIntroSequence();
        _introSequence = DOTween.Sequence();

        for (int i = 0; i < introElements.Length; i++)
        {
            var elem = introElements[i];
            if (elem?.target == null) continue;

            int index = i;
            Vector2 basePos = _introBasePositions != null && i < _introBasePositions.Length
                ? _introBasePositions[i]
                : elem.target.anchoredPosition;
            Vector3 baseScale = _introBaseScales != null && i < _introBaseScales.Length
                ? _introBaseScales[i]
                : elem.target.localScale;

            // CanvasGroup 가져오기 (없으면 추가)
            var canvasGroup = GetOrAddCanvasGroup(elem.target.gameObject);

            if (elem.animationType == IntroAnimationType.Slide)
            {
                // --- Slide 타입 ---
                Vector2 startPos = basePos + GetDirectionOffset(elem.direction, elem.distance);

                _introSequence.InsertCallback(0f, () =>
                {
                    elem.target.anchoredPosition = startPos;
                    if (canvasGroup != null) canvasGroup.alpha = 0f;
                    elem.target.gameObject.SetActive(true);
                });

                _introSequence.Insert(elem.delay, elem.target
                    .DOAnchorPos(basePos, elem.duration)
                    .SetEase(Ease.OutQuad));

                // 알파 페이드인
                if (canvasGroup != null)
                {
                    _introSequence.Insert(elem.delay, canvasGroup
                        .DOFade(1f, elem.duration)
                        .SetEase(Ease.OutQuad));
                }
            }
            else // Scale
            {
                // --- Scale 타입 ---
                Vector3 startScaleVec = baseScale * elem.startScale;

                _introSequence.InsertCallback(0f, () =>
                {
                    elem.target.localScale = startScaleVec;
                    if (canvasGroup != null) canvasGroup.alpha = 0f;
                    elem.target.gameObject.SetActive(true);
                });

                _introSequence.Insert(elem.delay, elem.target
                    .DOScale(baseScale, elem.duration)
                    .SetEase(Ease.OutBack));

                // 알파 페이드인
                if (canvasGroup != null)
                {
                    _introSequence.Insert(elem.delay, canvasGroup
                        .DOFade(1f, elem.duration)
                        .SetEase(Ease.OutQuad));
                }
            }
        }

        _introSequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 방향에 따른 시작 오프셋 (이동 방향의 반대에서 시작)
    /// - Up: 아래에서 시작 → 위로 이동
    /// - Down: 위에서 시작 → 아래로 이동
    /// - Left: 오른쪽에서 시작 → 왼쪽으로 이동
    /// - Right: 왼쪽에서 시작 → 오른쪽으로 이동
    /// </summary>
    protected Vector2 GetDirectionOffset(SlideDirection dir, float distance)
    {
        switch (dir)
        {
            case SlideDirection.Up: return Vector2.down * distance;
            case SlideDirection.Down: return Vector2.up * distance;
            case SlideDirection.Left: return Vector2.right * distance;
            case SlideDirection.Right: return Vector2.left * distance;
            default: return Vector2.zero;
        }
    }

    protected void KillIntroSequence()
    {
        _introSequence?.Kill();
        _introSequence = null;
    }

    /// <summary>
    /// 인트로 요소들 원래 위치/스케일/알파로 리셋
    /// </summary>
    protected void ResetIntroElements()
    {
        KillIntroSequence();

        if (introElements != null)
        {
            for (int i = 0; i < introElements.Length; i++)
            {
                if (introElements[i]?.target != null)
                {
                    if (_introBasePositions != null && i < _introBasePositions.Length)
                        introElements[i].target.anchoredPosition = _introBasePositions[i];
                    if (_introBaseScales != null && i < _introBaseScales.Length)
                        introElements[i].target.localScale = _introBaseScales[i];

                    // 알파도 1로 복원
                    var cg = introElements[i].target.GetComponent<CanvasGroup>();
                    if (cg != null)
                        cg.alpha = 1f;
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// 오브젝트 비활성화 시 시퀀스 정리
    /// </summary>
    protected virtual void OnDisable()
    {
        KillCurrentSequence();
        KillIntroSequence();
    }

    /// <summary>
    /// 오브젝트 파괴 시 시퀀스 정리
    /// </summary>
    protected virtual void OnDestroy()
    {
        KillCurrentSequence();
        KillIntroSequence();
    }

    #region 공통 유틸리티

    /// <summary>
    /// CanvasGroup 페이드
    /// </summary>
    protected Tween DOFade(CanvasGroup cg, float endValue, float duration)
    {
        if (cg == null) return null;
        return cg.DOFade(endValue, duration);
    }

    /// <summary>
    /// RectTransform 앵커 위치 이동
    /// </summary>
    protected Tween DOAnchorPos(RectTransform rt, Vector2 endValue, float duration)
    {
        if (rt == null) return null;
        return rt.DOAnchorPos(endValue, duration);
    }

    /// <summary>
    /// Transform 스케일
    /// </summary>
    protected Tween DOScale(Transform t, float endValue, float duration)
    {
        if (t == null) return null;
        return t.DOScale(endValue, duration);
    }

    /// <summary>
    /// Transform 스케일 (Vector3)
    /// </summary>
    protected Tween DOScale(Transform t, Vector3 endValue, float duration)
    {
        if (t == null) return null;
        return t.DOScale(endValue, duration);
    }

    /// <summary>
    /// GameObject 활성화/비활성화 콜백
    /// </summary>
    protected TweenCallback SetActiveCallback(GameObject go, bool active)
    {
        return () => { if (go != null) go.SetActive(active); };
    }

    /// <summary>
    /// CanvasGroup 가져오기 (없으면 추가)
    /// </summary>
    protected CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    #endregion
}
