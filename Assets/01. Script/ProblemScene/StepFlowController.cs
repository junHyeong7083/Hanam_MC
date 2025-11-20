using System;
using System.Collections.Generic;
using UnityEngine;

public class StepFlowController : MonoBehaviour   
{
    [Header("이 흐름에서 사용할 패널 순서")]
    [SerializeField] private List<GameObject> stepPanels = new List<GameObject>();

    [Header("Skip 설정 (Intro 건너뛰기 등)")]
    [Tooltip("Skip 버튼을 사용할지 여부")]
    [SerializeField] private bool useSkip = false;

    [Tooltip("Skip 시 이동할 step 인덱스 (0 기반)")]
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
            GoToStep(0); // 항상 0번 스텝부터 시작 (인트로 패널 등)
        }
        else
        {
            Debug.LogWarning($"[ProblemFlowController] {name} 에 등록된 패널이 없습니다.");
        }
    }

    private void SetAllInactive()
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
            Debug.LogError($"[ProblemFlowController] 잘못된 step index: {index}");
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
    /// Intro 패널 등에서 "건너뛰기" 눌렀을 때 호출할 함수
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
            Debug.LogWarning("[ProblemFlowController] stepPanels 비어 있음. Skip 불가.");
            return;
        }

        int target = skipTargetStepIndex;

        // 범위 보정
        if (target < 0) target = 0;
        if (target >= stepPanels.Count) target = stepPanels.Count - 1;

        GoToStep(target);
    }

    protected virtual void OnFlowFinished()
    {
        Debug.Log($"[ProblemFlowController] 문제 흐름 종료: {name}");
        // TODO: 여기서 이후 "답안 제출 후 결과 화면 전환" 같은 후처리 들어가면 됨.
    }
}
