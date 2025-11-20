using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 인벤토리 슬롯 연출 컨트롤러
/// - slot1 ~ slot10 구조를 그대로 사용
/// - LockedRoot / UnLockedRoot 를 유지하면서
///   슬롯 루트(slot1 등)에 scale/alpha 애니메이션만 얹는다.
/// </summary>
public class RewardInventoryPanel : MonoBehaviour
{
    [Serializable]
    public class SlotUI
    {
        [Header("식별용 아이템 ID (예: mind_lens)")]
        public string itemId;

        [Header("슬롯 루트 (slot1, slot2 ...)")]
        public RectTransform slotRoot;     // slot1 오브젝트
        public CanvasGroup canvasGroup;    // slot1 에 붙인 CanvasGroup

        [Header("Lock / Unlock 상태 루트")]
        public GameObject lockedRoot;      // slot1/LockedRoot
        public GameObject unlockedRoot;    // slot1/UnLockedRoot

        [Header("새로 획득 뱃지 (선택)")]
        public GameObject badgeRoot;       // 체크 표시 등

        [Header("초기 언락 상태 (시작 시 이미 가지고 있는 슬롯이면 체크)")]
        public bool defaultUnlocked;

        [NonSerialized] public bool isUnlocked;

        public void InitRuntimeState()
        {
            isUnlocked = defaultUnlocked;
            ApplyLockState(isUnlocked);
        }

        public void ApplyLockState(bool unlocked)
        {
            if (lockedRoot != null) lockedRoot.SetActive(!unlocked);
            if (unlockedRoot != null) unlockedRoot.SetActive(unlocked);
        }
    }

    [Header("슬롯 리스트 (slot1 ~ slot10)")]
    [SerializeField] private SlotUI[] slots;

    [Header("슬롯 순차 애니메이션 설정")]
    [SerializeField] private float firstDelay = 0.1f;      // 인벤토리 전체가 뜬 뒤 대기 시간
    [SerializeField] private float slotInterval = 0.05f;   // ★다음 슬롯 시작까지의 간격 (겹치게 시작)
    [SerializeField] private float slotAnimDuration = 0.3f; // ★각 슬롯 애니메이션 길이
    [SerializeField] private float slotStartScale = 0.0f;  // 0이면 0에서 시작해서 1로

    [Header("뱃지 팝 애니메이션")]
    [SerializeField] private float badgePopDuration = 0.3f;
    [SerializeField] private float badgePopOvershoot = 1.2f;

    private Coroutine _sequenceRoutine;

    private void Awake()
    {
        // 런타임 상태 초기화
        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s == null) continue;

            // 언락/락 상태만 먼저 반영
            s.InitRuntimeState();

            // ★ 처음에는 전부 안 보이게 (플리커 방지)
            if (s.slotRoot != null)
                s.slotRoot.localScale = Vector3.one * slotStartScale; // 보통 0
            if (s.canvasGroup != null)
                s.canvasGroup.alpha = 0f;

