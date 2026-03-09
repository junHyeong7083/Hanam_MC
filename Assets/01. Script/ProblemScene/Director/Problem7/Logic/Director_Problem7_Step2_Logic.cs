using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step2 로직 베이스
/// - "보여지는 나 vs 진짜 나" 가면 선택
/// - 2단계: selectMask → selectFeeling → 완료(NextStepBtn 표시)
/// </summary>
public abstract class Director_Problem7_Step2_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    [Serializable]
    public class ChoiceItem
    {
        public string id;           // DB 저장용 ID (예: "cool", "anxious")
        public int labelTextId;     // CSV textId (라벨 표시용)
        public Button button;       // 버튼 참조
        public GameObject clickImage;  // 선택 시 표시할 이미지
    }

    // =========================
    // DB 저장용 DTO
    // =========================

    [Serializable]
    private class SelectedChoiceDto
    {
        public string id;
        public string label;
    }

    [Serializable]
    private class MaskFeelingAttemptDto
    {
        public SelectedChoiceDto mask;
        public SelectedChoiceDto feeling;
    }

    protected enum Phase { SelectMask, SelectFeeling }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("페이즈 전환 텍스트")]
    protected abstract int MaskSelectedTextId { get; }

    [Header("가면 선택 화면")]
    protected abstract GameObject SelectMaskRoot { get; }
    protected abstract ChoiceItem[] MaskChoices { get; }

    [Header("진짜 마음 선택 화면")]
    protected abstract GameObject SelectFeelingRoot { get; }
    protected abstract ChoiceItem[] FeelingChoices { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    #endregion

    #region Virtual Config

    protected virtual float MaskSelectDelay => 2.0f;
    protected virtual float FeelingSelectDelay => 2.0f;

    #endregion

    // 내부 상태
    private Phase _currentPhase;
    private ChoiceItem _selectedMask;
    private ChoiceItem _selectedFeeling;
    private Coroutine _transitionRoutine;
    private bool _interactionLocked = true;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentPhase = Phase.SelectMask;
        _selectedMask = null;
        _selectedFeeling = null;

        SetupAllPhases();
        ApplyLabelsFromTextId();
        ShowPhase(Phase.SelectMask);

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
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        RemoveAllListeners();
    }

    // =========================
    // 초기 설정
    // =========================

    private void SetupAllPhases()
    {
        if (SelectMaskRoot != null) SelectMaskRoot.SetActive(false);
        if (SelectFeelingRoot != null) SelectFeelingRoot.SetActive(false);

        ResetClickImages(MaskChoices);
        ResetClickImages(FeelingChoices);

        RegisterListeners();
    }

    private void ResetClickImages(ChoiceItem[] choices)
    {
        if (choices == null) return;
        foreach (var choice in choices)
        {
            if (choice?.clickImage != null)
                choice.clickImage.SetActive(false);
        }
    }

    private void ApplyLabelsFromTextId()
    {
        ApplyLabels(MaskChoices);
        ApplyLabels(FeelingChoices);
    }

    private void ApplyLabels(ChoiceItem[] choices)
    {
        if (choices == null) return;
        foreach (var choice in choices)
        {
            if (choice == null || choice.button == null || choice.labelTextId <= 0) continue;
            var text = choice.button.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = ProblemRuntime.L(choice.labelTextId);
        }
    }

    private void RegisterListeners()
    {
        var masks = MaskChoices;
        if (masks != null)
        {
            for (int i = 0; i < masks.Length; i++)
            {
                var choice = masks[i];
                if (choice?.button != null)
                {
                    choice.button.onClick.RemoveAllListeners();
                    choice.button.onClick.AddListener(() => OnMaskSelected(choice));
                }
            }
        }

        var feelings = FeelingChoices;
        if (feelings != null)
        {
            for (int i = 0; i < feelings.Length; i++)
            {
                var choice = feelings[i];
                if (choice?.button != null)
                {
                    choice.button.onClick.RemoveAllListeners();
                    choice.button.onClick.AddListener(() => OnFeelingSelected(choice));
                }
            }
        }
    }

    private void RemoveAllListeners()
    {
        var masks = MaskChoices;
        if (masks != null)
        {
            foreach (var choice in masks)
                if (choice?.button != null) choice.button.onClick.RemoveAllListeners();
        }

        var feelings = FeelingChoices;
        if (feelings != null)
        {
            foreach (var choice in feelings)
                if (choice?.button != null) choice.button.onClick.RemoveAllListeners();
        }
    }

    // =========================
    // Phase 전환
    // =========================

    private void ShowPhase(Phase phase)
    {
        _currentPhase = phase;

        if (SelectMaskRoot != null) SelectMaskRoot.SetActive(phase == Phase.SelectMask);
        if (SelectFeelingRoot != null) SelectFeelingRoot.SetActive(phase == Phase.SelectFeeling);

        // Mask 선택 완료 → SelectFeeling 전환 시 텍스트 변경
        if (phase == Phase.SelectFeeling && dialogueSequencer != null && MaskSelectedTextId > 0)
            dialogueSequencer.SetText(MaskSelectedTextId);
    }

    // =========================
    // 버튼 핸들러
    // =========================

    private void OnMaskSelected(ChoiceItem choice)
    {
        if (_interactionLocked) return;
        if (_currentPhase != Phase.SelectMask) return;
        if (_selectedMask != null) return;

        _selectedMask = choice;
        OnMaskSelectedVisual(choice);

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionAfterDelay(Phase.SelectFeeling, MaskSelectDelay));
    }

    private void OnFeelingSelected(ChoiceItem choice)
    {
        if (_interactionLocked) return;
        if (_currentPhase != Phase.SelectFeeling) return;
        if (_selectedFeeling != null) return;

        _selectedFeeling = choice;
        OnFeelingSelectedVisual(choice);

        var body = new MaskFeelingAttemptDto
        {
            mask = new SelectedChoiceDto
            {
                id = _selectedMask?.id,
                label = _selectedMask != null && _selectedMask.labelTextId > 0
                    ? ProblemRuntime.L(_selectedMask.labelTextId) : ""
            },
            feeling = new SelectedChoiceDto
            {
                id = _selectedFeeling?.id,
                label = _selectedFeeling.labelTextId > 0
                    ? ProblemRuntime.L(_selectedFeeling.labelTextId) : ""
            }
        };
        SaveAttempt(body);

        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(CompleteAfterDelay(FeelingSelectDelay));
    }

    private IEnumerator CompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 완료 처리: completedTextIds[0]에 feeling 완료 텍스트 → 마지막에 NextStepBtn 표시
        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }

    // =========================
    // 코루틴
    // =========================

    private IEnumerator TransitionAfterDelay(Phase nextPhase, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPhase(nextPhase);
    }

    // =========================
    // 시각 효과 (파생 클래스에서 override 가능)
    // =========================

    protected virtual void OnMaskSelectedVisual(ChoiceItem selected)
    {
        var masks = MaskChoices;
        if (masks == null) return;

        foreach (var choice in masks)
        {
            if (choice == null) continue;

            bool isSelected = choice == selected;

            if (choice.clickImage != null)
                choice.clickImage.SetActive(isSelected);
        }
    }

    protected virtual void OnFeelingSelectedVisual(ChoiceItem selected)
    {
        var feelings = FeelingChoices;
        if (feelings == null) return;

        foreach (var choice in feelings)
        {
            if (choice == null) continue;

            bool isSelected = choice == selected;

            if (choice.clickImage != null)
                choice.clickImage.SetActive(isSelected);
        }
    }
}
