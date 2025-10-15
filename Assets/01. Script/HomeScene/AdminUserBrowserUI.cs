using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public class AdminUserBrowserUI : MonoBehaviour
{
    [Header("Search UI")]
    public TMP_InputField searchInput;

    [Header("List")]
    public Transform listRoot;
    public GameObject itemPrefab;

    public event Action<string> OnQueryChanged;

    string _lastPushed = "";

    void Awake()
    {
        // 기존 onValueChanged는 유지해도 되지만,
        // 우리는 매 프레임 조합 문자열까지 포함한 값을 직접 푸시할 거라 빈 리스너로 둬도 무방.
        if (searchInput)
            searchInput.onValueChanged.AddListener(_ => { /* Update()에서 처리 */ });
    }
    void Update()
    {
        if (!searchInput) return;

        string baseText = searchInput.text ?? "";
        string comp = Input.compositionString;

        string composed = baseText;

        if (!string.IsNullOrEmpty(comp))
        {
            // ▼ 버전별로 다른 캐럿/선택 프로퍼티를 안전하게 얻기
            int caret = GetStringCaretPosition(searchInput);
            (int selAnchor, int selFocus) = GetStringSelectionRange(searchInput);

            caret = Mathf.Clamp(caret, 0, baseText.Length);

            bool hasSelection = selFocus > selAnchor;
            if (hasSelection)
            {
                selAnchor = Mathf.Clamp(selAnchor, 0, baseText.Length);
                selFocus = Mathf.Clamp(selFocus, 0, baseText.Length);

                string before = baseText.Substring(0, selAnchor);
                string after = baseText.Substring(selFocus);
                composed = before + comp + after;
            }
            else
            {
                string before = baseText.Substring(0, caret);
                string after = baseText.Substring(caret);
                composed = before + comp + after;
            }
        }

        if (!string.Equals(composed, _lastPushed, StringComparison.Ordinal))
        {
            _lastPushed = composed;
            OnQueryChanged?.Invoke(composed);
        }
    }

    // ===== 버전 호환 보조 메서드 =====

    // stringPosition 또는 caretPosition 중 사용 가능 항목 반환
    static int GetStringCaretPosition(TMP_InputField f)
    {
        // stringPosition (신버전)
        var prop = typeof(TMP_InputField).GetProperty("stringPosition", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
        {
            try { return (int)prop.GetValue(f); } catch { /* fallthrough */ }
        }

        // caretPosition (구버전 대체)
        var propCaret = typeof(TMP_InputField).GetProperty("caretPosition", BindingFlags.Instance | BindingFlags.Public);
        if (propCaret != null)
        {
            try { return (int)propCaret.GetValue(f); } catch { /* fallthrough */ }
        }

        // 최후의 수단
        return (f.text ?? string.Empty).Length;
    }

    // stringSelectPosition 또는 selectionStringAnchor/Focus(신버전) → selectionAnchor/Focus(구버전) 폴백
    static (int anchor, int focus) GetStringSelectionRange(TMP_InputField f)
    {
        // 1) stringSelectPosition + stringPosition 조합이 있으면 그걸로 계산
        var propSelect = typeof(TMP_InputField).GetProperty("stringSelectPosition", BindingFlags.Instance | BindingFlags.Public);
        var propPos = typeof(TMP_InputField).GetProperty("stringPosition", BindingFlags.Instance | BindingFlags.Public);
        if (propSelect != null && propPos != null)
        {
            try
            {
                int a = (int)propPos.GetValue(f);
                int b = (int)propSelect.GetValue(f);
                return (Mathf.Min(a, b), Mathf.Max(a, b));
            }
            catch { /* fallthrough */ }
        }

        // 2) selectionStringAnchorPosition / selectionStringFocusPosition (많이 쓰이는 버전)
        var propAnchorStr = typeof(TMP_InputField).GetProperty("selectionStringAnchorPosition", BindingFlags.Instance | BindingFlags.Public);
        var propFocusStr = typeof(TMP_InputField).GetProperty("selectionStringFocusPosition", BindingFlags.Instance | BindingFlags.Public);
        if (propAnchorStr != null && propFocusStr != null)
        {
            try
            {
                int a = (int)propAnchorStr.GetValue(f);
                int b = (int)propFocusStr.GetValue(f);
                return (Mathf.Min(a, b), Mathf.Max(a, b));
            }
            catch { /* fallthrough */ }
        }

        // 3) selectionAnchorPosition / selectionFocusPosition (더 구버전)
        var propAnchor = typeof(TMP_InputField).GetProperty("selectionAnchorPosition", BindingFlags.Instance | BindingFlags.Public);
        var propFocus = typeof(TMP_InputField).GetProperty("selectionFocusPosition", BindingFlags.Instance | BindingFlags.Public);
        if (propAnchor != null && propFocus != null)
        {
            try
            {
                int a = (int)propAnchor.GetValue(f);
                int b = (int)propFocus.GetValue(f);
                return (Mathf.Min(a, b), Mathf.Max(a, b));
            }
            catch { /* fallthrough */ }
        }

        // 선택 없음으로 처리
        int caret = GetStringCaretPosition(f);
        return (caret, caret);
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
