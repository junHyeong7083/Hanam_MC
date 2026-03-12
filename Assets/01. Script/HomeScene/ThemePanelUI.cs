using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ThemePanelUI - 특정 테마(Director/Garden)의 문제 목록 패널 UI
///
/// 【역할】 테마별 문제 카드(1~10번) 목록을 표시하고, 각 문제의 잠금/미완료/완료 상태를 UI에 반영한다.
///         하단에 완료 수, 진행률(%), 보상 수 등의 통계를 표시한다.
/// 【씬】 HomeScene (LevelSelectScene)
/// 【참조하는 곳】 ThemePanelsController (테마 선택 시 이 패널을 활성화하고 상태를 갱신)
/// 【참조되는 곳】 없음 (이벤트 OnProblemClicked를 통해 외부에 알림)
/// 【흐름】 ApplyProblemState() 호출 → 각 카드의 잠금/시작/완료 상태 UI 반영 → 버튼 클릭 → OnProblemClicked 이벤트
/// </summary>
public class ThemePanelUI : MonoBehaviour
{
    /// <summary>
    /// 개별 문제 카드의 UI 요소를 묶은 직렬화 가능 클래스.
    /// 인스펙터에서 문제 번호, 시작 버튼, 잠금 아이콘, 완료 상태 루트를 설정한다.
    /// </summary>
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
    [SerializeField] ProblemItemUI[] items;           // 문제별 UI 아이템 배열

    [Header("하단 통계 패널")]
    [SerializeField] Text completedCountText;         // 완료 수 표시 텍스트 (예: "1/10")
    [SerializeField] Text progressPercentText;        // 진행률 표시 텍스트 (예: "10%")
    [SerializeField] Text rewardCountText;            // 보상 수 표시 텍스트 (예: "1")

    /// <summary>문제 카드 클릭 시 발행되는 이벤트. 매개변수: 문제 번호(1~10)</summary>
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

            // 시작하기 버튼
            if (item.startButton != null)
            {
                // TODO: 개발 완료 후 아래 주석을 해제하고, 임시 코드를 제거할 것
                // [원본] 이전 문제 재플레이 방지 — 미완료 + 언락 상태일 때만 표시
                // item.startButton.gameObject.SetActive(!solved && unlocked);
                // item.startButton.interactable = unlocked && !solved;

                // [임시] 개발 기간 중: 풀었던 문제도 다시 플레이 가능
                item.startButton.gameObject.SetActive(unlocked || solved);
                item.startButton.interactable = unlocked || solved;
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

    /// <summary>
    /// 모든 문제 카드의 시작 버튼 상호작용 가능 여부를 일괄 설정한다.
    /// </summary>
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
