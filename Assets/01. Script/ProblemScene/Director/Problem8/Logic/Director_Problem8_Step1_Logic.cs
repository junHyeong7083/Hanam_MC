using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step1 로직 베이스
/// - 화면은 처음부터 전체 표시
/// - 스토리보드 클릭 → 3초 후 → Step2로 자동 전환
/// </summary>
public abstract class Director_Problem8_Step1_Logic : ProblemStepBase
{
    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("스토리보드 버튼")]
    protected abstract Button StoryboardButton { get; }

    [Header("완료 게이트")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual float DelayAfterClick => 3.0f;

    #endregion

    // 내부 상태
    private Coroutine _completeRoutine;
    private bool _clicked;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _clicked = false;

        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        RegisterListeners();

        // 인트로 시각 효과
        OnStepEnterVisual();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_completeRoutine != null)
        {
            StopCoroutine(_completeRoutine);
            _completeRoutine = null;
        }

        RemoveAllListeners();
    }

    // =========================
    // 리스너 등록
    // =========================

    private void RegisterListeners()
    {
        if (StoryboardButton != null)
        {
            StoryboardButton.onClick.RemoveAllListeners();
            StoryboardButton.onClick.AddListener(OnStoryboardClicked);
        }
    }

    private void RemoveAllListeners()
    {
        if (StoryboardButton != null)
            StoryboardButton.onClick.RemoveAllListeners();
    }

    // =========================
    // 버튼 핸들러
    // =========================

    private void OnStoryboardClicked()
    {
        if (_clicked) return;
        _clicked = true;

        // 버튼 비활성화 (중복 클릭 방지)
        if (StoryboardButton != null)
            StoryboardButton.interactable = false;

        // 클릭 시각 효과
        OnStoryboardClickedVisual();

        // 3초 후 완료
        if (_completeRoutine != null)
            StopCoroutine(_completeRoutine);
        _completeRoutine = StartCoroutine(CompleteAfterDelay());
    }

    // =========================
    // 코루틴
    // =========================

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(DelayAfterClick);

        // Gate 완료 → Step2로 자동 전환
        var gate = CompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();
    }

    // =========================
    // 시각 효과 (파생 클래스에서 override 가능)
    // =========================

    protected virtual void OnStepEnterVisual()
    {
        // 스텝 진입 시 효과 (파생 클래스에서 override)
    }

    protected virtual void OnStoryboardClickedVisual()
    {
        // 스토리보드 클릭 시 효과 (파생 클래스에서 override)
    }
}
