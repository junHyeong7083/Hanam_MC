using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자식 Text의 내용 길이에 따라 이 오브젝트의 RectTransform 크기를 동적으로 조절합니다.
/// Image 오브젝트에 붙이면 자식 Text에 맞춰 배경이 자동 리사이즈됩니다.
/// horizontalAnchor를 Left로 설정하면 왼쪽은 고정, 오른쪽만 줄어듭니다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AutoSizeByText : MonoBehaviour
{
    public enum HorizontalAnchor { Left, Center, Right }
    public enum VerticalAnchor { Top, Center, Bottom }

    [SerializeField] private Text targetText;

    [Header("Fit 방향")]
    [SerializeField] private bool fitWidth = true;
    [SerializeField] private bool fitHeight = false;

    [Header("기준점 (어느 쪽을 고정할지)")]
    [SerializeField] private HorizontalAnchor horizontalAnchor = HorizontalAnchor.Left;
    [SerializeField] private VerticalAnchor verticalAnchor = VerticalAnchor.Center;

    [Header("Padding (left / right / top / bottom)")]
    [SerializeField] private RectOffset padding = new RectOffset(10, 10, 5, 5);

    [Header("크기 제한 (0 = 제한 없음)")]
    [SerializeField] private float minWidth = 0f;
    [SerializeField] private float maxWidth = 0f;
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 0f;

    private RectTransform _rt;
    private string _cachedText;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (targetText == null)
            targetText = GetComponentInChildren<Text>(true);
    }

    private void LateUpdate()
    {
        if (targetText == null) return;
        if (targetText.text == _cachedText) return;
        _cachedText = targetText.text;
        Refresh();
    }

    public void Refresh()
    {
        if (targetText == null) return;
        if (_rt == null) _rt = GetComponent<RectTransform>();

        // offsetMin.x = 왼쪽 가장자리, offsetMax.x = 오른쪽 가장자리
        // 직접 가장자리를 조작하므로 pivot/anchor 값에 상관없이 정확하게 동작
        Vector2 oMin = _rt.offsetMin;
        Vector2 oMax = _rt.offsetMax;

        if (fitWidth)
        {
            float w = targetText.preferredWidth + padding.left + padding.right;
            if (minWidth > 0f) w = Mathf.Max(w, minWidth);
            if (maxWidth > 0f) w = Mathf.Min(w, maxWidth);

            float curW = oMax.x - oMin.x;

            switch (horizontalAnchor)
            {
                case HorizontalAnchor.Left:
                    oMax.x = oMin.x + w;
                    break;
                case HorizontalAnchor.Right:
                    oMin.x = oMax.x - w;
                    break;
                case HorizontalAnchor.Center:
                    float cx = (oMin.x + oMax.x) * 0.5f;
                    oMin.x = cx - w * 0.5f;
                    oMax.x = cx + w * 0.5f;
                    break;
            }

            _rt.offsetMin = new Vector2(oMin.x, _rt.offsetMin.y);
            _rt.offsetMax = new Vector2(oMax.x, _rt.offsetMax.y);
        }

        if (fitHeight)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(targetText.rectTransform);

            float h = targetText.preferredHeight + padding.top + padding.bottom;
            if (minHeight > 0f) h = Mathf.Max(h, minHeight);
            if (maxHeight > 0f) h = Mathf.Min(h, maxHeight);

            oMin = _rt.offsetMin;
            oMax = _rt.offsetMax;

            switch (verticalAnchor)
            {
                case VerticalAnchor.Top:
                    oMin.y = oMax.y - h;
                    break;
                case VerticalAnchor.Bottom:
                    oMax.y = oMin.y + h;
                    break;
                case VerticalAnchor.Center:
                    float cy = (oMin.y + oMax.y) * 0.5f;
                    oMin.y = cy - h * 0.5f;
                    oMax.y = cy + h * 0.5f;
                    break;
            }

            _rt.offsetMin = oMin;
            _rt.offsetMax = oMax;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (targetText == null) targetText = GetComponentInChildren<Text>(true);
        _cachedText = null;
    }
#endif
}
