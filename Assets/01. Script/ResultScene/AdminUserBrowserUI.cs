using System;
using System.Reflection;
using TMPro;
using UnityEngine;

/// <summary>
/// AdminUserBrowserUI - 관리자용 사용자 목록 브라우저의 UI 레이어 (View)
///
/// 【역할】 검색 입력 필드의 텍스트 변화를 감지하여 OnQueryChanged 이벤트를 발행하고,
///         사용자 아이템 목록의 생성/삭제를 관리한다.
///         한글 조합 중(compositionString) 실시간 검색을 지원하기 위해
///         CompositionAdapter를 사용하여 조합 문자열을 포함한 전체 텍스트를 추출한다.
/// 【씬】 ResultScene (관리자 결과 조회 화면)
/// 【참조하는 곳】 AdminUserBrowserController (이벤트 구독 및 아이템 추가)
/// 【참조되는 곳】 AdminUserItemUI (아이템 프리팹의 컴포넌트)
/// 【흐름】 검색 입력 → Update()에서 조합 텍스트 감지 → OnQueryChanged 발행 → Controller에서 처리
/// </summary>
public class AdminUserBrowserUI : MonoBehaviour
{
    [Header("Search UI")]
    public TMP_InputField searchInput;    // 검색 입력 필드

    [Header("List")]
    public Transform listRoot;            // 사용자 아이템이 생성될 부모 Transform
    public GameObject itemPrefab;         // 사용자 아이템 프리팹 (AdminUserItemUI 포함)

    /// <summary>검색 텍스트 변화 시 발행되는 이벤트. 매개변수: 조합 문자열 포함 전체 텍스트</summary>
    public event Action<string> OnQueryChanged;

    private string _lastPushed = "";                              // 마지막으로 발행한 검색 텍스트 (중복 발행 방지)
    private CompositionAdapter _adapter = new CompositionAdapter(); // 한글 조합 문자열 처리 어댑터

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

    /// <summary>사용자 목록의 모든 아이템을 삭제한다</summary>
    public void ClearList()
    {
        if (!listRoot) return;
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);
    }

    /// <summary>
    /// 사용자 아이템을 목록에 추가한다.
    /// itemPrefab에 AdminUserItemUI가 있으면 Bind()로 데이터를 설정하고,
    /// 없으면 TMP_Text에 직접 텍스트를 설정한다.
    /// </summary>
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
    /// <summary>
    /// TMP_InputField의 캐럿/선택 위치 프로퍼티가 TMP 버전마다 다르므로
    /// Reflection으로 안전하게 접근하는 어댑터.
    /// 한글 IME 조합 중인 문자열을 InputField.text에 삽입하여 실시간 검색을 지원한다.
    /// </summary>
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
