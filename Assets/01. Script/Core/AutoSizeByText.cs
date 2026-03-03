using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AutoSizeByText : MonoBehaviour
{
    [SerializeField] private Text targetText;

    [Header("Fit 방향")]
    [SerializeField] private bool fitWidth = true;
    [SerializeField] private bool fitHeight = false;

    [Header("Padding")]
    [SerializeField] private float paddingLeft = 10f;
    [SerializeField] private float paddingRight = 10f;
    [SerializeField] private float paddingTop = 5f;
    [SerializeField] private float paddingBottom = 5f;

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

        // Text의 부모(자기자신) 내 오프셋을 자동 계산
        RectTransform textRt = targetText.rectTransform;
        float textLeftOffset = 0f;
        float textRightOffset = 0f;
        float textTopOffset = 0f;
        float textBottomOffset = 0f;

        // Text 앵커가 stretch일 때 offsetMin/offsetMax에서 마진을 읽어옴
        if (textRt.parent == _rt)
        {
            textLeftOffset = textRt.offsetMin.x;                // 왼쪽 마진
            textRightOffset = -textRt.offsetMax.x;              // 오른쪽 마진
            textTopOffset = -textRt.offsetMax.y;                // 위쪽 마진
            textBottomOffset = textRt.offsetMin.y;              // 아래쪽 마진
        }

        Vector2 size = _rt.sizeDelta;

        if (fitWidth)
        {
            float w = targetText.preferredWidth
                    + Mathf.Max(textLeftOffset, 0f) + Mathf.Max(textRightOffset, 0f)
                    + paddingLeft + paddingRight;
            if (minWidth > 0f) w = Mathf.Max(w, minWidth);
            if (maxWidth > 0f) w = Mathf.Min(w, maxWidth);
          //  Debug.Log($"[AutoSizeByText] \"{targetText.text}\" | preferredWidth={targetText.preferredWidth:F1}, textOffset=({textLeftOffset:F0}+{textRightOffset:F0}), padding=({paddingLeft}+{paddingRight}), result w={w:F1}");
            size.x = w;
        }

        if (fitHeight)
        {
            if (fitWidth)
                _rt.sizeDelta = new Vector2(size.x, _rt.sizeDelta.y);

            // preferredHeight를 정확히 읽으려면 항상 레이아웃 강제 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(textRt);

            float h = targetText.preferredHeight
                    + Mathf.Max(textTopOffset, 0f) + Mathf.Max(textBottomOffset, 0f)
                    + paddingTop + paddingBottom;
            if (minHeight > 0f) h = Mathf.Max(h, minHeight);
            if (maxHeight > 0f) h = Mathf.Min(h, maxHeight);
            size.y = h;
        }

        //Debug.Log($"[AutoSizeByText] \"{gameObject.name}\" | preferredH={targetText.preferredHeight:F1} fitH={fitHeight} final=({size.x:F1}, {size.y:F1})");
        _rt.sizeDelta = size;
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
