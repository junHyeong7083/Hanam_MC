using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step2 로직 베이스
/// - "보여지는 나 vs 진짜 나" 가면 선택
/// - 4단계: intro → selectMask → selectFeeling → reveal
/// </summary>
public abstract class Director_Problem7_Step2_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    [Serializable]
    public class ChoiceItem
    {
        public string id;       // DB 저장용 ID (예: "cool", "anxious")
        public string label;    // 표시용 라벨 (예: "쿨한 척", "사실은 불안했어")
        public Button button;   // 버튼 참조
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

    protected enum Phase { Intro, SelectMask, SelectFeeling }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("Intro 화면")]
    protected abstract GameObject IntroRoot { get; }
    protected abstract Button IntroNextButton { get; }

    [Header("가면 선택 화면")]
    protected abstract GameObject SelectMaskRoot { get; }
    protected abstract ChoiceItem[] MaskChoices { get; }

    [Header("진짜 마음 선택 화면")]
    protected abstract GameObject SelectFeelingRoot { get; }
    protected abstract ChoiceItem[] FeelingChoices { get; }

    [Header("완료 게이트 (CompleteRoot에 Reveal 화면 연결)")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual float MaskSelectDelay => 0.8f;
    protected virtual float FeelingSelectDelay => 1.0f;

    #endregion

    // 내부 상태
    private Phase _currentPhase;
    private ChoiceItem _selectedMask;
    private ChoiceItem _selectedFeeling;
    private Coroutine _transitionRoutine;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentPhase = Phase.Intro;
        _selectedMask = null;
        _selectedFeeling = null;

        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        SetupAllPhases();
        ShowPhase(Phase.Intro);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

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
        // 모든 화면 숨기기
        if (IntroRoot != null) IntroRoot.SetActive(false);
        if (SelectMaskRoot != null) SelectMaskRoot.SetActive(false);
        if (SelectFeelingRoot != null) SelectFeelingRoot.SetActive(false);

        // 버튼 리스너 등록
        RegisterListeners();
    }

    private void RegisterListeners()
    {
        // Intro 버튼
        if (IntroNextButton != null)
        {
            IntroNextButton.onClick.RemoveAllListeners();
            IntroNextButton.onClick.AddListener(OnIntroNextClicked);
        }

        // 가면 버튼들
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

        // 감정 버튼들
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
        if (IntroNextButton != null)
            IntroNextButton.onClick.RemoveAllListeners();

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

        if (IntroRoot != null) IntroRoot.SetActive(phase == Phase.Intro);
        if (SelectMaskRoot != null) SelectMaskRoot.SetActive(phase == Phase.SelectMask);
        if (SelectFeelingRoot != null) SelectFeelingRoot.SetActive(phase == Phase.SelectFeeling);
        // Reveal은 CompletionGate의 CompleteRoot로 자동 표시됨
    }

    // =========================
    // 버튼 핸들러
    // =========================

    private void OnIntroNextClicked()
    {
        ShowPhase(Phase.SelectMask);
    }

    private void OnMaskSelected(ChoiceItem choice)
    {
        if (_currentPhase != Phase.SelectMask) return;
        if (_selectedMask != null) return; // 이미 선택됨

        _selectedMask = choice;

        // 선택 시각 효과 (파생 클래스에서 추가 가능)
        OnMaskSelectedVisual(choice);

        // 딜레이 후 다음 Phase로
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionAfterDelay(Phase.SelectFeeling, MaskSelectDelay));
    }

    private void OnFeelingSelected(ChoiceItem choice)
    {
        if (_currentPhase != Phase.SelectFeeling) return;
        if (_selectedFeeling != null) return; // 이미 선택됨

        _selectedFeeling = choice;

        // 선택 시각 효과
        OnFeelingSelectedVisual(choice);

        // Attempt 저장
        var body = new MaskFeelingAttemptDto
        {
            mask = new SelectedChoiceDto
            {
                id = _selectedMask?.id,
                label = _selectedMask?.label
            },
            feeling = new SelectedChoiceDto
            {
                id = _selectedFeeling?.id,
                label = _selectedFeeling?.label
            }
        };
        SaveAttempt(body);

        // 딜레이 후 MarkOneDone → CompleteRoot(Reveal) 표시
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(CompleteAfterDelay(FeelingSelectDelay));
    }

    private IEnumerator CompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // SelectFeeling 패널 숨기기
        if (SelectFeelingRoot != null)
            SelectFeelingRoot.SetActive(false);

        // Gate 완료 → CompleteRoot(Reveal 패널) 표시
        var gate = CompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();
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
        // 기본: 선택된 버튼 하이라이트, 나머지 비활성화
        var masks = MaskChoices;
        if (masks == null) return;

        foreach (var choice in masks)
        {
            if (choice?.button != null)
                choice.button.interactable = false;
        }
    }

    protected virtual void OnFeelingSelectedVisual(ChoiceItem selected)
    {
        // 기본: 선택된 버튼 하이라이트, 나머지 비활성화
        var feelings = FeelingChoices;
        if (feelings == null) return;

        foreach (var choice in feelings)
        {
            if (choice?.button != null)
                choice.button.interactable = false;
        }
    }
}
