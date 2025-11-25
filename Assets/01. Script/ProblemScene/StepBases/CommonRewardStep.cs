using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 공용 보상 연출 Step
/// - ProblemStepBase 를 상속
/// - 여러 UI 요소를 배열(sequenceItems)로 받아 순차적으로 등장
/// - 인벤토리 패널 + 보상 DB 저장까지 포함
/// </summary>
public class CommonRewardStep : ProblemStepBase
{
    [Serializable]
    public class SequenceItem
    {
        [Header("디버그/설명용 이름 (선택)")]
        public string name;

        [Header("UI Root")]
        public RectTransform root;         // 위치/스케일을 줄 대상
        public CanvasGroup canvasGroup;    // 알파 페이드 대상 (없으면 생략 가능)

        [Header("타이밍")]
        [Tooltip("이전 아이템이 끝난 후 기다릴 시간")]
        public float delay = 0f;
        [Tooltip("이 아이템의 등장 애니메이션 시간")]
        public float duration = 0.4f;

        [Header("위치/스케일 연출")]
        [Tooltip("basePos + startOffset 위치에서 시작")]
        public Vector2 startOffset = Vector2.zero;

        [Tooltip("스케일 애니메이션을 사용할지 여부")]
        public bool useScale = false;
        public float startScale = 1f;

        [Tooltip("스케일 오버슈트(통통 튀는 효과) 사용 여부")]
        public bool useOvershoot = false;
        public float overshootScale = 1.1f;

        // --- 내부 캐시 ---
        [NonSerialized] public bool initialized;
        [NonSerialized] public Vector2 basePos;
        [NonSerialized] public Vector3 baseScale;
    }

    [Header("연출 시퀀스 (위에서 아래 순서대로 재생)")]
    [SerializeField] private SequenceItem[] sequenceItems;

    [Header("인벤토리 패널 (선택)")]
    [Tooltip("보상 아이템을 보여줄 RewardInventoryPanel (없으면 무시)")]
    [SerializeField] private RewardInventoryPanel inventoryPanel;

    [Tooltip("sequenceItems 중 인벤토리가 등장하는 인덱스 (없으면 -1)")]
    [SerializeField] private int inventorySequenceIndex = -1;

    [Header("보상 메타 (DB 저장용)")]
    [SerializeField] private string rewardItemId = "mind_lens";
    [SerializeField] private string rewardItemName = "마음 렌즈";

    // 내부 상태
    private Coroutine _sequenceRoutine;
    private bool _rewardSaved;

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>
    /// 스텝이 켜질 때(활성화될 때) 호출됨.
    /// ProblemStepBase.OnEnable -> OnStepEnter() 순서로 들어옴.
    /// </summary>
    protected override void OnStepEnter()
    {
        // 1) 보상 DB 저장 (한 번만)
        SaveRewardToDbOnce();

        // 2) 연출 시퀀스 시작
        StartSequence();
    }

    /// <summary>
    /// 스텝이 꺼질 때 정리
    /// </summary>
    protected override void OnStepExit()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    // =========================
    // 시퀀스 제어
    // =========================

