using System;
using System.Collections.Generic;
using UnityEngine;

public class ProblemFlowController : MonoBehaviour
{
    [Header("이 문제에서 사용할 패널 순서")]
    [SerializeField] private List<GameObject> stepPanels = new List<GameObject>();

    private int _currentIndex = -1;

    private void Awake()
    {
        // 처음엔 모두 비활성화
        SetAllInactive();
    }

    private void OnEnable()
    {
        // ProblemSceneController가 이 Problem_X를 활성화했을 때
        // 처음 패널부터 시작
        if (stepPanels.Count > 0)
        {
            GoToStep(0);
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
            // 마지막 스텝을 넘겼을 때: 여기서 "문제 종료" 처리 훅
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
        if (prev < 0) prev = 0; // 필요하면 그냥 무시해도 됨
        GoToStep(prev);
    }

    // 필요하면 외부에서 특정 index로 점프할 때 사용
    public void JumpToStep(int index)
    {
        GoToStep(index);
    }

    protected virtual void OnFlowFinished()
    {
        Debug.Log($"[ProblemFlowController] 문제 흐름 종료: {name}");
        // TODO:
        // - 여기서 "답안 제출" 버튼이 있는 최종 패널에서 NextStep을 호출하게 해놓고,
        //   이 안에서 답안 검증 → Attempt/Result 저장 로직을 호출하도록 확장
    }
}
