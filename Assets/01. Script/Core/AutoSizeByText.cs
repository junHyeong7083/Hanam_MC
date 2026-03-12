using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AutoSizeByText - Text 컴포넌트의 내용에 따라 부모 RectTransform 크기를 자동 조절하는 유틸리티
///
/// 【역할】 자식 Text 컴포넌트의 preferredWidth/preferredHeight를 기반으로,
///          이 컴포넌트가 부착된 RectTransform의 sizeDelta를 자동으로 맞춘다.
///          대화 말풍선(HanamBox)이나 동적 텍스트 UI에서 사용된다.
/// 【참조하는 곳】 HanamBox 프리팹의 말풍선 배경, 각종 동적 텍스트 UI
/// 【참조되는 곳】 UnityEngine.UI.Text (텍스트 크기 측정)
/// 【흐름】 LateUpdate()에서 텍스트 변경 감지 → Refresh()로 sizeDelta 재계산
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AutoSizeByText : MonoBehaviour
{
    /// <summary>크기 기준이 되는 Text 컴포넌트. 미지정 시 자식에서 자동 탐색</summary>
    [SerializeField] private Text targetText;

    /// <summary>가로 크기를 텍스트에 맞출지 여부</summary>
    [Header("Fit 방향")]
    [SerializeField] private bool fitWidth = true;
    /// <summary>세로 크기를 텍스트에 맞출지 여부</summary>
    [SerializeField] private bool fitHeight = false;

    /// <summary>왼쪽 여백 (px)</summary>
    [Header("Padding")]
    [SerializeField] private float paddingLeft = 10f;
    /// <summary>오른쪽 여백 (px)</summary>
    [SerializeField] private float paddingRight = 10f;
    /// <summary>위쪽 여백 (px)</summary>
    [SerializeField] private float paddingTop = 5f;
    /// <summary>아래쪽 여백 (px)</summary>
    [SerializeField] private float paddingBottom = 5f;

    /// <summary>최소 가로 크기. 0이면 제한 없음</summary>
    [Header("크기 제한 (0 = 제한 없음)")]
    [SerializeField] private float minWidth = 0f;
    /// <summary>최대 가로 크기. 0이면 제한 없음</summary>
    [SerializeField] private float maxWidth = 0f;
    /// <summary>최소 세로 크기. 0이면 제한 없음</summary>
    [SerializeField] private float minHeight = 0f;
    /// <summary>최대 세로 크기. 0이면 제한 없음</summary>
    [SerializeField] private float maxHeight = 0f;

    /// <summary>자신의 RectTransform 캐시</summary>
    private RectTransform _rt;
    /// <summary>이전 프레임의 텍스트 내용. 변경 감지에 사용</summary>
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

    /// <summary>
    /// Text의 preferredWidth/Height를 읽어 부모 RectTransform 크기를 재계산한다.
    /// 텍스트의 offsetMin/offsetMax에서 마진을 자동으로 읽어와 패딩에 반영한다.
    /// </summary>
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
