using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 텍스트 투명도 펄스 애니메이션
/// - 안내 텍스트가 깜빡이며 주목을 끄는 효과
/// - CanvasGroup 없이 Text 컴포넌트 직접 제어
///
/// [사용처]
/// - Problem2 Step1: "마음 렌즈를 필름 위로 드래그하세요" 안내 텍스트
/// - 모든 Step의 안내/힌트 텍스트
/// </summary>
public class TextOpacityPulse : MonoBehaviour
{
    [Header("===== 펄스 설정 =====")]
    [SerializeField] private float minOpacity = 0.5f;
    [SerializeField] private float maxOpacity = 1f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool playOnEnable = true;

    // 내부 상태
    private Graphic _graphic; // Image, Text 등 모든 UI 요소 지원
    private Color _baseColor;
    private Tween _tween;

    private void Awake()
    {
        _graphic = GetComponent<Graphic>();

        if (_graphic != null)
            _baseColor = _graphic.color;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void KillTween()
    {
        _tween?.Kill();
        _tween = null;
    }

    #region Public API

    public void Play()
    {
        KillTween();

        if (_graphic == null) return;

        // 시작 알파 설정
        Color c = _baseColor;
        c.a = minOpacity;
        _graphic.color = c;

        // min → max 펄스 (Yoyo로 왕복)
        _tween = _graphic
            .DOFade(maxOpacity, duration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void Stop()
    {
        KillTween();

        // 원래 색상 복원
        if (_graphic != null)
            _graphic.color = _baseColor;
    }

    #endregion
}
