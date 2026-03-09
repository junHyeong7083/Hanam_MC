using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 순서대로 카드를 하나씩 처리하는 공통 Step 베이스.
/// - 카드 표시/처리는 자식에서 구현
/// - 현재 카드 인덱스, 순서(_order) 관리는 여기서 담당
/// - 카드 1장 처리 완료 시 completionGate에 1 추가
/// - progressLabel이 있으면 "현재/전체" 형식으로 표시
///
/// 자식 클래스에서 아래를 구현하면 된다:
/// - CardCount, OnApplyCardToUI, OnClearCurrentCardUI,
///   OnCardProcessed, OnAllCardsProcessed
/// </summary>
public abstract class RandomCardSequenceStepBase : ProblemStepBase
{
    [Header("진행도 표시 (옵션)")]
    [SerializeField] private Text progressLabel;

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    /// <summary>전체 카드 개수 (자식이 구현)</summary>
    protected abstract int CardCount { get; }

    /// <summary>logicalIndex 번째 카드를 UI에 반영할 때 호출</summary>
    protected abstract void OnApplyCardToUI(int logicalIndex);

    /// <summary>현재 카드를 UI에서 제거할 때 호출</summary>
    protected abstract void OnClearCurrentCardUI();

    /// <summary>카드 한 장의 처리가 완료되었을 때(정답/분류 등 처리 후) 호출</summary>
    protected abstract void OnCardProcessed(int logicalIndex);

    /// <summary>모든 카드를 처리하고 난 뒤 호출</summary>
    protected abstract void OnAllCardsProcessed();

    /// <summary>자식이 초기화 추가 작업 필요하면 오버라이드</summary>
    protected virtual void OnSequenceReset() { }

    // 내부 상태
    protected int _currentIndex;     // 0..CardCount
    protected int[] _order;          // 표시 순서 (logical index 배열)

    protected override void OnStepEnter()
    {
        int total = CardCount;
        BuildOrder(total);
        _currentIndex = 0;

        // 게이트 초기화
        if (completionGate != null)
            completionGate.ResetGate(total);

        OnSequenceReset();
        UpdateCurrentCardUI();
        UpdateProgressLabel();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
    }

    /// <summary>현재 logical index 가져오기 (끝나면 -1)</summary>
    protected int GetCurrentLogicalIndex()
    {
        if (_order == null) return -1;
        if (_currentIndex < 0 || _currentIndex >= _order.Length) return -1;
        return _order[_currentIndex];
    }

    /// <summary>현재 카드 UI 갱신 (있으면 Apply, 없으면 Clear)</summary>
    protected void UpdateCurrentCardUI()
    {
        int idx = GetCurrentLogicalIndex();
        bool isComplete = (idx < 0);

        if (isComplete)
        {
            OnClearCurrentCardUI();
        }
        else
        {
            OnApplyCardToUI(idx);
        }
    }

    /// <summary>현재/전체 진행도 텍스트 갱신</summary>
    protected void UpdateProgressLabel()
    {
        if (progressLabel == null) return;

        int total = CardCount;
        int current = Mathf.Clamp(_currentIndex, 0, total);
        progressLabel.text = $"{current}/{total}";
    }

    /// <summary>현재 카드를 성공 처리했다고 알리는 함수.
    /// - completionGate 1 추가
    /// - _currentIndex++ 후 다음 카드 표시 or 전체 완료 처리
    /// </summary>
    protected void CompleteCurrentCard()
    {
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0)
        {
            Debug.LogWarning("[RandomCardSequenceStepBase] CompleteCurrentCard 호출 시점에 logicalIndex<0");
            return;
        }

        // 자식에게 카드 처리(로그 기록 등)
        OnCardProcessed(logicalIndex);

        // 게이트 1 추가
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 다음 카드로 이동
        _currentIndex++;

        if (_currentIndex >= CardCount)
        {
            // 전체 완료됨
            OnClearCurrentCardUI();
            UpdateProgressLabel();
            OnAllCardsProcessed();
        }
        else
        {
            UpdateCurrentCardUI();
            UpdateProgressLabel();
        }
    }

    private void BuildOrder(int total)
    {
        _order = new int[total];
        for (int i = 0; i < total; i++)
            _order[i] = i;
    }
}
