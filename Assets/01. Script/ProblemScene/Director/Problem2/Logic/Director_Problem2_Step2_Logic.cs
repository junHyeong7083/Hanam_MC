using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem2_Step2_Logic - 문제2 스텝2의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 감정 필름 슬롯들을 화면에 배치하고, 사용자가 각 필름 버튼을 클릭하면
///         텍스트를 숨기고 이미지를 표시하는 "리빌(reveal)" 처리를 수행한다.
///         모든 슬롯이 리빌되면 completed 대사를 표시하고 다음 스텝으로 진행한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측. EmotionSlot 배열, CompletionGate 등 abstract.
/// 【문제/스텝】 Director 테마 / 문제2 / 스텝2 (메인 활동 - 감정 필름 리빌)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem2_Step2 (Binder 자식 클래스)
/// 【참조되는 곳】 DialogueSequencer (대사), StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → enter 대사 → 대사 완료 → 각 필름 클릭 → 텍스트→이미지 전환 →
///         모든 필름 리빌 → completed 대사 → 다음 스텝
/// </summary>
public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    /// <summary>
    /// 개별 감정 슬롯 데이터. 버튼, 리빌 스프라이트, 텍스트/이미지 루트를 포함한다.
    /// </summary>
    [Serializable]
    protected class EmotionSlot
    {
        public Button filmButton;          // 클릭용 필름 버튼
        public Sprite revealedSprite;      // 리빌 후 표시할 스프라이트
        public GameObject textRoot;        // 리빌 전 텍스트 루트 (클릭 전 보임)
        public GameObject imageRoot;       // 리빌 후 이미지 루트 (클릭 후 보임)

        [NonSerialized] public bool revealed;  // 리빌 완료 여부 (런타임 상태)
    }

    #region Abstract Properties

    /// <summary>감정 슬롯 배열 (자식에서 SerializeField로 바인딩)</summary>
    protected abstract EmotionSlot[] Slots { get; }

    /// <summary>완료 게이트 - 모든 슬롯 리빌 시 다음 스텝 진행</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    #endregion

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;  // 대사 시퀀서

    /// <summary>모든 슬롯 리빌 완료 여부</summary>
    private bool _completed = false;

    /// <summary>대사 재생 중 상호작용 잠금 플래그</summary>
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