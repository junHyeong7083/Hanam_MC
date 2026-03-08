using System;
using UnityEngine;

/// <summary>
/// 인벤토리 루트 관리.
/// - slots 배열로 각 아이템 슬롯 참조
/// - 현재 스텝에서 드래그 가능한 아이템 제어
/// </summary>
public class StepInventory : MonoBehaviour
{
    [Serializable]
    public class InventorySlot
    {
        public string itemId;
        public bool draggableThisStep;
        public GameObject slotRoot;
        public StepInventoryItem itemComponent;
    }

    public InventorySlot[] slots;

    /// <summary>
    /// 모든 슬롯의 draggableThisStep을 false로 리셋
    /// </summary>
    public void ResetAllDraggable()
    {
        if (slots == null) return;
        foreach (var slot in slots)
            slot.draggableThisStep = false;
    }

    /// <summary>
    /// 특정 itemId의 슬롯을 드래그 가능하게 설정
    /// </summary>
    public void SetDraggable(string itemId, bool draggable)
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot.itemId == itemId)
            {
                slot.draggableThisStep = draggable;
                break;
            }
        }
    }
}
