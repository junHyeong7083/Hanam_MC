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
        {
            outline.SetActive(false);
            Debug.Log($"[UIDropBoxArea] OnEnable - outline 숨김 처리됨");
        }
        else
        {
            Debug.LogWarning("[UIDropBoxArea] OnEnable - outline이 null!");
        }
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

        // Screen Space - Overlay 캔버스에서는 camera가 null이어야 함
        // pressEventCamera가 null이면 그대로 null 사용 (Overlay 캔버스)
        Camera cam = eventData.pressEventCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            area,
            eventData.position,
            cam
        );
    }

    /// <summary>드래그 중에 영역 안/밖에 따라 외곽선 On/Off</summary>
    public void UpdateHighlight(PointerEventData eventData)
    {
        if (outline == null)
        {
            Debug.LogWarning("[UIDropBoxArea] outline이 할당되지 않았습니다");
            return;
        }

        if (area == null)
        {
            Debug.LogWarning("[UIDropBoxArea] area가 할당되지 않았습니다");
            return;
        }

        bool over = IsPointerOver(eventData);
        Debug.Log($"[UIDropBoxArea] UpdateHighlight - over={over}, pos={eventData.position}");
        outline.SetActive(over);
    }

    /// <summary>�ܰ��� ���� On/Off</summary>
    public void SetOutlineVisible(bool visible)
    {
        if (outline == null) return;
        Debug.Log($"[UIDropBoxArea] SetOutlineVisible({visible}) 호출됨");
        outline.SetActive(visible);
    }
}
