using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director 테마 / Problem1 / Step4 (보상 연출 + 보상 DB 기록)
/// </summary>
public class Director_Problem1_Step4 : MonoBehaviour
{
    // ===== Reward Item =====
    [Header("Reward Item")]
    [SerializeField] private RectTransform rewardItemRoot;
    [SerializeField] private CanvasGroup rewardItemCanvasGroup;
    [SerializeField] private float rewardEnterDuration = 1f;
    [SerializeField] private float rewardStartScale = 0.5f;
    [SerializeField] private float rewardStartOffsetY = -100f;
    [SerializeField] private float rewardOvershootScale = 1.1f;

    // ===== 텍스트(제목 + 서브텍스트) =====
    [Header("Text Group (제목 + 설명)")]
    [SerializeField] private RectTransform textGroupRoot;
    [SerializeField] private CanvasGroup textGroupCanvasGroup;
    [SerializeField] private float textDelay = 0.5f;
    [SerializeField] private float textEnterDuration = 0.4f;
    [SerializeField] private float textStartOffsetY = -30f;

    // ===== 인벤토리 패널 =====
    [Header("Inventory Panel (선택)")]
    [SerializeField] private RewardInventoryPanel inventoryPanel;
    [SerializeField] private RectTransform inventoryRoot;
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private float inventoryDelay = 0.2f;
    [SerializeField] private float inventoryEnterDuration = 0.4f;
    [SerializeField] private float inventoryStartOffsetY = -30f;

    // ===== Next 버튼 =====
    [Header("Next Button")]
    [SerializeField] private CanvasGroup nextButtonCanvasGroup;
    [SerializeField] private float nextButtonDelay = 1.5f;
    [SerializeField] private float nextButtonFadeDuration = 0.3f;

    // ===== Part 완료 뱃지 =====
    [Header("Part Complete Badge")]
    [SerializeField] private CanvasGroup partCompleteBadgeCanvasGroup;
    [SerializeField] private float badgeDelay = 1.8f;
    [SerializeField] private float badgeFadeDuration = 0.3f;

    // ===== 보상 메타 =====
    [Header("Reward Meta")]
    [SerializeField] private string rewardItemId = "mind_lens";
    [SerializeField] private string rewardItemName = "마음 렌즈";

    // ===== 공용 Problem 컨텍스트 =====
    [Header("공용 Problem 컨텍스트")]
    [SerializeField] private ProblemContext context;

    // ===== 내부 상태 =====
    private Coroutine _sequenceRoutine;

    private bool _rewardInit;
    private Vector2 _rewardBasePos;
    private Vector3 _rewardBaseScale;

    private bool _textInit;
    private Vector2 _textBasePos;

    private bool _inventoryInit;
    private Vector2 _inventoryBasePos;

    private bool _rewardSaved;

    private void OnEnable()
    {
        StartSequence();
        SaveRewardToDbOnce();
    }

    private void OnDisable()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        InitState();
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    private void InitState()
    {
        // Reward 기본값 캐시
        if (rewardItemRoot != null && !_rewardInit)
        {
            _rewardBasePos = rewardItemRoot.anchoredPosition;
            _rewardBaseScale = rewardItemRoot.localScale;
            _rewardInit = true;
        }

        // Text 기본값 캐시
        if (textGroupRoot != null && !_textInit)
        {
            _textBasePos = textGroupRoot.anchoredPosition;
            _textInit = true;
        }

        // Inventory 기본값 캐시
        if (inventoryRoot != null && !_inventoryInit)
        {
            _inventoryBasePos = inventoryRoot.anchoredPosition;
            _inventoryInit = true;
        }

        // Reward 초기 상태
        if (rewardItemRoot != null)
        {
            rewardItemRoot.anchoredPosition = _rewardBasePos + new Vector2(0f, rewardStartOffsetY);
            rewardItemRoot.localScale = new Vector3(rewardStartScale, rewardStartScale, 1f);
        }
        if (rewardItemCanvasGroup != null)
            rewardItemCanvasGroup.alpha = 0f;

        // Text 초기 상태
        if (textGroupRoot != null)
            textGroupRoot.anchoredPosition = _textBasePos + new Vector2(0f, textStartOffsetY);
        if (textGroupCanvasGroup != null)
            textGroupCanvasGroup.alpha = 0f;

        // Inventory 초기 상태
        if (inventoryRoot != null)
            inventoryRoot.anchoredPosition = _inventoryBasePos + new Vector2(0f, inventoryStartOffsetY);
        if (inventoryCanvasGroup != null)
            inventoryCanvasGroup.alpha = 0f;

        // 버튼/뱃지 초기 상태
        if (nextButtonCanvasGroup != null)
            nextButtonCanvasGroup.alpha = 0f;
        if (partCompleteBadgeCanvasGroup != null)
            partCompleteBadgeCanvasGroup.alpha = 0f;
    }

    private IEnumerator SequenceRoutine()
    {
        // 1) 보상 아이템 등장
        yield return RewardEnterRoutine();

        // 2) 텍스트 등장 (완전히 끝날 때까지 기다림)
        if (textGroupRoot != null && textGroupCanvasGroup != null)
            yield return TextEnterRoutine();

        // 3) 인벤토리 패널 등장 (텍스트 이후)
        if (inventoryRoot != null && inventoryCanvasGroup != null && inventoryPanel != null)
            yield return InventoryEnterRoutine();

        // 4) 버튼/뱃지 페이드 인 (동시에)
        if (nextButtonCanvasGroup != null)
            StartCoroutine(NextButtonRoutine());

        if (partCompleteBadgeCanvasGroup != null)
            StartCoroutine(BadgeRoutine());
    }

