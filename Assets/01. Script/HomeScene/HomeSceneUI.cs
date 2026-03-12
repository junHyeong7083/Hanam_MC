using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HomeSceneUI - 홈 화면의 사용자 정보 표시 및 UI 이벤트 발행
///
/// 【역할】 로그인한 사용자의 이름, 환영 메시지, 역할(일반회원/관리자/최고관리자)을 표시하고,
///         로그아웃/관리자 패널 요청 이벤트를 외부(Controller)에 발행한다.
/// 【씬】 HomeScene (LevelSelectScene)
/// 【참조하는 곳】 HomeScene 내 Controller(이벤트 구독하여 처리)
/// 【참조되는 곳】 User 모델 (사용자 정보 바인딩)
/// 【흐름】 BindUser(user) 호출 → 사용자 정보 UI에 표시 / 버튼 클릭 → OnLogoutRequested 이벤트 발행
/// </summary>
public class HomeSceneUI : MonoBehaviour
{
    [Header("User Info")]
    [SerializeField] Text welcomeText;    // "OOO님, 안녕하세요" 환영 텍스트
    [SerializeField] Text nameText;       // 사용자 이름 표시
    [SerializeField] Text roleText;       // 사용자 역할 표시 (일반회원/관리자/최고관리자)

    [Header("Buttons")]
    [SerializeField] Button logoutButton; // 로그아웃 버튼

    /// <summary>로그아웃 버튼 클릭 시 발행되는 이벤트</summary>
    public event Action OnLogoutRequested;
    /// <summary>관리자 패널 요청 시 발행되는 이벤트</summary>
    public event Action OnAdminPanelRequested;

    void Awake()
    {
        if (logoutButton) logoutButton.onClick.AddListener(ClickLogout);
    }

    /// <summary>로그아웃 버튼 클릭 핸들러 - OnLogoutRequested 이벤트 발행</summary>
    public void ClickLogout() => OnLogoutRequested?.Invoke();
    /// <summary>관리자 패널 버튼 클릭 핸들러 - OnAdminPanelRequested 이벤트 발행</summary>
    public void ClickAdminPanel() => OnAdminPanelRequested?.Invoke();

    /// <summary>
    /// User 객체를 바인딩하여 환영 메시지, 이름, 역할을 UI에 표시한다.
    /// user가 null이면 "로그인 정보를 불러올 수 없습니다." 메시지를 표시한다.
    /// </summary>
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

    /// <summary>환영 메시지 텍스트 설정</summary>
    public void SetWelcomeText(string text)
    {
        if (welcomeText) welcomeText.text = text ?? "";
    }

    /// <summary>사용자 이름 텍스트 설정</summary>
    public void SetNameText(string text)
    {
        if (nameText) nameText.text = text ?? "";
    }

    /// <summary>사용자 역할 텍스트 설정</summary>
    public void SetRoleText(string text)
    {
        if (roleText) roleText.text = text ?? "";
    }

    /// <summary>로그아웃 버튼의 상호작용 가능 여부 설정</summary>
    public void SetInteractable(bool on)
    {
        if (logoutButton) logoutButton.interactable = on;
    }

    /// <summary>
    /// UserRole 열거형을 한글 문자열로 변환한다.
    /// SUPERADMIN → "최고관리자", ADMIN → "관리자", USER → "일반회원"
    /// </summary>
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
