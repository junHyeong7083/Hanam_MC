using System;
using UnityEngine;

/// <summary>
/// Problem5 Step3: Effect Controller
/// - 정답 선택 시 NPC 반응 등장 애니메이션
/// - NPC 캐릭터 스케일 펄스 + 글로우
/// - NPC 응답 버블 슬라이드 업 + 페이드 인
/// </summary>
public class Problem5_Step3_EffectController : MonoBehaviour
{
    [Header("===== NPC 캐릭터 =====")]
    [SerializeField] private RectTransform npcCharacterRect;
    [SerializeField] private GameObject npcGlowImage;
    [SerializeField] private float characterPulseDuration = 0.5f;
    [SerializeField] private float characterPulseScale = 1.05f;

    [Header("===== NPC 응답 버블 =====")]
    [SerializeField] private RectTransform npcResponseRect;
    [SerializeField] private CanvasGroup npcResponseCanvasGroup;
    [SerializeField] private float responseSlideUpDistance = 20f;
    [SerializeField] private float responseFadeInDuration = 0.3f;

    // 상태
    private bool _isAnimating;
    private float _elapsed;
    private AnimPhase _phase;
    private Action _onComplete;

    // 초기값 저장
    private Vector3 _npcCharacterBaseScale;
    private Vector2 _npcResponseBasePos;
    private bool _initialized;

    private enum AnimPhase
    {
        Idle,
        NpcPulse,
        ResponseFadeIn
    }

    public bool IsAnimating => _isAnimating;

    private void Awake()
    {
        SaveInitialState();
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.deltaTime;

        switch (_phase)
        {
            case AnimPhase.NpcPulse:
                ProcessNpcPulse();
                break;
            case AnimPhase.ResponseFadeIn:
                ProcessResponseFadeIn();
                break;
        }
    }

    #region Public API

    /// <summary>
    /// 초기 상태 저장
    /// </summary>
    public void SaveInitialState()
    {
        if (_initialized) return;

        if (npcCharacterRect != null)
            _npcCharacterBaseScale = npcCharacterRect.localScale;

        if (npcResponseRect != null)
            _npcResponseBasePos = npcResponseRect.anchoredPosition;

        _initialized = true;
    }

    /// <summary>
    /// NPC 반응 등장 애니메이션 재생 (정답 선택 시 호출)
    /// </summary>
    public void PlayNpcResponseAppear(Action onComplete = null)
    {
        if (_isAnimating) return;

        SaveInitialState();

        _onComplete = onComplete;
        _isAnimating = true;
        _elapsed = 0f;

        // 글로우 활성화
        if (npcGlowImage != null)
            npcGlowImage.SetActive(true);

        // 응답 버블 초기 상태 (아래에서 시작, 투명)
        if (npcResponseRect != null)
            npcResponseRect.anchoredPosition = _npcResponseBasePos + Vector2.down * responseSlideUpDistance;

        if (npcResponseCanvasGroup != null)
            npcResponseCanvasGroup.alpha = 0f;

        _phase = AnimPhase.NpcPulse;
    }

    /// <summary>
    /// 스텝 진입 시 리셋
    /// </summary>
    public void ResetAll()
    {
        _isAnimating = false;
        _phase = AnimPhase.Idle;
        _elapsed = 0f;

        if (npcCharacterRect != null && _initialized)
            npcCharacterRect.localScale = _npcCharacterBaseScale;

        if (npcGlowImage != null)
            npcGlowImage.SetActive(false);

        if (npcResponseRect != null && _initialized)
            npcResponseRect.anchoredPosition = _npcResponseBasePos;

        if (npcResponseCanvasGroup != null)
            npcResponseCanvasGroup.alpha = 1f;
    }

    #endregion

    #region Animation Processing

    private void ProcessNpcPulse()
    {
        float t = Mathf.Clamp01(_elapsed / characterPulseDuration);

        // 스케일: 1 → 1.05 → 1 (sin 곡선)
        if (npcCharacterRect != null)
        {
            float s = Mathf.Sin(t * Mathf.PI);
            float scale = Mathf.Lerp(1f, characterPulseScale, s);
            npcCharacterRect.localScale = _npcCharacterBaseScale * scale;
        }

        if (t >= 1f)
        {
            // 캐릭터 스케일 원복
            if (npcCharacterRect != null)
                npcCharacterRect.localScale = _npcCharacterBaseScale;

            // 응답 버블 페이드인 시작
            _phase = AnimPhase.ResponseFadeIn;
            _elapsed = 0f;
        }
    }

    private void ProcessResponseFadeIn()
    {
        float t = Mathf.Clamp01(_elapsed / responseFadeInDuration);

        // 슬라이드 업
        if (npcResponseRect != null)
        {
            Vector2 startPos = _npcResponseBasePos + Vector2.down * responseSlideUpDistance;
            npcResponseRect.anchoredPosition = Vector2.Lerp(startPos, _npcResponseBasePos, EaseOutQuad(t));
        }

        // 페이드 인
        if (npcResponseCanvasGroup != null)
            npcResponseCanvasGroup.alpha = t;

        if (t >= 1f)
        {
            CompleteAnimation();
        }
    }

    private void CompleteAnimation()
    {
        _isAnimating = false;
        _phase = AnimPhase.Idle;

        _onComplete?.Invoke();
        _onComplete = null;
    }

    #endregion

    #region Easing

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    #endregion
}
