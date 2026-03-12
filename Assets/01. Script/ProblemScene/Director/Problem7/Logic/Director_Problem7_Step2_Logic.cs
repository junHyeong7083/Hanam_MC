using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem7_Step2_Logic - 문제7 스텝2 가면/감정 선택 로직 (추상 클래스)
///
/// 【역할】 "보여지는 나 vs 진짜 나" 테마에서 2단계 선택을 처리한다.
///          Phase1: 가면(외면) 선택 → Phase2: 진짜 감정 선택 → DB 저장 → 완료
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. SerializeField는 Binder(Director_Problem7_Step2)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제7 > 스텝2 (메인 활동 - 가면/감정 선택)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem7_Step2 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, ProblemRuntime
/// 【흐름】 스텝 진입 → 대화 재생 → 가면 4개 중 1개 선택 → 딜레이 후 감정 선택 화면 전환
///         → 감정 4개 중 1개 선택 → DB 저장 → 딜레이 후 완료 텍스트 표시
/// </summary>
public abstract class Director_Problem7_Step2_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    /// <summary>
    /// ChoiceItem - 가면/감정 선택지 한 개의 데이터와 UI 참조를 묶는 직렬화 가능 구조체.
    /// 가면 선택과 감정 선택 모두에서 동일한 구조로 사용된다.
    /// </summary>
    [Serializable]
    public class ChoiceItem
    {
        public string id;           // DB 저장용 식별자 (예: "cool", "anxious")
        public int labelTextId;     // CSV textId (버튼 라벨 표시용)
        public Button button;       // 선택지 버튼 참조
        public GameObject clickImage;  // 선택 시 활성화할 시각적 표시 이미지
    }

    // =========================
    // DB 저장용 DTO (Data Transfer Object)
    // =========================

    /// <summary>선택된 항목의 ID와 라벨을 담는 DTO</summary>
    [Serializable]
    private class SelectedChoiceDto
    {
        public string id;     // 선택된 항목의 식별자
        public string label;  // 선택된 항목의 표시 텍스트
    }

    /// <summary>가면 + 감정 선택 결과를 하나로 묶어 DB에 저장하는 DTO</summary>
    [Serializable]
    private class MaskFeelingAttemptDto
    {
        public SelectedChoiceDto mask;     // 선택된 가면 정보
        public SelectedChoiceDto feeling;  // 선택된 감정 정보
    }

    /// <summary>현재 페이즈를 나타내는 열거형 (가면 선택 / 감정 선택)</summary>
    protected enum Phase { SelectMask, SelectFeeling }

    // =========================
    // 파생 클래스(Binder)에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>가면 선택 완료 후 감정 선택 화면 전환 시 표시할 안내 텍스트의 CSV textId</summary>
    [Header("페이즈 전환 텍스트")]
    protected abstract int MaskSelectedTextId { get; }

    [Header("가면 선택 화면")]
    /// <summary>가면 선택 UI 루트 (Phase1에서 표시)</summary>
    protected abstract GameObject SelectMaskRoot { get; }
    /// <summary>가면 선택지 배열 (4개)</summary>
    protected abstract ChoiceItem[] MaskChoices { get; }

    [Header("진짜 마음 선택 화면")]
    /// <summary>감정 선택 UI 루트 (Phase2에서 표시)</summary>
    protected abstract GameObject SelectFeelingRoot { get; }
    /// <summary>감정 선택지 배열 (4개)</summary>
    protected abstract ChoiceItem[] FeelingChoices { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서 (진입/완료/페이즈 전환 대사)

    #endregion

    #region Virtual Config

    /// <summary>가면 선택 후 감정 선택 화면으로 전환하기까지의 딜레이 (초)</summary>
    protected virtual float MaskSelectDelay => 2.0f;
    /// <summary>감정 선택 후 완료 처리까지의 딜레이 (초)</summary>
    protected virtual float FeelingSelectDelay => 2.0f;

    #endregion

    // ===== 내부 상태 =====
    private Phase _currentPhase;               // 현재 페이즈 (가면 선택 / 감정 선택)
    private ChoiceItem _selectedMask;          // 선택된 가면 항목 (null이면 미선택)
    private ChoiceItem _selectedFeeling;       // 선택된 감정 항목 (null이면 미선택)
    private Coroutine _transitionRoutine;      // 페이즈 전환 딜레이 코루틴 핸들
    private bool _interactionLocked = true;    // 대화 재생 중 상호작용 잠금 플래그

    // =========================
    // ProblemStepBase 생명주기 구현
    // =========================

    /// <summary>
    /// 스텝 진입 시 호출. 상태 초기화, 모든 페이즈 UI 세팅, 가면 선택 화면 표시.
    /// 대화 재생이 끝날 때까지 상호작용을 잠근다.
    /// </summary>
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

    /// <summary>대화 진입 완료 시 상호작용 잠금을 해제한다.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>
    /// 스텝 퇴장 시 호출. 이벤트 구독 해제, 전환 코루틴 정지, 버튼 리스너 정리.
    /// </summary>
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

    /// <summary>모든 페이즈 루트를 숨기고, clickImage를 리셋하며, 버튼 리스너를 등록한다.</summary>
    private void SetupAllPhases()
    {
        if (SelectMaskRoot != null) SelectMaskRoot.SetActive(false);
        if (SelectFeelingRoot != null) SelectFeelingRoot.SetActive(false);

        ResetClickImages(MaskChoices);
        ResetClickImages(FeelingChoices);

        RegisterListeners();
    }

    /// <summary>선택지 배열의 모든 clickImage를 비활성화한다.</summary>
    private void ResetClickImages(ChoiceItem[] choices)
    {
        if (choices == null) return;
        foreach (var choice in choices)
        {
            if (choice?.clickImage != null)
                choice.clickImage.SetActive(false);
        }
    }

    /// <summary>가면/감정 선택지 모두에 CSV 텍스트를 적용한다.</summary>
    private void ApplyLabelsFromTextId()
    {
        ApplyLabels(MaskChoices);
        ApplyLabels(FeelingChoices);
    }

    /// <summary>선택지 배열의 각 버튼 하위 Text 컴포넌트에 CSV 텍스트를 설정한다.</summary>
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

    /// <summary>가면/감정 선택지 버튼에 클릭 리스너를 등록한다.</summary>
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

    /// <summary>가면/감정 선택지 버튼의 모든 클릭 리스너를 제거한다.</summary>
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

    /// <summary>
    /// 지정된 페이즈로 전환한다. 해당 페이즈의 UI 루트만 활성화하고,
    /// 감정 선택 페이즈 진입 시 DialogueSequencer에 안내 텍스트를 설정한다.
    /// </summary>
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

    /// <summary>
    /// 가면(외면) 항목 클릭 시 호출. 선택 시각 효과를 적용하고
    /// MaskSelectDelay 후 감정 선택 페이즈로 전환한다.
    /// </summary>
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

    /// <summary>
    /// 진짜 감정 항목 클릭 시 호출. 선택 시각 효과를 적용하고
    /// 가면+감정 선택 결과를 DB에 저장한 뒤, FeelingSelectDelay 후 완료 처리한다.
    /// </summary>
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

    /// <summary>지정된 딜레이 후 완료 텍스트를 표시하는 코루틴.</summary>
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

    /// <summary>지정된 딜레이 후 다음 페이즈로 전환하는 코루틴.</summary>
    private IEnumerator TransitionAfterDelay(Phase nextPhase, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPhase(nextPhase);
    }

    // =========================
    // 시각 효과 (파생 클래스에서 override 가능)
    // =========================

    /// <summary>
    /// 가면 선택 시 시각 효과. 선택된 항목의 clickImage만 활성화하고 나머지는 비활성화.
    /// 파생 클래스에서 override하여 커스텀 이펙트를 적용할 수 있다.
    /// </summary>
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

    /// <summary>
    /// 감정 선택 시 시각 효과. 선택된 항목의 clickImage만 활성화하고 나머지는 비활성화.
    /// 파생 클래스에서 override하여 커스텀 이펙트를 적용할 수 있다.
    /// </summary>
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
