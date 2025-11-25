using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 두 Anchor(시작/끝 Transform)를 잇는 UI Image 선을
/// - 0 → 길이까지 자라게
/// - 잠깐 유지
/// - 알파 페이드 아웃
/// 시켜주는 공통 컴포넌트.
///
/// RectTransform + Image 가 붙은 오브젝트에 붙여서 사용.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class UILineConnector : MonoBehaviour
{
    [Header("Canvas (비워두면 부모에서 자동 검색)")]
    [SerializeField] private Canvas canvas;

    [Header("선 두께 / 타이밍")]
    [SerializeField] private float maxThickness = 12f;
    [SerializeField] private float growDuration = 0.25f;
    [SerializeField] private float holdDuration = 0.15f;
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("애니메이션 곡선")]
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve thicknessCurve = AnimationCurve.Linear(0, 0.7f, 1, 1);

    [Header("끝나면 오브젝트 비활성화")]
    [SerializeField] private bool disableAtEnd = true;

    RectTransform _rect;
    Image _image;
    RectTransform _canvasRect;

    Color _baseColor;
    bool _baseColorInitialized;

    // ------------------------------------------------------
    // 공통 초기화 (Awake/OnEnable에 의존 X)
    // ------------------------------------------------------
    void EnsureInitialized()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        if (_image == null)
            _image = GetComponent<Image>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null && _canvasRect == null)
            _canvasRect = canvas.transform as RectTransform;

        // 처음 한 번만, "씬에 배치된 원래 색"을 저장
        if (_image != null && !_baseColorInitialized)
        {
            _baseColor = _image.color;  // 인스펙터에서 세팅한 색 (알파 1)
            _baseColorInitialized = true;
        }
    }

    /// <summary>
    /// 에디터/초기화용. 길이 0, 알파 0 상태로 리셋.
    /// </summary>
    public void ResetLine()
    {
        EnsureInitialized();

        if (_rect != null)
        {
            var sd = _rect.sizeDelta;
            sd.x = 0f;
            sd.y = maxThickness;
            _rect.sizeDelta = sd;
        }

        if (_image != null && _baseColorInitialized)
        {
            // 원래 색 기준으로 알파만 0으로
            var c = _baseColor;
            c.a = 0f;
            _image.color = c;
        }

        // 이건 선택사항이긴 한데, 굳이 초기화에서 비활성 안 해도 됨
        if (disableAtEnd && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 외부에서 StartCoroutine으로 돌릴 코루틴.
    /// - startAnchor → endAnchor 까지 선을 그려줌.
    /// </summary>
    public IEnumerator PlayLineRoutine(Transform startAnchor, Transform endAnchor)
    {
        EnsureInitialized();

        if (_rect == null || _image == null || _canvasRect == null ||
            startAnchor == null || endAnchor == null)
            yield break;

        // 필요하면 여기서 활성화
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (!_image.enabled)
            _image.enabled = true;

        var cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas != null ? canvas.worldCamera : null;

        // 월드 좌표 → Canvas 로컬 좌표
        Vector2 startLocal;
        Vector2 endLocal;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, startAnchor.position),
            cam,
            out startLocal);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, endAnchor.position),
            cam,
            out endLocal);

        Vector2 dir = endLocal - startLocal;
        float length = dir.magnitude;

        if (length <= 0.0001f)
            yield break;

        // 기본 상태 세팅
        var baseColor = _baseColorInitialized ? _baseColor : _image.color;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        _rect.pivot = new Vector2(0f, 0.5f);
        _rect.anchoredPosition = startLocal;
        _rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        var sd = _rect.sizeDelta;
        sd.y = maxThickness;
        sd.x = 0f;
        _rect.sizeDelta = sd;

        // 1) 0 → length 로 자라기
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / growDuration);
            float eased = growCurve.Evaluate(u);
            float thickMul = thicknessCurve.Evaluate(u);

            sd.x = Mathf.Lerp(0f, length, eased);
            sd.y = maxThickness * thickMul;
            _rect.sizeDelta = sd;

            var c = baseColor;
            c.a = Mathf.Lerp(0f, baseColor.a, eased);  // 0 → 1
            _image.color = c;

            yield return null;
        }

        sd.x = length;
        sd.y = maxThickness;
        _rect.sizeDelta = sd;
        _image.color = baseColor;   // 알파 1

        // 2) 잠깐 유지
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        // 3) 길이는 유지하고, 알파만 1 → 0으로 페이드 아웃
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / fadeDuration);

            var c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, u);
            _image.color = c;

            yield return null;
        }

        if (disableAtEnd)
            gameObject.SetActive(false);
    }
}
