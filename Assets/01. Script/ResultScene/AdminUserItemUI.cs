using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminUserItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text emailText;
    public TMP_Text roleText;
    public TMP_Text activeText;

    public Button commentButton;   // ← 다시 살리기

    private UserSummary _user;

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
