using System;
using System.Collections.Generic;
using UnityEngine;

public class StepFlowController : MonoBehaviour
{
    [Header("�� �帧���� ����� �г� ����")]
    [SerializeField] private List<GameObject> stepPanels = new List<GameObject>();

    [Header("Skip ���� (Intro �ǳʶٱ� ��)")]
    [Tooltip("Skip ��ư�� ������� ����")]
    [SerializeField] private bool useSkip = false;

    [Tooltip("Skip �� �̵��� step �ε��� (0 ���)")]
    [SerializeField] private int skipTargetStepIndex = 0;

    private int _currentIndex = -1;

    private void Awake()
    {
        SetAllInactive();
    }

    private void OnEnable()
    {
        if (stepPanels.Count > 0)
        {
            GoToStep(0); // �׻� 0�� ���ܺ��� ���� (��Ʈ�� �г� ��)
        }
        else
        {
            Debug.LogWarning($"[ProblemFlowController] {name} �� ��ϵ� �г��� �����ϴ�.");
        }
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

        SceneNavigator.Instance.GoTo(ScreenId.HOME);
    }

    protected virtual void OnFlowFinished()
    {
        Debug.Log($"[ProblemFlowController] ���� �帧 ����: {name}");
        // TODO: ���⼭ ���� "��� ���� �� ��� ȭ�� ��ȯ" ���� ��ó�� ���� ��.
    }
}
