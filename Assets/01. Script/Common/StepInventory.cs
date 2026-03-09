using System;
using System.Collections;
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

    private void OnEnable()
    {
        // 1프레임 대기: 모든 자식 컴포넌트(GlowEffect 포함) OnEnable 완료 후 적용
        StartCoroutine(ApplyDraggableGlowDelayed());
    }

    private IEnumerator ApplyDraggableGlowDelayed()
    {
        yield return null;
        ApplyDraggableGlow();
    }

    /// <summary>
    /// slots의 draggableThisStep 값에 따라 glow Play/Stop 적용
    /// </summary>
    public void ApplyDraggableGlow()
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot.itemComponent == null || slot.itemComponent.glowEffect == null)
                continue;

            if (slot.draggableThisStep)
            {
                slot.itemComponent.glowEffect.playOnEnable = true;
                slot.itemComponent.glowEffect.Play();
            }
            else
            {
                slot.itemComponent.glowEffect.playOnEnable = false;
                slot.itemComponent.glowEffect.Stop();
            }
        }
    }

    /// <summary>
    /// 모든 슬롯의 draggableThisStep을 false로 리셋
    /// </summary>
    public void ResetAllDraggable()
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            slot.draggableThisStep = false;
            if (slot.itemComponent != null && slot.itemComponent.glowEffect != null)
                slot.itemComponent.glowEffect.Stop();
        }
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
                if (slot.itemComponent != null && slot.itemComponent.glowEffect != null)
                {
                    if (draggable)
                    {
                        slot.itemComponent.glowEffect.playOnEnable = true;
                        slot.itemComponent.glowEffect.Play();
                    }
                    else
                    {
                        slot.itemComponent.glowEffect.playOnEnable = false;
                        slot.itemComponent.glowEffect.Stop();
                    }
                }
                break;
            }
        }
    }
}
