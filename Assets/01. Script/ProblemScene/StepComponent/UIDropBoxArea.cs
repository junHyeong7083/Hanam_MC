using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UIDropBoxArea - 드래그 앤 드롭의 "여기에 놓으세요" 드롭 영역 컴포넌트
///
/// 【역할】 사용자가 아이템을 드래그할 때 드롭 가능한 영역을 판정하고,
///          드래그 중 영역 안에 들어오면 외곽선(outline)을 하이라이트로 표시한다.
///          RectTransformUtility.RectangleContainsScreenPoint()로 영역 판정.
/// 【참조하는 곳】 StepInventory 시스템에서 아이템 드래그 시 드롭 대상으로 사용,
///                각 Problem Director Logic에서 드래그 이벤트 처리 시 참조
/// 【참조되는 곳】 없음 (독립 컴포넌트)
/// 【흐름】 드래그 시작 → UpdateHighlight(eventData) 반복 호출 → 드롭 시 IsPointerOver() 판정
/// </summary>
public class UIDropBoxArea : MonoBehaviour
{
    [Header("DropDown box")]
    [SerializeField] private RectTransform area;   // 드롭 영역으로 사용할 RectTransform (포인터 판정 범위)

    [Header("drag Outline box")]
    [SerializeField] private GameObject outline;   // 드래그 중 영역 안에 있을 때 표시되는 외곽선 UI 오브젝트

    private void Awake()
    {
        Debug.Log($"[UIDropBoxArea] Awake 호출됨 - outline={outline != null}");

        // 가장 먼저 outline 숨김
        if (outline != null)
        {
            bool wasActive = outline.activeSelf;
            outline.SetActive(false);
            Debug.Log($"[UIDropBoxArea] Awake - outline 숨김 처리됨 (이전 상태: {wasActive})");
        }
        else
        {
            Debug.LogError("[UIDropBoxArea] Awake - outline이 null! Inspector에서 할당하세요.");
        }
    }

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

    /// <summary>초기 상태로 리셋 (외곽선 숨김)</summary>
    public void ResetVisual()
    {
        if (outline != null)
            outline.SetActive(false);
    }

    /// <summary>
    /// 현재 포인터가 드롭 영역 안에 있는지 판정.
    /// (true면 드롭 가능 영역에 들어온 상태)
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

    /// <summary>외곽선 강제 On/Off</summary>
    public void SetOutlineVisible(bool visible)
    {
        if (outline == null) return;
        Debug.Log($"[UIDropBoxArea] SetOutlineVisible({visible}) 호출됨");
        outline.SetActive(visible);
    }
}
