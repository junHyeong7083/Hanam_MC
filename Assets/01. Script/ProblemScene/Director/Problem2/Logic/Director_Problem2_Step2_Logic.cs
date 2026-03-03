using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    [Serializable]
    protected class EmotionLightSlot
    {
        public int sceneTextId;

        [Header("Top Light UI")]
        public GameObject lightLockedRoot;
        public GameObject lightRevealedRoot;

        [Header("Bottom Film Card UI")]
        public Button filmButton;
        public Sprite filmClickedSprite;
        public GameObject filmTouchPromptRoot;

        [Header("Film After Click Display")]
        public GameObject filmAfterClickRoot;
        public Text filmSceneText;

        [NonSerialized] public bool revealed;
        [NonSerialized] public Sprite filmOriginalSprite;
    }

    [Header("Emotion Light Slots (�ڽĿ��� ����)")]
    protected abstract EmotionLightSlot[] Slots { get; }

    [Header("Light ���� �ִϸ��̼� (�ڽĿ��� ����)")]
    protected abstract bool PlayLightAppearAnimation { get; }
    protected abstract float LightAppearDuration { get; }
    protected abstract float LightAppearScale { get; }

    [Header("�Ϸ� ����Ʈ (�ڽĿ��� ����)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // =======================
    // �߰�: Problem1 Step2 ��Ÿ��(guide text + next button)
    // =======================
    [Header("Guide Text (Localized)")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdOnEnter = 0;

    [Header("Next Shoot Button")]
    [SerializeField] private GameObject nextShootButtonRoot;
    [SerializeField] private Button nextShootButton;

    [Header("NextStep Auto Call")]
    [SerializeField] private bool autoCallNextStep = true;

    [Header("Fallback Callback")]
    [SerializeField] private UnityEvent onClickNextShootFallback;

    private bool _completed = false;
    private StepFlowController _flowController;

    protected override void OnStepEnter()
    {
        _completed = false;

        InitSlots();

        if (CompletionGate != null)
        {
            int total = (Slots != null) ? Slots.Length : 0;
            CompletionGate.ResetGate(total);
        }

        ApplyGuideText(guideTextIdOnEnter);
        SetNextShootButtonVisible(false);

        CacheStepFlowController();
        BindNextShootButton();
    }

    protected override void OnStepExit()
    {
        UnbindNextShootButton();
        _completed = false;
    }

    private void InitSlots()
    {
        if (Slots == null) return;

        foreach (var slot in Slots)
        {
            if (slot == null) continue;

            slot.revealed = false;

            SetRevealState(slot, reveal: false, immediate: true);

            if (slot.filmButton != null)
            {
                var img = slot.filmButton.GetComponent<Image>();
                if (img != null)
                    slot.filmOriginalSprite = img.sprite;

                var captured = slot;
                slot.filmButton.onClick.RemoveAllListeners();
                slot.filmButton.onClick.AddListener(() => OnFilmClicked(captured));
            }
        }
    }

    private void SetRevealState(EmotionLightSlot slot, bool reveal, bool immediate = false)
    {
        slot.revealed = reveal;

        if (slot.lightLockedRoot != null)
            slot.lightLockedRoot.SetActive(!reveal);

        Debug.Log($"[Step2] SetRevealState reveal={reveal}, lightRevealedRoot={(slot.lightRevealedRoot != null ? slot.lightRevealedRoot.name : "NULL")}, activeSelf before={slot.lightRevealedRoot?.activeSelf}");

        if (slot.lightRevealedRoot != null)
        {
            slot.lightRevealedRoot.SetActive(reveal);
            Debug.Log($"[Step2] After SetActive({reveal}), activeSelf={slot.lightRevealedRoot.activeSelf}, activeInHierarchy={slot.lightRevealedRoot.activeInHierarchy}");

            if (reveal && PlayLightAppearAnimation && !immediate)
            {
                StartCoroutine(PlayLightAppear(slot.lightRevealedRoot.transform));
            }
            else if (reveal && immediate)
            {
                slot.lightRevealedRoot.transform.localScale = Vector3.one;
            }
        }

        // 필름 스프라이트 교체 (클릭 전/후)
        if (slot.filmButton != null)
        {
            var img = slot.filmButton.GetComponent<Image>();
            if (img != null)
            {
                if (reveal && slot.filmClickedSprite != null)
                    img.sprite = slot.filmClickedSprite;
                else if (!reveal && slot.filmOriginalSprite != null)
                    img.sprite = slot.filmOriginalSprite;
            }
        }

        if (slot.filmTouchPromptRoot != null)
            slot.filmTouchPromptRoot.SetActive(!reveal);

        if (slot.filmAfterClickRoot != null)
            slot.filmAfterClickRoot.SetActive(reveal);

        if (reveal && slot.filmSceneText != null && slot.sceneTextId > 0)
        {
            var resolved = ProblemRuntime.L(slot.sceneTextId);
            slot.filmSceneText.text = resolved;
            Debug.Log($"[Step2] filmSceneText set: textId={slot.sceneTextId}, text='{resolved}', go={slot.filmSceneText.gameObject.name}, activeInHierarchy={slot.filmSceneText.gameObject.activeInHierarchy}");
        }
        else if (reveal)
        {
            Debug.LogWarning($"[Step2] filmSceneText 미설정! filmSceneText={(slot.filmSceneText != null ? "OK" : "NULL")}, sceneTextId={slot.sceneTextId}");
        }
    }

    private void OnFilmClicked(EmotionLightSlot slot)
    {
        if (slot.revealed)
            return;

        SetRevealState(slot, true);

        if (CompletionGate != null)
            CompletionGate.MarkOneDone();

        TryHandleCompleted();
    }

    private void TryHandleCompleted()
    {
        if (_completed) return;

        var slots = Slots;
        int total = (slots != null) ? slots.Length : 0;
        if (total <= 0) return;

        int revealedCount = 0;
        for (int i = 0; i < total; i++)
        {
            if (slots[i] != null && slots[i].revealed)
                revealedCount++;
        }

        if (revealedCount < total) return;

        _completed = true;

        SetNextShootButtonVisible(true);
    }

    private void ApplyGuideText(int textId)
    {
        if (guideText == null) return;
        guideText.text = ProblemRuntime.L(textId);
    }

    private void SetNextShootButtonVisible(bool visible)
    {
        if (nextShootButtonRoot != null)
            nextShootButtonRoot.SetActive(visible);

        if (nextShootButton != null)
            nextShootButton.interactable = visible;
    }

    private void BindNextShootButton()
    {
        if (nextShootButton == null) return;
        nextShootButton.onClick.RemoveListener(OnClickNextShoot);
        nextShootButton.onClick.AddListener(OnClickNextShoot);
    }

    private void UnbindNextShootButton()
    {
        if (nextShootButton == null) return;
        nextShootButton.onClick.RemoveListener(OnClickNextShoot);
    }

    private void OnClickNextShoot()
    {
        if (!_completed) return;

        if (autoCallNextStep && TryCallNextStep())
            return;

        onClickNextShootFallback?.Invoke();
    }

    private void CacheStepFlowController()
    {
        _flowController = null;
        if (!autoCallNextStep) return;
        _flowController = GetComponentInParent<StepFlowController>();
    }

    private bool TryCallNextStep()
    {
        if (_flowController == null)
            _flowController = GetComponentInParent<StepFlowController>();

        if (_flowController == null) return false;

        _flowController.NextStep();
        return true;
    }

    private IEnumerator PlayLightAppear(Transform target)
    {
        if (target == null) yield break;

        Vector3 startScale = Vector3.one * 0.8f;
        Vector3 peakScale = Vector3.one * LightAppearScale;
        Vector3 endScale = Vector3.one;

        float t = 0f;
        target.localScale = startScale;
        float duration = LightAppearDuration;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float lerpT = Mathf.Clamp01(t);

            if (lerpT < 0.5f)
            {
                float u = lerpT / 0.5f;
                target.localScale = Vector3.Lerp(startScale, peakScale, u);
            }
            else
            {
                float u = (lerpT - 0.5f) / 0.5f;
                target.localScale = Vector3.Lerp(peakScale, endScale, u);
            }

            yield return null;
        }

        target.localScale = endScale;
    }
}