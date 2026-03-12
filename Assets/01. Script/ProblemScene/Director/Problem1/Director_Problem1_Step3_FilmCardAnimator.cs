using System.Collections;
using UnityEngine;

/// <summary>
/// Director_Problem1_Step3_FilmCardAnimator - 필름 카드 등장/퇴장 애니메이션 컨트롤러.
///
/// 【역할】 스텝3에서 필름 카드가 화면에 나타나고 사라지는 애니메이션을 담당한다.
///         React의 motion.div 패턴을 Unity UI로 구현한 것이다.
///         - Enter: 아래에서 위로 올라오면서 투명→불투명, 작은 스케일→정상 스케일
///         - Exit: 위로 올라가면서 불투명→투명, 정상 스케일→작은 스케일
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝3에서 사용
/// 【부모 클래스】 MonoBehaviour (독립 컴포넌트)
/// 【참조하는 곳】 Director_Problem1_Step3_Logic (PlayEnter/PlayExit 호출)
/// 【참조되는 곳】 없음 (독립적으로 동작)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Director_Problem1_Step3_FilmCardAnimator : MonoBehaviour
{
    [Header("등장 애니메이션")]
    [SerializeField] private float enterDuration = 0.25f;     // 등장 애니메이션 시간 (초)
    [SerializeField] private float enterOffsetY = 50f;        // 아래에서 50px 위치에서 시작 (DOM: y:50)

    [Header("퇴장 애니메이션")]
    [SerializeField] private float exitDuration = 0.25f;      // 퇴장 애니메이션 시간 (초)
    [SerializeField] private float exitOffsetY = 50f;         // 위로 50px 이동 (DOM: y:-50)
    [SerializeField] private float exitEndScale = 0.8f;       // 퇴장 시 최종 스케일

    [Header("스케일 설정")]
    [SerializeField] private float enterStartScale = 0.8f;    // 등장 시작 스케일 (DOM: scale:0.8)

    [Header("투명 페이드 (옵션)")]
    [SerializeField] private CanvasGroup canvasGroup;          // 알파값 0↔1 애니메이션용

    private RectTransform _rt;            // 자신의 RectTransform 캐시
    private Vector2 _baseAnchoredPos;     // 기본(원래) anchored 위치
    private Vector3 _baseScale;           // 기본(원래) 스케일
    private bool _initialized;            // 초기화 완료 여부
    private Coroutine _running;           // 현재 실행 중인 애니메이션 코루틴

    /// <summary>RectTransform, 기본 위치/스케일, CanvasGroup을 최초 1회 캐싱한다.</summary>
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
    /// 등장 애니메이션 재생. 내부 코루틴으로 동작하며, 호출하면 바로 반환된다.
    /// 아래→위 이동, 투명→불투명, 작은→정상 스케일로 전환된다.
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
    /// 퇴장 애니메이션. 호출하는 쪽에서 StartCoroutine으로 대기 가능:
    /// yield return StartCoroutine(animator.PlayExit());
    /// 정상→작은 스케일, 불투명→투명, 위쪽으로 이동.
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

    /// <summary>등장 애니메이션 코루틴. SmoothStep 보간으로 부드러운 전환.</summary>
    private IEnumerator EnterRoutine()
    {
        // React: initial { opacity:0, scale:0.8, y:50 } �� animate { opacity:1, scale:1, y:0 }
        // Unity UI: y �Ʒ������� -�̹Ƿ�, base - enterOffsetY ���� ����
        Vector2 startPos = _baseAnchoredPos + new Vector2(0f, -enterOffsetY);
        Vector2 endPos = _baseAnchoredPos;

        Vector3 startScale = new Vector3(enterStartScale, enterStartScale, 1f);
        Vector3 endScale = _baseScale; // ���� (1,1,1)

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

    /// <summary>퇴장 애니메이션 코루틴. 현재 위치에서 위쪽으로 이동하며 축소+페이드아웃.</summary>
    private IEnumerator ExitRoutine()
    {
        // React: exit { opacity:0, scale:0.8, y:-50 } ����
        // �� ����(1,1,1, y0)���� ������ 0.8, y +exitOffsetY(��)��
        Vector2 startPos = _rt.anchoredPosition;
        Vector2 endPos = _baseAnchoredPos + new Vector2(0f, exitOffsetY); // ���� �̵�

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
