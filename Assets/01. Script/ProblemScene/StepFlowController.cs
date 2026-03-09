using System;
using System.Collections.Generic;
using UnityEngine;

public class StepFlowController : MonoBehaviour
{
    [SerializeField] private List<GameObject> stepPanels = new List<GameObject>();
    [SerializeField] private bool useSkip = false;
    [SerializeField] private int skipTargetStepIndex = 0;

    [Header("BGM (비어있으면 재생 안 함)")]
    [Tooltip("Resources/BGM/ 하위 클립명 (예: BGM_C01_S06)")]
    [SerializeField] private string bgmClipName;
    [SerializeField] private bool stopBgmOnExit = true;

    private int _currentIndex = -1;

    private void Awake()
    {
        SetAllInactive();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(bgmClipName) && SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(bgmClipName);

        if (stepPanels.Count > 0)
        {
            GoToStep(0);
        }
        else
        {
            Debug.LogWarning($"[ProblemFlowController] {name} 에 할당된 패널이 없습니다.");
        }
    }

    private void OnDisable()
    {
        if (stopBgmOnExit && !string.IsNullOrEmpty(bgmClipName) && SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    public void SetAllInactive()
    {
        foreach (var p in stepPanels)
        {
            if (p != null) p.SetActive(false);
        }
    }

    private void GoToStep(int index)
    {
        if (stepPanels == null || stepPanels.Count == 0) return;
        if (index < 0 || index >= stepPanels.Count)
        {
            Debug.LogError($"[ProblemFlowController] �߸��� step index: {index}");
            return;
        }

        _currentIndex = index;

        for (int i = 0; i < stepPanels.Count; i++)
        {
            bool active = (i == _currentIndex);
            if (stepPanels[i] != null)
                stepPanels[i].SetActive(active);
        }
    }

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

    public void PrevStep()
    {
        if (stepPanels == null || stepPanels.Count == 0) return;

        int prev = _currentIndex - 1;
        if (prev < 0) prev = 0;
        GoToStep(prev);
    }

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
    /// Intro �г� ��� "�ǳʶٱ�" ������ �� ȣ���� �Լ�
    /// </summary>
    public void SkipFlow()
    {
        if (!useSkip)
        {
            Debug.LogWarning($"[ProblemFlowController] {name} ���� useSkip=false �ε� SkipFlow�� ȣ��Ǿ����ϴ�.");
            return;
        }

        if (stepPanels == null || stepPanels.Count == 0)
        {
            Debug.LogWarning("[ProblemFlowController] stepPanels ��� ����. Skip �Ұ�.");
            return;
        }

        int target = skipTargetStepIndex;

        // ���� ����
        if (target < 0) target = 0;
        if (target >= stepPanels.Count) target = stepPanels.Count - 1;

        GoToStep(target);
    }

    public void ProblemEnd()
    {
        var ds = DataService.Instance;
        var user = SessionManager.Instance?.CurrentUser;

        if (ds != null && ds.Progress != null && user != null)
        {
            var theme = ProblemSession.CurrentTheme;
            var index = ProblemSession.CurrentProblemIndex;

            var res = ds.Progress.MarkProblemSolvedForCurrentUser(theme, index);
            if (!res.Ok)
            {
                Debug.LogWarning($"[StepFlow] MarkProblemSolved ����: {res.Error}");
            }
        }
        else
        {
            Debug.LogWarning("[StepFlow] ���൵ ���� ���� - ���� �Ǵ� DataService.Progress ����");
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

    protected virtual void OnFlowFinished()
    {
        Debug.Log($"[ProblemFlowController] ���� �帧 ����: {name}");
        // TODO: ���⼭ ���� "��� ���� �� ��� ȭ�� ��ȯ" ���� ��ó�� ���� ��.
    }
}
