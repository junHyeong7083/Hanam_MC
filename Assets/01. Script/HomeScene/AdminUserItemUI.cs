using TMPro;
using UnityEngine;

public class AdminUserItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text emailText;
    public TMP_Text roleText;
    public TMP_Text activeText;

    public void Bind(UserSummary u)
    {
        if (nameText) nameText.text = u.Name ?? "-";
        if (emailText) emailText.text = u.Email ?? "-";
        if (roleText) roleText.text = u.Role.ToString();


        var currentEmail = SessionManager.Instance?.CurrentUser?.Email;
        bool isCurrent = !string.IsNullOrEmpty(currentEmail) && currentEmail == u.Email;

        // isCurrent: 지금 로그인한 사용자
        // u.IsActive: 계정 사용 가능 여부(정지/비활성 아님)
        string status = isCurrent
            ? "활성(현재 접속)"
            : (u.IsActive ? "오프라인" : "정지");

        if (activeText) activeText.text = status;
    }
}