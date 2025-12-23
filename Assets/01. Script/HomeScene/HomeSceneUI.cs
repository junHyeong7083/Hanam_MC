using System;
using UnityEngine;
using UnityEngine.UI;

public class HomeSceneUI : MonoBehaviour
{
    [Header("User Info")]
    [SerializeField] Text welcomeText;
    [SerializeField] Text nameText;
    [SerializeField] Text roleText;

    [Header("Buttons")]
    [SerializeField] Button logoutButton;

    public event Action OnLogoutRequested;
    public event Action OnAdminPanelRequested;

    void Awake()
    {
        if (logoutButton) logoutButton.onClick.AddListener(ClickLogout);
    }

    public void ClickLogout() => OnLogoutRequested?.Invoke();
    public void ClickAdminPanel() => OnAdminPanelRequested?.Invoke();

    public void BindUser(User user)
    {
        if (user == null)
        {
            SetWelcomeText("로그인 정보를 불러올 수 없습니다.");
            SetNameText("");
            SetRoleText("");
            return;
        }

        SetNameText(user.Name);
        SetRoleText(GetRoleKor(user.Role));

        if (!string.IsNullOrEmpty(user.Name))
            SetWelcomeText($"{user.Name}님, 안녕하세요");
        else
            SetWelcomeText("안녕하세요");
    }

    public void SetWelcomeText(string text)
    {
        if (welcomeText) welcomeText.text = text ?? "";
    }

    public void SetNameText(string text)
    {
        if (nameText) nameText.text = text ?? "";
    }

    public void SetRoleText(string text)
    {
        if (roleText) roleText.text = text ?? "";
    }

    public void SetInteractable(bool on)
    {
        if (logoutButton) logoutButton.interactable = on;
    }

    string GetRoleKor(UserRole role)
    {
        switch (role)
        {
            case UserRole.SUPERADMIN: return "최고관리자";
            case UserRole.ADMIN: return "관리자";
            case UserRole.USER:
            default: return "일반회원";
        }
    }
}
