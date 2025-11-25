using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "여러 장의 카드"를 랜덤 순서로 하나씩 처리하는 공통 Step 베이스.
/// - 카드 개수/내용은 자식에서 정의
/// - 현재 카드 인덱스, 랜덤 순서(_order) 등은 여기서 관리
/// - 카드 1장 처리 완료 시 completionGate에 1 증가
/// - progressLabel이 있으면 "현재/전체" 형식으로 표시
/// 
/// 자식 클래스는 다음만 구현하면 된다:
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

    /// <summary>logicalIndex 번째 카드를 UI에 보여줄 때 호출</summary>
    protected abstract void OnApplyCardToUI(int logicalIndex);

    /// <summary>현재 카드를 UI에서 제거할 때 호출</summary>
    protected abstract void OnClearCurrentCardUI();

    /// <summary>카드 한 장이 완료되었을 때(정답/분류 등 처리 후) 호출</summary>
    protected abstract void OnCardProcessed(int logicalIndex);

    /// <summary>모든 카드를 처리하고 난 뒤 호출</summary>
    protected abstract void OnAllCardsProcessed();

    /// <summary>자식이 초기화 추가 작업 필요하면 오버라이드</summary>
    protected virtual void OnSequenceReset() { }

    // 내부 상태
    protected int _currentIndex;     // 0..CardCount
    protected int[] _order;          // 랜덤 순서 (logical index 배열)

    protected override void OnStepEnter()
    {
        int total = CardCount;
        BuildRandomOrder(total);
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

    /// <summary>현재 logical index 가져오기 (없으면 -1)</summary>
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

    /// <summary>현재 카드를 모두 처리했다고 알리는 함수.
    /// - completionGate 1 증가
    /// - _currentIndex++ 후 다음 카드 적용 or 전체 완료 처리
    /// </summary>
    protected void CompleteCurrentCard()
    {
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0)
        {
            Debug.LogWarning("[RandomCardSequenceStepBase] CompleteCurrentCard 호출 시점에 logicalIndex<0");
            return;
        }

        // 자식에서 카드 처리(로그 기록 등)
        OnCardProcessed(logicalIndex);

        // 게이트 1 증가
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 다음 카드로 진행
        _currentIndex++;

        if (_currentIndex >= CardCount)
        {
            // 전부 완료됨
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

    private void BuildRandomOrder(int total)
    {
        _order = new int[total];
        for (int i = 0; i < total; i++)
            _order[i] = i;

        // Fisher-Yates 셔플
        for (int i = total - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }
    }
}
