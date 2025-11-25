using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 드래그 가능한 UI 요소들이 "여기에 떨어지면 성공"하는 드롭 박스 영역.
/// - RectTransform 기준으로 포인터가 안/밖에 있는지 판정
/// - 외곽선(하이라이트) On/Off 관리
/// </summary>
public class UIDropBoxArea : MonoBehaviour
{
    [Header("드롭 영역")]
    [SerializeField] private RectTransform area;

    [Header("하이라이트(외곽선)")]
    [SerializeField] private GameObject outline;

    /// <summary>초기 상태로 리셋 (라인 끄기)</summary>
    public void ResetVisual()
    {
        if (outline != null)
            outline.SetActive(false);
    }

    /// <summary>
    /// 현재 포인터가 드롭 영역 안에 있는지 판정.
    /// (true면 드롭 성공 판정에 쓸 수 있음)
    /// </summary>
    public bool IsPointerOver(PointerEventData eventData)
    {
        if (area == null || eventData == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            area,
            eventData.position,
            eventData.pressEventCamera
        );
    }

    /// <summary>드래그 중에 영역 위/밖에 따라 외곽선 On/Off</summary>
    public void UpdateHighlight(PointerEventData eventData)
    {
        if (outline == null) return;

        bool over = IsPointerOver(eventData);
        outline.SetActive(over);
    }

    /// <summary>외곽선 강제 On/Off</summary>
    public void SetOutlineVisible(bool visible)
    {
        if (outline == null) return;
        outline.SetActive(visible);
    }
}
