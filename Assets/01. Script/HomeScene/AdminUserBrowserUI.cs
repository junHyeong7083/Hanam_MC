using System;
using TMPro;
using UnityEngine;

public class AdminUserBrowserUI : MonoBehaviour
{
    [Header("Search UI")]
    public TMP_InputField searchInput;      // 아래쪽 인풋

    [Header("List")]
    public Transform listRoot;              // ScrollView/Viewport/Content 등
    public GameObject itemPrefab;           // 단순 Text 프리팹 또는 AdminUserItemUI 달린 프리팹

    public event Action<string> OnQueryChanged;

    void Awake()
    {
        if (searchInput)
            searchInput.onValueChanged.AddListener(q => OnQueryChanged?.Invoke(q));
    }

    public void ClearList()
    {
        if (!listRoot) return;
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);
    }

    public void AddItem(UserSummary u)
    {
        if (!listRoot || !itemPrefab) return;
        var go = Instantiate(itemPrefab, listRoot);

        var item = go.GetComponent<AdminUserItemUI>();
        if (item) { item.Bind(u); return; }

        var txt = go.GetComponentInChildren<TMP_Text>();
        if (txt)
        {
            var currentEmail = SessionManager.Instance?.CurrentUser?.Email;
            bool isCurrent = !string.IsNullOrEmpty(currentEmail) && currentEmail == u.Email;
            string status = isCurrent ? "활성(현재 접속)" : (u.IsActive ? "오프라인" : "정지");

            txt.text = $"{u.Name}  ({u.Email})  [{u.Role}]  {status}";
        }
    }

}