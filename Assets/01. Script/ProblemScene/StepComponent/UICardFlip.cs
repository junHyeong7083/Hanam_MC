using System.Collections;
using UnityEngine;

/// <summary>
/// UI 카드 RectTransform의 width를
/// - 원래 width → 0 → 다시 원래 width
/// 로 애니메이션시키는 플립 연출 전용 컴포넌트.
///
/// 실제 텍스트/이미지 교체는 중간/끝에서
/// 외부(컨트롤러)가 해주는 구조로 사용.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UICardFlip : MonoBehaviour
{
    [Header("플립 대상 Rect (비워두면 자기 RectTransform)")]
    [SerializeField] private RectTransform targetRect;

    [Header("플립 시간 설정")]
    [SerializeField] private float duration = 0.5f;

    [Header("애니메이션 곡선")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("true면 플립 중에도 anchoredPosition을 고정")]
    [SerializeField] private bool keepPosition = true;

    RectTransform Rect
    {
        get
        {
            if (targetRect == null)
                targetRect = GetComponent<RectTransform>();
            return targetRect;
        }
    }

    /// <summary>
    /// width를 줄였다가 다시 늘리는 플립 연출.
    /// (텍스트/배지 교체는 외부에서 이 코루틴 이후에 해줄 것)
    /// </summary>
    public IEnumerator PlayFlipRoutine()
    {
        var rt = Rect;
        if (rt == null)
            yield break;

        float total = Mathf.Max(0.05f, duration);
        float half = total * 0.5f;

        Vector2 originalSize = rt.sizeDelta;
        Vector2 originalPos = rt.anchoredPosition;

        // 1단계: width -> 0
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / half);
            float eased = curve.Evaluate(u);

            float width = Mathf.Lerp(originalSize.x, 0f, eased);
            rt.sizeDelta = new Vector2(width, originalSize.y);
            if (keepPosition)
                rt.anchoredPosition = originalPos;

            yield return null;
        }

        rt.sizeDelta = new Vector2(0f, originalSize.y);
        if (keepPosition)
            rt.anchoredPosition = originalPos;

        // 2단계: 0 -> 원래 width
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / half);
            float eased = curve.Evaluate(u);

            float width = Mathf.Lerp(0f, originalSize.x, eased);
            rt.sizeDelta = new Vector2(width, originalSize.y);
            if (keepPosition)
                rt.anchoredPosition = originalPos;

            yield return null;
        }

        rt.sizeDelta = originalSize;
        if (keepPosition)
            rt.anchoredPosition = originalPos;
    }
}
