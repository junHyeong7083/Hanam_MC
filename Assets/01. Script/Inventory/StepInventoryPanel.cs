using System;
using UnityEngine;

/// <summary>
/// Step 내부에서 사용하는 인벤토리 패널.
/// - DB에서 현재 유저의 인벤토리를 읽어서,
///   보유한 아이템만 언락 상태로 표시.
/// - 각 슬롯마다 "이 스텝에서 드래그 가능 여부"를 설정.
/// - RewardInventoryPanel 과 같은 DB를 공유 (ItemId 기준).
/// </summary>
public class StepInventoryPanel : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        [Header("공통 설정")]
        //[Tooltip("DB InventoryItem.ItemId 와 동일 (비워두면 itemComponent.itemId 사용)")]
        public string itemId;

        [Tooltip("이 스텝에서 이 아이템을 드래그 가능하게 할지 여부")]
        public bool draggableThisStep = false;

        [Header("UI 참조")]
        [Tooltip("슬롯 전체 루트 (항상 보이게 두고, 안에서 락/언락만 바꿈)")]
        public GameObject slotRoot;

        [Tooltip("슬롯 안에서 실제 아이콘 드래그/호버를 담당하는 컴포넌트")]
        public StepInventoryItem itemComponent;

        // 내부 상태
        [NonSerialized] public bool isUnlocked;
    }

    [Header("슬롯들 (인스펙터에서 할당)")]
    [SerializeField] private Slot[] slots;

    private void OnEnable()
    {
        RefreshFromDb();
        ApplyStepSettings();
    }

    /// <summary>
    /// DB에서 현재 유저 인벤토리를 읽어서,
    /// 각 슬롯의 보유 여부(isUnlocked)를 업데이트.
    /// </summary>
    private void RefreshFromDb()
    {
        if (slots == null || slots.Length == 0)
            return;

        var data = DataService.Instance;
        if (data == null)
        {
            Debug.LogWarning("[StepInventoryPanel] DataService.Instance 가 없음");
            MarkAllLocked();
            return;
        }

        // 🔁 여기: User → Reward 로 교체
        var rewardService = data.Reward;
        if (rewardService == null)
        {
            Debug.LogWarning("[StepInventoryPanel] RewardService 가 없음");
            MarkAllLocked();
            return;
        }

        var sess = SessionManager.Instance;
        var currentUser = sess?.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogWarning("[StepInventoryPanel] 로그인 유저가 없어 인벤토리 표시 스킵");
            MarkAllLocked();
            return;
        }

        var invResult = rewardService.GetInventory(currentUser.Email);
        if (!invResult.Ok || invResult.Value == null)
        {
            Debug.LogWarning("[StepInventoryPanel] 인벤토리 조회 실패 혹은 null");
            MarkAllLocked();
            return;
        }

        var inventory = invResult.Value;

        foreach (var s in slots)
        {
            if (s == null)
                continue;

            string slotItemId = s.itemComponent != null ? s.itemComponent.itemId : null;
            if (string.IsNullOrEmpty(slotItemId))
            {
                s.isUnlocked = false;
                if (s.itemComponent != null)
                    s.itemComponent.SetUnlockedVisual(false);
                continue;
            }

            bool hasItem = false;
            for (int i = 0; i < inventory.Length; i++)
            {
                var it = inventory[i];
                if (it != null && it.ItemId == slotItemId)
                {
                    Debug.Log("Has Item!! : " + it.ItemId);
                    hasItem = true;
                    break;
                }
            }

            s.isUnlocked = hasItem;

            if (s.slotRoot != null)
                s.slotRoot.SetActive(true); // 슬롯은 항상 보이게

            if (s.itemComponent != null)
                s.itemComponent.SetUnlockedVisual(hasItem);
        }
    }

    /// <summary>
    /// 인벤토리 조회 실패/유저 없음 등의 경우 전부 잠긴 상태로 표시
    /// </summary>
    private void MarkAllLocked()
    {
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s == null) continue;

            s.isUnlocked = false;

            if (s.slotRoot != null)
                s.slotRoot.SetActive(true); // 슬롯 자체는 보이게

            if (s.itemComponent != null)
                s.itemComponent.SetUnlockedVisual(false);
        }
    }

    /// <summary>
    /// 각 슬롯에 대해 "이 스텝에서 드래그 가능" 여부를 StepInventoryItem에 넘겨줌.
    /// </summary>
    private void ApplyStepSettings()
    {
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s == null || s.itemComponent == null)
                continue;

            // 인벤토리를 보유하고 있고 && 이 스텝에서 드래그 허용이면
            bool canDragNow = s.isUnlocked && s.draggableThisStep;

            s.itemComponent.SetDraggable(canDragNow);
            s.itemComponent.SetWiggleActive(canDragNow);
        }
    }
}
