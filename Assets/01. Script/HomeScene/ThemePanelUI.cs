using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThemePanelUI : MonoBehaviour
{
    [Serializable]
    public class ProblemItemUI
    {
        [Tooltip("이 버튼이 담당하는 문제 번호 (1~10)")]
        public int index = 1;

        [Tooltip("해당 문제 버튼")]
        public Button button;

        [Tooltip("잠금 상태일 때 보여줄 자물쇠 이미지 오브젝트")]
        public GameObject lockIcon;

        [Tooltip("문제 번호나 제목 표시용(선택)")]
        public TMP_Text label;
    }

    [Header("문제 버튼들 (1~10)")]
    [SerializeField] ProblemItemUI[] items;

    // index: 1~10
    public event Action<int> OnProblemClicked;

    void Awake()
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (item == null || item.button == null) continue;

            int idx = item.index; // 클로저 캡쳐 방지
            item.button.onClick.AddListener(() => HandleClick(idx));
        }
    }

    void HandleClick(int index)
    {
        OnProblemClicked?.Invoke(index);
    }

    /// <summary>
    /// bool[] unlockedByIndex는 1 기반으로 사용 (0번은 무시)
    /// unlockedByIndex[i] == true 이면 i번 문제는 풀 수 있음
    /// </summary>
    public void ApplyUnlockState(bool[] unlockedByIndex)
    {
        if (items == null || unlockedByIndex == null) return;

        foreach (var item in items)
        {
            if (item == null) continue;

            int idx = item.index;
            bool unlocked = (idx >= 0 && idx < unlockedByIndex.Length) ? unlockedByIndex[idx] : false;

            if (item.button != null)
                item.button.interactable = unlocked;

            if (item.lockIcon != null)
            {
                //  잠금상태: lockIcon.SetActive(true)
                //  열려 있음: lockIcon.SetActive(false)
                item.lockIcon.SetActive(!unlocked);
            }

            if (item.label != null && string.IsNullOrEmpty(item.label.text))
                item.label.text = idx.ToString();
        }
    }

    public void SetAllInteractable(bool interactable)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (item?.button != null)
                item.button.interactable = interactable;
        }
    }
}
