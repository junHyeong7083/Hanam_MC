using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StepFlowController - 스텝(패널) 순차 진행을 관리하는 핵심 컨트롤러
///
/// 【역할】 stepPanels 리스트에 등록된 GameObject(각 스텝 패널)를 순서대로 활성화/비활성화한다.
///          NextStep(), PrevStep(), JumpToStep(), RestartCurrentStep() 등으로 스텝 간 이동을 제어하며,
///          BGM 재생/정지, 건너뛰기(Skip), 문제 완료 처리(ProblemEnd)도 담당한다.
/// 【참조하는 곳】 StepCompletionGate (완료 시 NextStep 호출), AutoNextStepButton (리플렉션으로 NextStep 호출),
///                CommonRewardStep (SaveAndNextStep → NextStep), DialogueSequencer (NextStepBtn → NextStep),
///                StartStep (시작 버튼 → NextStep), ProblemSceneController (문제 패널 관리),
///                DebugStepController (디버그 스텝 이동), 각 Problem Director Logic
/// 【참조되는 곳】 SoundManager (BGM), DataService (문제 완료 저장), SessionManager (사용자 조회),
///                ProblemSession, SceneNavigator, ProblemStepBase (상위 참조)
/// 【흐름】 OnEnable() → SetAllInactive() → GoToStep(0) → 사용자 상호작용 → NextStep() → ... → ProblemEnd()
///
/// ※ 각 스텝 패널에는 ProblemStepBase 파생 컴포넌트가 붙어있고,
///    GoToStep()으로 패널이 SetActive(true)되면 OnEnable → OnStepEnter()가 자동 호출된다.
/// </summary>
public class StepFlowController : MonoBehaviour
{
    [SerializeField] private List<GameObject> stepPanels = new List<GameObject>(); // 스텝 패널 리스트 (순서대로 인덱스 0, 1, 2...)
    [SerializeField] private bool useSkip = false;           // 건너뛰기 기능 사용 여부 (Intro 화면 등에서 사용)
    [SerializeField] private int skipTargetStepIndex = 0;    // 건너뛰기 시 이동할 스텝 인덱스

    [Header("BGM (비어있으면 재생 안 함)")]
    [Tooltip("Resources/BGM/ 하위 클립명 (예: BGM_C01_S06)")]
    [SerializeField] private string bgmClipName;     // 이 문제에서 재생할 BGM 클립 이름
    [SerializeField] private bool stopBgmOnExit = true; // OnDisable 시 BGM 정지 여부

    private int _currentIndex = -1; // 현재 활성화된 스텝의 인덱스 (-1이면 아직 시작 전)

