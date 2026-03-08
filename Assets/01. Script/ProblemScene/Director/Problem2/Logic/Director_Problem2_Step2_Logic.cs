using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    [Serializable]
    protected class EmotionSlot
    {
        public Button filmButton;
        public Sprite revealedSprite;
        public GameObject textRoot;
        public GameObject imageRoot;

        [NonSerialized] public bool revealed;
    }

    protected abstract EmotionSlot[] Slots { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    private bool _completed = false;
    private bool _interactionLocked = true;

    protected override void OnStepEnter()
    {
        _completed = false;

        InitSlots();

        if (CompletionGate != null)
        {
            int total = (Slots != null) ? Slots.Length : 0;
            CompletionGate.ResetGate(total);
        }

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _completed = false;
        _interactionLocked = true;
    }

    private void InitSlots()
    {
        if (Slots == null) return;

        foreach (var slot in Slots)
        {
            if (slot == null) continue;

            slot.revealed = false;

            if (slot.textRoot != null)
                slot.textRoot.SetActive(true);

            if (slot.imageRoot != null)
                slot.imageRoot.SetActive(false);

            if (slot.filmButton != null)
            {
                var captured = slot;
                slot.filmButton.onClick.RemoveAllListeners();
                slot.filmButton.onClick.AddListener(() => OnFilmClicked(captured));
            }
        }
    }

    private void OnFilmClicked(EmotionSlot slot)
    {
        if (_interactionLocked) return;
        if (slot.revealed) return;

        slot.revealed = true;

        if (slot.textRoot != null)
            slot.textRoot.SetActive(false);

        if (slot.imageRoot != null)
            slot.imageRoot.SetActive(true);

        if (slot.revealedSprite != null && slot.filmButton != null)
        {
            var img = slot.filmButton.GetComponent<Image>();
            if (img != null)
                img.sprite = slot.revealedSprite;
        }

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

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }
}