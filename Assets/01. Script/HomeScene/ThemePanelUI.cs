using System;
using UnityEngine;
using UnityEngine.UI;

public class ThemePanelUI : MonoBehaviour
{
    [Serializable]
    public class ProblemItemUI
    {
        [Tooltip("이 버튼이 담당하는 문제 번호 (1~10)")]
        public int index = 1;

        [Tooltip("시작하기 버튼 (미완료 시 표시)")]
        public Button startButton;

        [Tooltip("잠금 상태일 때 보여줄 자물쇠 이미지 오브젝트")]
        public GameObject lockIcon;

        [Tooltip("완료 상태 루트 (별, 완료됨 텍스트, 체크 아이콘 포함)")]
        public GameObject completeRoot;
    }

    [Header("문제 카드들 (1~10)")]
    [SerializeField] ProblemItemUI[] items;

    [Header("하단 통계 패널")]
    [SerializeField] Text completedCountText;  // "1/10"
    [SerializeField] Text progressPercentText; // "10%"
    [SerializeField] Text rewardCountText;     // "1"

    // index: 1~10
    public event Action<int> OnProblemClicked;

    void Awake()
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (item == null || item.startButton == null) continue;

            int idx = item.index; // 클로저 캡쳐 방지
            item.startButton.onClick.AddListener(() => HandleClick(idx));
        }
    }

    void HandleClick(int index)
    {
        OnProblemClicked?.Invoke(index);
    }

    /// <summary>
    /// 문제별 상태 적용 (잠금/미완료/완료)
    /// unlockedByIndex[i] == true 이면 i번 문제는 풀 수 있음
    /// solvedByIndex[i] == true 이면 i번 문제는 이미 완료됨
    /// </summary>
    public void ApplyProblemState(bool[] unlockedByIndex, bool[] solvedByIndex)
    {
        if (items == null) return;

        int solvedCount = 0;

        foreach (var item in items)
        {
            if (item == null) continue;

            int idx = item.index;
            bool unlocked = (unlockedByIndex != null && idx >= 0 && idx < unlockedByIndex.Length)
                ? unlockedByIndex[idx] : false;
            bool solved = (solvedByIndex != null && idx >= 0 && idx < solvedByIndex.Length)
                ? solvedByIndex[idx] : false;

            if (solved) solvedCount++;

            // 시작하기 버튼: 미완료 + 언락 상태일 때만 표시
            if (item.startButton != null)
            {
                item.startButton.gameObject.SetActive(!solved && unlocked);
                item.startButton.interactable = unlocked && !solved;
            }

            // 잠금 아이콘: 미완료 + 잠김 상태일 때 표시
            if (item.lockIcon != null)
                item.lockIcon.SetActive(!solved && !unlocked);

            // 완료 상태 루트: 완료 시에만 표시
            if (item.completeRoot != null)
                item.completeRoot.SetActive(solved);
        }

        // 하단 통계 업데이트
        UpdateStats(solvedCount, items.Length);
    }

    /// <summary>
    /// 하단 통계 패널 업데이트
    /// </summary>
    void UpdateStats(int solvedCount, int totalCount)
    {
        if (completedCountText != null)
            completedCountText.text = $"{solvedCount}/{totalCount}";

        if (progressPercentText != null)
        {
            int percent = totalCount > 0 ? (solvedCount * 100 / totalCount) : 0;
            progressPercentText.text = $"{percent}%";
        }

        if (rewardCountText != null)
            rewardCountText.text = solvedCount.ToString();
    }

    /// <summary>
    /// 하위 호환용 - 기존 ApplyUnlockState 유지
    /// </summary>
    public void ApplyUnlockState(bool[] unlockedByIndex)
    {
        // 기존 코드 호환: solved 정보 없이 호출된 경우
        ApplyProblemState(unlockedByIndex, null);
    }

    public void SetAllInteractable(bool interactable)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (item?.startButton != null)
                item.startButton.interactable = interactable;
        }
    }
}