    // ==== Reward Item ====
    private IEnumerator RewardEnterRoutine()
    {
        if (rewardItemRoot == null)
            yield break;

        float t = 0f;
        Vector2 startPos = rewardItemRoot.anchoredPosition;
        Vector2 endPos = _rewardBasePos;

        Vector3 startScale = new Vector3(rewardStartScale, rewardStartScale, 1f);
        Vector3 overScale = new Vector3(rewardOvershootScale, rewardOvershootScale, 1f);
        Vector3 endScale = _rewardBaseScale;

        if (rewardItemCanvasGroup != null)
            rewardItemCanvasGroup.alpha = 0f;

        while (t < rewardEnterDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / rewardEnterDuration);

            // 위치: 아래에서 위로
            float posLerp = Mathf.SmoothStep(0f, 1f, x);
            rewardItemRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, posLerp);

            // 스케일: 절반까지 overshoot, 이후 1.0으로 수렴
            if (x < 0.5f)
            {
                float inner = x / 0.5f;
                float lerp = Mathf.SmoothStep(0f, 1f, inner);
                rewardItemRoot.localScale = Vector3.Lerp(startScale, overScale, lerp);
            }
            else
            {
                float inner = (x - 0.5f) / 0.5f;
                float lerp = Mathf.SmoothStep(0f, 1f, inner);
                rewardItemRoot.localScale = Vector3.Lerp(overScale, endScale, lerp);
            }

            if (rewardItemCanvasGroup != null)
                rewardItemCanvasGroup.alpha = x;

            yield return null;
        }

        rewardItemRoot.anchoredPosition = endPos;
        rewardItemRoot.localScale = endScale;
        if (rewardItemCanvasGroup != null)
            rewardItemCanvasGroup.alpha = 1f;
    }

    // ==== Text ====
    private IEnumerator TextEnterRoutine()
    {
        yield return new WaitForSeconds(textDelay);

        if (textGroupRoot == null || textGroupCanvasGroup == null)
            yield break;

        float t = 0f;
        Vector2 startPos = textGroupRoot.anchoredPosition;
        Vector2 endPos = _textBasePos;

        while (t < textEnterDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / textEnterDuration);
            float lerp = Mathf.SmoothStep(0f, 1f, x);

            textGroupRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);
            textGroupCanvasGroup.alpha = x;

            yield return null;
        }

        textGroupRoot.anchoredPosition = endPos;
        textGroupCanvasGroup.alpha = 1f;
    }

    // ==== Inventory Panel ====
    private IEnumerator InventoryEnterRoutine()
    {
        if (inventoryRoot == null || inventoryCanvasGroup == null || inventoryPanel == null)
            yield break;

        // 텍스트가 다 나온 뒤 약간 기다렸다가 등장
        if (inventoryDelay > 0f)
            yield return new WaitForSeconds(inventoryDelay);

        // 이 시점에 슬롯 애니메이션 시작
        inventoryPanel.ShowInventory(rewardItemId, true);

        float t = 0f;
        Vector2 startPos = inventoryRoot.anchoredPosition;
        Vector2 endPos = _inventoryBasePos;

        while (t < inventoryEnterDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / inventoryEnterDuration);
            float lerp = Mathf.SmoothStep(0f, 1f, x);

            inventoryRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);
            inventoryCanvasGroup.alpha = x;

            yield return null;
        }

        inventoryRoot.anchoredPosition = endPos;
        inventoryCanvasGroup.alpha = 1f;
    }

    // ==== Next Button ====
    private IEnumerator NextButtonRoutine()
    {
        yield return new WaitForSeconds(nextButtonDelay);

        if (nextButtonCanvasGroup == null) yield break;

        float t = 0f;
        while (t < nextButtonFadeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / nextButtonFadeDuration);
            nextButtonCanvasGroup.alpha = x;
            yield return null;
        }

        nextButtonCanvasGroup.alpha = 1f;
    }

    // ==== Badge ====
    private IEnumerator BadgeRoutine()
    {
        yield return new WaitForSeconds(badgeDelay);

        if (partCompleteBadgeCanvasGroup == null) yield break;

        float t = 0f;
        while (t < badgeFadeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / badgeFadeDuration);
            partCompleteBadgeCanvasGroup.alpha = x;
            yield return null;
        }

        partCompleteBadgeCanvasGroup.alpha = 1f;
    }

    // ====== 보상 로그 + 인벤토리 저장 ======
    private void SaveRewardToDbOnce()
    {
        if (_rewardSaved) return;
        _rewardSaved = true;

        if (context == null)
        {
            Debug.LogWarning("[Director_Problem1_Step4] ProblemContext가 설정되지 않아 보상 저장 스킵");
            return;
        }

        // 이 스텝 키 설정
        context.CurrentStepKey = "Director_Problem1_Step4";

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

        context.SaveReward(body, rewardItemId, rewardItemName);
    }
}