            if (s.badgeRoot != null)
            {
                s.badgeRoot.SetActive(false);
                s.badgeRoot.transform.localScale = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 인벤토리를 보여줄 때 사용하는 진입 함수.
    /// unlockedItemId: 이번에 새로 획득한 아이템 ID (없으면 null 또는 빈 문자열).
    /// playAnimation: true면 슬롯들이 순차적으로 등장하는 애니메이션, false면 즉시 표시.
    /// </summary>
    public void ShowInventory(string unlockedItemId, bool playAnimation)
    {
        // 새로 언락된 아이템이 있으면 슬롯 상태 갱신
        if (!string.IsNullOrEmpty(unlockedItemId))
        {
            var newSlot = FindSlot(unlockedItemId);
            if (newSlot != null)
            {
                newSlot.isUnlocked = true;
            }
        }

        // 현재 isUnlocked 상태를 실제 Lock/Unlock 오브젝트에 반영
        ApplyLockStatesFromRuntime();

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        if (playAnimation)
        {
            // 애니메이션용으로 slot scale/alpha 초기화 후 코루틴 시작
            InitSlotsForAnimation();
            _sequenceRoutine = StartCoroutine(SlotsSequence(unlockedItemId));
        }
        else
        {
            // 애니메이션 없이 바로 보여주기
            ShowSlotsInstant(unlockedItemId);
        }
    }

    /// <summary>
    /// 현재 isUnlocked 값을 기준으로 Lock/Unlock 오브젝트 토글
    /// </summary>
    private void ApplyLockStatesFromRuntime()
    {
        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s == null) continue;
            s.ApplyLockState(s.isUnlocked);
        }
    }

    /// <summary>
    /// 애니메이션용 초기화: 모든 슬롯 scale/alpha를 0으로
    /// </summary>
    private void InitSlotsForAnimation()
    {
        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s == null || s.slotRoot == null) continue;

            s.slotRoot.localScale = Vector3.one * slotStartScale;
            if (s.canvasGroup != null) s.canvasGroup.alpha = 0f;

            if (s.badgeRoot != null)
            {
                s.badgeRoot.SetActive(false);
                s.badgeRoot.transform.localScale = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// bool이 true일 때 사용하는 슬롯 순차 애니메이션
    /// 슬롯1 시작 -> 0.05초 후 슬롯2 시작 -> 0.05초 후 슬롯3 시작 ...
    /// 각 슬롯 애니메이션 자체는 slotAnimDuration(0.3초) 동안 진행 (겹쳐서 재생)
    /// </summary>
    private IEnumerator SlotsSequence(string unlockedItemId)
    {
        // 전체 인벤토리가 뜨는 느낌용 딜레이
        if (firstDelay > 0f)
            yield return new WaitForSeconds(firstDelay);

        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                if (s == null || s.slotRoot == null || s.canvasGroup == null)
                    continue;

                // ★ 완료를 기다리지 않고 바로 시작만 하고
                StartCoroutine(PlaySlotEnter(s));

                // ★ slotInterval 만큼 기다렸다가 다음 슬롯 시작
                if (slotInterval > 0f)
                    yield return new WaitForSeconds(slotInterval);
            }
        }

        // 모든 슬롯이 시작된 뒤, 마지막 애니메이션이 끝났을 법한 시간만큼 기다림
        if (slotAnimDuration > 0f)
            yield return new WaitForSeconds(slotAnimDuration);

        // 새로 언락된 슬롯에 뱃지 팝 애니메이션
        var newSlot2 = FindSlot(unlockedItemId);
        if (newSlot2 != null && newSlot2.badgeRoot != null)
        {
            yield return StartCoroutine(PlayBadgePop(newSlot2.badgeRoot.transform));
        }

        _sequenceRoutine = null;
    }

    /// <summary>
    /// 슬롯 하나 등장 애니메이션 (scale 0 -> 1, alpha 0 -> 1)
    /// </summary>
    private IEnumerator PlaySlotEnter(SlotUI s)
    {
        float t = 0f;
        Vector3 start = Vector3.one * slotStartScale;
        Vector3 end = Vector3.one;

        s.slotRoot.localScale = start;
        if (s.canvasGroup != null) s.canvasGroup.alpha = 0f;

        while (t < slotAnimDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / slotAnimDuration);
            float eased = Mathf.SmoothStep(0f, 1f, x);

            s.slotRoot.localScale = Vector3.Lerp(start, end, eased);
            if (s.canvasGroup != null)
                s.canvasGroup.alpha = x;

            yield return null;
        }

        s.slotRoot.localScale = end;
        if (s.canvasGroup != null) s.canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 새로 언락된 슬롯 뱃지 팝 애니메이션
    /// </summary>
    private IEnumerator PlayBadgePop(Transform badge)
    {
        badge.gameObject.SetActive(true);

        float t = 0f;
        Vector3 over = Vector3.one * badgePopOvershoot;

        while (t < badgePopDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / badgePopDuration);

            if (x < 0.5f)
            {
                float inner = x / 0.5f;
                badge.localScale = Vector3.Lerp(Vector3.zero, over, inner);
            }
            else
            {
                float inner = (x - 0.5f) / 0.5f;
                badge.localScale = Vector3.Lerp(over, Vector3.one, inner);
            }

            yield return null;
        }

        badge.localScale = Vector3.one;
    }

    /// <summary>
    /// bool이 false일 때: 애니메이션 없이 즉시 보여주기
    /// </summary>
    private void ShowSlotsInstant(string unlockedItemId)
    {
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s == null || s.slotRoot == null) continue;

            s.slotRoot.localScale = Vector3.one;
            if (s.canvasGroup != null)
                s.canvasGroup.alpha = 1f;

            if (s.badgeRoot != null)
            {
                bool isNewlyUnlocked = !string.IsNullOrEmpty(unlockedItemId) && s.itemId == unlockedItemId;
                s.badgeRoot.SetActive(isNewlyUnlocked);
                s.badgeRoot.transform.localScale = Vector3.one;
            }
        }
    }

    private SlotUI FindSlot(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || slots == null) return null;

        foreach (var s in slots)
        {
            if (s != null && s.itemId == itemId)
                return s;
        }
        return null;
    }
}
