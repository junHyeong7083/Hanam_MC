using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// �巡�� ������ UI ��ҵ��� "���⿡ �������� ����"�ϴ� ��� �ڽ� ����.
/// - RectTransform �������� �����Ͱ� ��/�ۿ� �ִ��� ����
/// - �ܰ���(���̶���Ʈ) On/Off ����
/// </summary>
public class UIDropBoxArea : MonoBehaviour
{
    [Header("��� ����")]
    [SerializeField] private RectTransform area;

    [Header("���̶���Ʈ(�ܰ���)")]
    [SerializeField] private GameObject outline;

    private void OnEnable()
    {
        // Step 활성화 시 outline 숨김
        if (outline != null)
            outline.SetActive(false);
    }

    /// <summary>�ʱ� ���·� ���� (���� ����)</summary>
    public void ResetVisual()
    {
        if (outline != null)
            outline.SetActive(false);
    }

    /// <summary>
    /// ���� �����Ͱ� ��� ���� �ȿ� �ִ��� ����.
    /// (true�� ��� ���� ������ �� �� ����)
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

    /// <summary>�巡�� �߿� ���� ��/�ۿ� ���� �ܰ��� On/Off</summary>
    public void UpdateHighlight(PointerEventData eventData)
    {
        if (outline == null) return;

        bool over = IsPointerOver(eventData);
        outline.SetActive(over);
    }

    /// <summary>�ܰ��� ���� On/Off</summary>
    public void SetOutlineVisible(bool visible)
    {
        if (outline == null) return;
        outline.SetActive(visible);
    }
}