    /// <summary>
    /// 활성화 시 모든 패널을 끄고 BGM을 재생한 뒤 첫 번째 스텝으로 이동한다.
    /// ProblemSceneController가 Problem_N을 활성화하면 이 OnEnable이 호출되어 스텝 흐름이 시작된다.
    /// </summary>
    private void OnEnable()
    {
        SetAllInactive();

        // BGM 재생 (bgmClipName이 설정된 경우)
        if (!string.IsNullOrEmpty(bgmClipName) && SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(bgmClipName);

        if (stepPanels.Count > 0)
        {
            GoToStep(0); // 첫 번째 스텝부터 시작
        }
        else
        {
            Debug.LogWarning($"[ProblemFlowController] {name} 에 할당된 패널이 없습니다.");
        }
    }

    /// <summary>비활성화 시 BGM 정지 (stopBgmOnExit가 true인 경우)</summary>
    private void OnDisable()
    {
        if (stopBgmOnExit && !string.IsNullOrEmpty(bgmClipName) && SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    /// <summary>모든 스텝 패널을 비활성화한다.</summary>
    public void SetAllInactive()
    {
        foreach (var p in stepPanels)
        {
            if (p != null) p.SetActive(false);
        }
    }

    /// <summary>
    /// 지정 인덱스의 스텝 패널만 활성화하고 나머지는 모두 비활성화한다.
    /// 패널이 SetActive(true)되면 해당 패널의 ProblemStepBase.OnEnable() → OnStepEnter()가 호출된다.
    /// 패널이 SetActive(false)되면 ProblemStepBase.OnDisable() → OnStepExit()가 호출된다.
    /// </summary>
    /// <param name="index">활성화할 스텝 인덱스 (0-based)</param>
    private void GoToStep(int index)
    {
        if (stepPanels == null || stepPanels.Count == 0) return;
        if (index < 0 || index >= stepPanels.Count)
        {
            Debug.LogError($"[ProblemFlowController] 잘못된 step index: {index}");
            return;
        }

        _currentIndex = index;

        // 현재 인덱스에 해당하는 패널만 켜고, 나머지는 모두 끈다
        for (int i = 0; i < stepPanels.Count; i++)
        {
            bool active = (i == _currentIndex);
            if (stepPanels[i] != null)
                stepPanels[i].SetActive(active);
        }
    }

    /// <summary>
    /// 다음 스텝으로 이동한다. 마지막 스텝을 넘어가면 OnFlowFinished()를 호출한다.
    /// StepCompletionGate, AutoNextStepButton 등에서 호출된다.
    /// </summary>
    public void NextStep()
    {
        if (stepPanels == null || stepPanels.Count == 0) return;

        int next = _currentIndex + 1;
        if (next >= stepPanels.Count)
        {
            OnFlowFinished();
        }
        else
        {
            GoToStep(next);
        }
    }

    /// <summary>이전 스텝으로 이동한다. 첫 번째 스텝 이전으로는 갈 수 없다.</summary>
    public void PrevStep()
    {
        if (stepPanels == null || stepPanels.Count == 0) return;

        int prev = _currentIndex - 1;
        if (prev < 0) prev = 0;
        GoToStep(prev);
    }

    /// <summary>
    /// 지정된 인덱스의 스텝으로 직접 이동한다. (순서 무시)
    /// </summary>
    /// <param name="index">이동할 스텝 인덱스 (0-based)</param>
    public void JumpToStep(int index)
    {
        GoToStep(index);
    }

    /// <summary>
    /// 현재 스텝을 재시작 (OnDisable → OnEnable 재호출)
    /// </summary>
    public void RestartCurrentStep()
    {
        if (_currentIndex < 0 || stepPanels == null || stepPanels.Count == 0) return;
        GoToStep(_currentIndex);
    }

    /// <summary>
    /// Intro 패널 위의 "건너뛰기" 버튼에서 호출되는 함수
    /// </summary>
    public void SkipFlow()
    {
        if (!useSkip)
        {
            Debug.LogWarning($"[ProblemFlowController] {name} 에서 useSkip=false 인데 SkipFlow가 호출되었습니다.");
            return;
        }

        if (stepPanels == null || stepPanels.Count == 0)
        {
            Debug.LogWarning("[ProblemFlowController] stepPanels 할당 없음. Skip 불가.");
            return;
        }

        int target = skipTargetStepIndex;

        // 범위 보정
        if (target < 0) target = 0;
        if (target >= stepPanels.Count) target = stepPanels.Count - 1;

        GoToStep(target);
    }

    /// <summary>
    /// 문제 전체 완료 처리. DB에 문제 풀이 완료를 기록하고 홈 화면으로 전환한다.
    /// Director 테마에서 마지막 문제(P10) 완료 시 EndingPanel로, 그 외에는 LevelSelectPanel로 복귀한다.
    /// </summary>
    public void ProblemEnd()
    {
        var ds = DataService.Instance;
        var user = SessionManager.Instance?.CurrentUser;

        // DB에 문제 완료(Solved) 상태 저장
        if (ds != null && ds.Progress != null && user != null)
        {
            var theme = ProblemSession.CurrentTheme;
            var index = ProblemSession.CurrentProblemIndex;

            var res = ds.Progress.MarkProblemSolvedForCurrentUser(theme, index);
            if (!res.Ok)
            {
                Debug.LogWarning($"[StepFlow] MarkProblemSolved 실패: {res.Error}");
            }
        }
        else
        {
            Debug.LogWarning("[StepFlow] 문제 완료 저장 실패 - 세션 또는 DataService.Progress 없음");
        }

        // Director 테마: LevelSelectPanel 또는 EndingPanel로 복귀
        if (ProblemSession.CurrentTheme == ProblemTheme.Director)
        {
            ProblemSession.ReturnTarget = ProblemSession.CurrentProblemIndex >= 10
                ? HomeReturnTarget.Ending
                : HomeReturnTarget.LevelSelect;
        }

        SceneNavigator.Instance.GoTo(ScreenId.HOME);
    }

    /// <summary>
    /// 전체 스텝 흐름이 완료되었을 때 호출된다.
    /// 기본 구현은 로그만 출력하며, 파생 클래스에서 override하여 후처리를 추가할 수 있다.
    /// </summary>
    protected virtual void OnFlowFinished()
    {
        Debug.Log($"[ProblemFlowController] 전체 흐름 완료: {name}");
        // TODO: 여기서 추후 "모든 스텝 완료 후 홈 화면 전환" 등의 후처리 추가 가능.
    }
}