    /// <summary>
    /// 외부에서 다시 재생하고 싶을 때 호출
    /// </summary>
    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        InitState();
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    /// <summary>
    /// sequenceItems 초기 위치/스케일/알파 세팅
    /// </summary>
    private void InitState()
    {
        if (sequenceItems == null) return;

        foreach (var item in sequenceItems)
        {
            if (item == null || item.root == null)
                continue;

            if (!item.initialized)
            {
                item.basePos = item.root.anchoredPosition;
                item.baseScale = item.root.localScale;
                item.initialized = true;
            }

            // 시작 위치: basePos + startOffset
            item.root.anchoredPosition = item.basePos + item.startOffset;

            // 시작 스케일
            if (item.useScale)
                item.root.localScale = Vector3.one * item.startScale;
            else
                item.root.localScale = item.baseScale;

            // 시작 알파 0
            if (item.canvasGroup != null)
                item.canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// 전체 시퀀스 재생
    /// </summary>
    private IEnumerator SequenceRoutine()
    {
        if (sequenceItems == null || sequenceItems.Length == 0)
            yield break;

        for (int i = 0; i < sequenceItems.Length; i++)
        {
            var item = sequenceItems[i];
            if (item == null || item.root == null)
                continue;

            // 인벤토리 등장 시점이면 ShowInventory 호출
            if (i == inventorySequenceIndex &&
                inventoryPanel != null &&
                !string.IsNullOrEmpty(rewardItemId))
            {
                inventoryPanel.ShowInventory(rewardItemId, true);
            }

            // 개별 아이템 앞 딜레이
            if (item.delay > 0f)
                yield return new WaitForSeconds(item.delay);

            // 개별 아이템 등장 애니메이션
            yield return PlayItemRoutine(item);
        }
    }

    /// <summary>
    /// 개별 SequenceItem 등장 애니메이션
    /// </summary>
    private IEnumerator PlayItemRoutine(SequenceItem item)
    {
        float duration = Mathf.Max(0.001f, item.duration);
        float t = 0f;

        Vector2 startPos = item.root.anchoredPosition;
        Vector2 endPos = item.basePos;

        Vector3 baseScale = item.baseScale;
        Vector3 startScale = item.useScale ? Vector3.one * item.startScale : baseScale;
        Vector3 overScale = item.useOvershoot
            ? Vector3.one * item.overshootScale
            : startScale;
        Vector3 endScale = baseScale;

        if (item.canvasGroup != null)
            item.canvasGroup.alpha = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / duration);
            float ease = Mathf.SmoothStep(0f, 1f, x);

            // 위치 lerp
            item.root.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);

            // 스케일 연출
            if (item.useScale)
            {
                if (item.useOvershoot)
                {
                    if (x < 0.5f)
                    {
                        float inner = x / 0.5f;
                        float lerp = Mathf.SmoothStep(0f, 1f, inner);
                        item.root.localScale = Vector3.Lerp(startScale, overScale, lerp);
                    }
                    else
                    {
                        float inner = (x - 0.5f) / 0.5f;
                        float lerp = Mathf.SmoothStep(0f, 1f, inner);
                        item.root.localScale = Vector3.Lerp(overScale, endScale, lerp);
                    }
                }
                else
                {
                    item.root.localScale = Vector3.Lerp(startScale, endScale, ease);
                }
            }

            // 알파 페이드
            if (item.canvasGroup != null)
                item.canvasGroup.alpha = x;

            yield return null;
        }

        // 최종값 보정
        item.root.anchoredPosition = endPos;
        if (item.useScale)
            item.root.localScale = endScale;
        if (item.canvasGroup != null)
            item.canvasGroup.alpha = 1f;
    }

    // =========================
    // 보상 DB 저장
    // =========================

    /// <summary>
    /// 보상 Attempt + 인벤토리 저장을 한 번만 수행
    /// ProblemStepBase.stepKey / context 를 사용
    /// </summary>
    private void SaveRewardToDbOnce()
    {
        if (_rewardSaved) return;
        _rewardSaved = true;

        if (context == null)
        {
            Debug.LogWarning("[CommonRewardStep] ProblemContext가 설정되지 않아 보상 저장 스킵");
            return;
        }

        // 이 스텝의 key 설정 (인스펙터에서 stepKey 세팅해둘 것)
        if (!string.IsNullOrEmpty(stepKey))
            context.CurrentStepKey = stepKey;

        // body에는 이 스텝 전용 데이터 구조만 넣어준다.
        var body = new
        {
            items = new[]
            {
                new
                {
                    itemId = rewardItemId,
                    itemName = rewardItemName,
                    unlocked = true
                }
            }
        };

        // 기존 Step4처럼 Reward + InventoryItem 저장
        context.SaveReward(body, rewardItemId, rewardItemName);
    }
}
