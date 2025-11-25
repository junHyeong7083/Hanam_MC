using System.Collections;
using UnityEngine;

/// <summary>
/// Step3의 현재 필름 카드 애니메이션 전담 스크립트
/// React Screen3SortActivity 의 motion.div 기준:
/// - Enter : opacity 0 → 1, scale 0.8 → 1,   y 50(아래) → 0
/// - Exit  : opacity 1 → 0, scale 1   → 0.8, y 0        → -50(위)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Director_Problem1_Step3_FilmCardAnimator : MonoBehaviour
{
    [Header("등장 애니메이션")]
    [SerializeField] private float enterDuration = 0.25f;
    [SerializeField] private float enterOffsetY = 50f;   // 아래쪽 50px에서 시작 (DOM: y:50)

    [Header("퇴장 애니메이션")]
    [SerializeField] private float exitDuration = 0.25f;
    [SerializeField] private float exitOffsetY = 50f;     // 위로 50px 이동 (DOM: y:-50)
    [SerializeField] private float exitEndScale = 0.8f;   // 퇴장 시 최종 스케일

    [Header("스케일 설정")]
    [SerializeField] private float enterStartScale = 0.8f;  // 등장 시작 스케일 (DOM: scale:0.8)

    [Header("알파 페이드 (선택)")]
    [SerializeField] private CanvasGroup canvasGroup;       // 있으면 alpha 0↔1 애니메이션

    private RectTransform _rt;
    private Vector2 _baseAnchoredPos;
    private Vector3 _baseScale;
    private bool _initialized;
    private Coroutine _running;

    private void EnsureInit()
    {
        if (_initialized) return;

        _rt = GetComponent<RectTransform>();
        _baseAnchoredPos = _rt.anchoredPosition;
        _baseScale = _rt.localScale;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        _initialized = true;
    }

    /// <summary>
    /// 등장 애니메이션 (자체 코루틴으로 실행, 호출측이 기다릴 필요 없음)
    /// </summary>
    public void PlayEnter()
    {
        if (!gameObject.activeInHierarchy) return;

        EnsureInit();

        if (_running != null)
            StopCoroutine(_running);

        _running = StartCoroutine(EnterRoutine());
    }

    /// <summary>
    /// 퇴장 애니메이션. 호출측에서 StartCoroutine으로 사용:
    /// yield return StartCoroutine(animator.PlayExit());
    /// </summary>
    public IEnumerator PlayExit()
    {
        if (!gameObject.activeInHierarchy)
            yield break;

        EnsureInit();

        if (_running != null)
            StopCoroutine(_running);

        _running = StartCoroutine(ExitRoutine());
        yield return _running;
    }

    private IEnumerator EnterRoutine()
    {
        // React: initial { opacity:0, scale:0.8, y:50 } → animate { opacity:1, scale:1, y:0 }
        // Unity UI: y 아래방향은 -이므로, base - enterOffsetY 에서 시작
        Vector2 startPos = _baseAnchoredPos + new Vector2(0f, -enterOffsetY);
        Vector2 endPos = _baseAnchoredPos;

        Vector3 startScale = new Vector3(enterStartScale, enterStartScale, 1f);
        Vector3 endScale = _baseScale; // 보통 (1,1,1)

        _rt.anchoredPosition = startPos;
        _rt.localScale = startScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < enterDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / enterDuration);
            lerp = Mathf.SmoothStep(0f, 1f, lerp);

            _rt.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);
            _rt.localScale = Vector3.Lerp(startScale, endScale, lerp);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, lerp);

            yield return null;
        }

        _rt.anchoredPosition = endPos;
        _rt.localScale = endScale;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        _running = null;
    }

    private IEnumerator ExitRoutine()
    {
        // React: exit { opacity:0, scale:0.8, y:-50 } 기준
        // → 현재(1,1,1, y0)에서 스케일 0.8, y +exitOffsetY(위)로
        Vector2 startPos = _rt.anchoredPosition;
        Vector2 endPos = _baseAnchoredPos + new Vector2(0f, exitOffsetY); // 위로 이동

        Vector3 startScale = _rt.localScale;
        Vector3 endScale = new Vector3(exitEndScale, exitEndScale, 1f);

        float startAlpha = (canvasGroup != null) ? canvasGroup.alpha : 1f;

        float t = 0f;
        while (t < exitDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / exitDuration);
            lerp = Mathf.SmoothStep(0f, 1f, lerp);

            _rt.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);
            _rt.localScale = Vector3.Lerp(startScale, endScale, lerp);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, lerp);

            yield return null;
        }

        _rt.anchoredPosition = endPos;
        _rt.localScale = endScale;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        _running = null;
    }
}
