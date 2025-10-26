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
    CompositionAdapter _adapter = new CompositionAdapter();

    void Update()
    {
        if (!searchInput) return;

        string composed = _adapter.GetComposedText(searchInput, Input.compositionString);

        if (!string.Equals(composed, _lastPushed, StringComparison.Ordinal))
        {
            _lastPushed = composed;
            OnQueryChanged?.Invoke(composed);
        }
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

    // ===== 내부 어댑터: TMP 버전별 프로퍼티 캡슐화 =====
    class CompositionAdapter
    {
        readonly PropertyInfo _pStringPos;
        readonly PropertyInfo _pStringSelPos;
        readonly PropertyInfo _pSelStrAnchor;
        readonly PropertyInfo _pSelStrFocus;
        readonly PropertyInfo _pCaretPos;
        readonly PropertyInfo _pSelAnchor;
        readonly PropertyInfo _pSelFocus;

        public CompositionAdapter()
        {
            var t = typeof(TMP_InputField);
            _pStringPos = t.GetProperty("stringPosition");
            _pStringSelPos = t.GetProperty("stringSelectPosition");
            _pSelStrAnchor = t.GetProperty("selectionStringAnchorPosition");
            _pSelStrFocus = t.GetProperty("selectionStringFocusPosition");
            _pCaretPos = t.GetProperty("caretPosition");
            _pSelAnchor = t.GetProperty("selectionAnchorPosition");
            _pSelFocus = t.GetProperty("selectionFocusPosition");
        }

        public string GetComposedText(TMP_InputField f, string composition)
        {
            var baseText = f.text ?? "";
            if (string.IsNullOrEmpty(composition)) return baseText;

            int caret = GetCaret(f);
            (int a, int b) = GetSelection(f);

            caret = Mathf.Clamp(caret, 0, baseText.Length);
            a = Mathf.Clamp(a, 0, baseText.Length);
            b = Mathf.Clamp(b, 0, baseText.Length);

            bool hasSel = b > a;
            if (hasSel)
            {
                string before = baseText.Substring(0, a);
                string after = baseText.Substring(b);
                return before + composition + after;
            }
            else
            {
                string before = baseText.Substring(0, caret);
                string after = baseText.Substring(caret);
                return before + composition + after;
            }
        }

        int GetCaret(TMP_InputField f)
        {
            try
            {
                if (_pStringPos != null) return (int)_pStringPos.GetValue(f);
                if (_pCaretPos != null) return (int)_pCaretPos.GetValue(f);
            }
            catch { }
            return (f.text ?? string.Empty).Length;
        }

        (int, int) GetSelection(TMP_InputField f)
        {
            try
            {
                if (_pStringPos != null && _pStringSelPos != null)
                {
                    int a = (int)_pStringPos.GetValue(f);
                    int b = (int)_pStringSelPos.GetValue(f);
                    return (Mathf.Min(a, b), Mathf.Max(a, b));
                }
                if (_pSelStrAnchor != null && _pSelStrFocus != null)
                {
                    int a = (int)_pSelStrAnchor.GetValue(f);
                    int b = (int)_pSelStrFocus.GetValue(f);
                    return (Mathf.Min(a, b), Mathf.Max(a, b));
                }
                if (_pSelAnchor != null && _pSelFocus != null)
                {
                    int a = (int)_pSelAnchor.GetValue(f);
                    int b = (int)_pSelFocus.GetValue(f);
                    return (Mathf.Min(a, b), Mathf.Max(a, b));
                }
            }
            catch { }
            int c = GetCaret(f);
            return (c, c);
        }
    }
}
