using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AdminUserItemUI - 관리자 사용자 목록의 개별 아이템 UI
///
/// 【역할】 사용자 목록에서 한 명의 사용자 정보(이름, 이메일, 역할, 상태)를 표시하고,
///         코멘트 버튼 클릭 시 AdminUserCommentPanel을 열어 해당 사용자의 상세 패널을 표시한다.
/// 【씬】 ResultScene (관리자 결과 조회 화면)
/// 【참조하는 곳】 AdminUserBrowserUI (프리팹 인스턴스화 후 Bind() 호출)
/// 【참조되는 곳】 AdminUserCommentPanel (코멘트 패널 열기)
/// 【흐름】 프리팹 인스턴스화 → Bind(UserSummary) → UI에 사용자 정보 표시 → 코멘트 버튼 클릭 → 코멘트 패널 열기
/// </summary>
public class AdminUserItemUI : MonoBehaviour
{
    public TMP_Text nameText;       // 사용자 이름 텍스트
    public TMP_Text emailText;      // 사용자 이메일 텍스트
    public TMP_Text roleText;       // 사용자 역할 텍스트 (USER/ADMIN/SUPERADMIN)
    public TMP_Text activeText;     // 사용자 상태 텍스트 (활성/오프라인/정지)

    public Button commentButton;    // 코멘트 버튼 (클릭 시 코멘트 패널 열기)

    private UserSummary _user;      // 이 아이템에 바인딩된 사용자 요약 정보

    void Awake()
    {
        // 혹시 인스펙터에서 안 넣어놨으면 자동으로 찾기
        if (!commentButton)
            commentButton = GetComponentInChildren<Button>();

        if (commentButton != null)
        {
            commentButton.onClick.RemoveAllListeners();
            commentButton.onClick.AddListener(OnClickComment);
        }
    }

    /// <summary>
    /// UserSummary 데이터를 UI에 바인딩한다.
    /// 이름, 이메일, 역할, 접속 상태를 각 텍스트에 표시한다.
    /// 현재 로그인한 관리자와 같은 이메일이면 "활성(현재 접속)"으로 표시한다.
    /// </summary>
    public void Bind(UserSummary u)
    {
        _user = u;   // ★ 반드시 가장 먼저!

        if (nameText) nameText.text = u.Name ?? "-";
        if (emailText) emailText.text = u.Email ?? "-";
        if (roleText) roleText.text = u.Role.ToString();

        var currentEmail = SessionManager.Instance?.CurrentUser?.Email;
        bool isCurrent = !string.IsNullOrEmpty(currentEmail) && currentEmail == u.Email;

        string status = isCurrent
            ? "활성(현재 접속)"
            : (u.IsActive ? "오프라인" : "정지");

        if (activeText) activeText.text = status;
        _user = u;
    }

    /// <summary>
    /// 코멘트 버튼 클릭 핸들러.
    /// AdminUserCommentPanel 싱글턴을 통해 해당 사용자의 코멘트 패널을 연다.
    /// </summary>
    public void OnClickComment()
    {
        Debug.Log($"[AdminUserItemUI] Click / _user = {(_user == null ? "NULL" : _user.Email)} / instanceID={GetInstanceID()}");

        if (_user == null) return;

        if (AdminUserCommentPanel.Instance != null)
        {
            AdminUserCommentPanel.Instance.Open(_user);
        }
        else
        {
            Debug.LogWarning("[AdminUserItemUI] AdminUserCommentPanel.Instance 가 없음");
        }
    }
}
