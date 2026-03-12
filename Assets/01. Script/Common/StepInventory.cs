using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// StepInventory - 문제 풀이 중 획득하는 아이템 인벤토리 관리 컴포넌트
///
/// 【역할】 StepInventory 프리팹(Assets/02. Prefab/Director/StepInventory.prefab)에 부착되어
///          각 스텝에서 사용 가능한 아이템 슬롯을 관리한다.
///          슬롯별로 "이번 스텝에서 드래그 가능 여부(draggableThisStep)"를 제어하며,
///          드래그 가능한 아이템에 GlowEffect를 적용/해제한다.
///
/// 【참조하는 곳】 Director_Problem2_Step1_Logic (P2~P6 Step1의 공통 로직) — 인벤토리 드래그 앤 드롭 문제 풀이,
///                UIDropBoxArea — 드롭 영역에서 StepInventory 참조,
///                LocalRewardService — 인벤토리 아이템 목록 조회
/// 【참조되는 곳】 StepInventoryItem — 개별 슬롯의 아이템 컴포넌트,
///                GlowEffect — 드래그 가능 아이템의 시각적 강조 효과
///
/// 【흐름】
///   1. 각 스텝 Logic에서 SetDraggable(itemId, true)로 드래그 가능 아이템 설정
///   2. OnEnable() → 1프레임 대기 후 ApplyDraggableGlow()로 GlowEffect 적용
///   3. 유저가 아이템 드래그 → StepInventoryItem이 드래그 이벤트 처리
///   4. 스텝 전환 시 ResetAllDraggable()로 초기화
/// </summary>
public class StepInventory : MonoBehaviour
{
    /// <summary>
    /// 인벤토리 슬롯 하나의 정보를 담는 직렬화 클래스.
    /// 인스펙터에서 각 슬롯의 아이템 ID, 드래그 가능 여부, UI 오브젝트 참조를 설정한다.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        /// <summary>아이템 고유 식별자 (예: "item_mirror", "item_camera")</summary>
        public string itemId;

        /// <summary>현재 스텝에서 드래그 가능한지 여부 (스텝마다 Logic에서 설정)</summary>
        public bool draggableThisStep;

        /// <summary>슬롯의 루트 GameObject (UI 표시/숨김 제어용)</summary>
        public GameObject slotRoot;

        /// <summary>슬롯에 부착된 StepInventoryItem 컴포넌트 (드래그 로직 담당)</summary>
        public StepInventoryItem itemComponent;
    }

    /// <summary>모든 인벤토리 슬롯 배열 (인스펙터에서 설정). MCP로 설정 시 전체 배열이 교체되므로 주의.</summary>
    public InventorySlot[] slots;

    /// <summary>
    /// 활성화 시 1프레임 대기 후 GlowEffect를 적용한다.
    /// 1프레임 지연 이유: 자식 컴포넌트(GlowEffect 등)의 OnEnable이 완료된 후에 Play/Stop을 호출해야 정상 동작.
    /// </summary>
    private void OnEnable()
    {
        // 1프레임 대기: 모든 자식 컴포넌트(GlowEffect 포함) OnEnable 완료 후 적용
        StartCoroutine(ApplyDraggableGlowDelayed());
    }

    /// <summary>1프레임 대기 후 ApplyDraggableGlow()를 호출하는 코루틴</summary>
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
