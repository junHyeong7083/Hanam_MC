using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// StepInventory 프리팹의 참조를 자동으로 설정하는 에디터 유틸리티.
/// 메뉴: Tools > Setup StepInventory References
/// </summary>
public static class StepInventorySetup
{
    [MenuItem("Tools/Setup StepInventory References")]
    public static void Setup()
    {
        string prefabPath = "Assets/02. Prefab/Director/StepInventory.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[StepInventorySetup] 프리팹을 찾을 수 없습니다: " + prefabPath);
            return;
        }

        // 프리팹 편집 모드 진입
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(assetPath);

        try
        {
            // StepInventory 루트 컴포넌트
            var inventory = root.GetComponent<StepInventory>();
            if (inventory == null)
            {
                Debug.LogError("[StepInventorySetup] StepInventory 컴포넌트가 없습니다.");
                return;
            }

            // 각 자식 아이템 설정
            var items = root.GetComponentsInChildren<StepInventoryItem>(true);
            var slots = new StepInventory.InventorySlot[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var itemGo = item.gameObject;

                // itemId를 게임오브젝트 이름으로 설정
                item.itemId = itemGo.name;

                // LockedRoot / UnLockedRoot 찾기
                Transform lockedRoot = itemGo.transform.Find("_LockedRoot");
                Transform unlockedRoot = itemGo.transform.Find("UnLockedRoot");

                item.lockedRoot = lockedRoot != null ? lockedRoot.gameObject : null;
                item.unlockedRoot = unlockedRoot != null ? unlockedRoot.gameObject : null;

                // UnLockedRoot 하위 이미지 찾기
                if (unlockedRoot != null)
                {
                    Transform bgAlpha = unlockedRoot.Find("BackgroundImageAlpha");
                    Transform bgClick = unlockedRoot.Find("BackgroundImageClcik");

                    item.iconImage = bgClick != null ? bgClick.GetComponent<Image>() : null;
                    item.backgroundImage = bgAlpha != null ? bgAlpha.GetComponent<Image>() : null;
                    item.glowEffect = bgClick != null ? bgClick.GetComponentInChildren<GlowEffect>(true) : null;
                }

                // iconRect = 아이템 자체의 RectTransform
                item.iconRect = itemGo.GetComponent<RectTransform>();

                // rootCanvas는 런타임에 설정 (프리팹에서는 null)
                item.rootCanvas = null;

                // hoverScale 기본값 유지 (1.05)

                // 슬롯 설정
                slots[i] = new StepInventory.InventorySlot
                {
                    itemId = item.itemId,
                    draggableThisStep = false,
                    slotRoot = itemGo,
                    itemComponent = item
                };

                Debug.Log($"[StepInventorySetup] {item.itemId}: locked={item.lockedRoot != null}, unlocked={item.unlockedRoot != null}, icon={item.iconImage != null}, bg={item.backgroundImage != null}, glow={item.glowEffect != null}");
            }

            inventory.slots = slots;

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Debug.Log($"[StepInventorySetup] 완료! {items.Length}개 아이템 설정됨.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
