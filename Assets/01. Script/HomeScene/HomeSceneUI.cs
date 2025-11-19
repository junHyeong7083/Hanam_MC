using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeSceneUI : MonoBehaviour
{
    [Header("User Info")]
    [SerializeField] Text welcomeText;   // "OOO님, 안녕하세요" 같은 문구
    [SerializeField] Text nameText;      // 이름만 따로 표시하고 싶을 때
    [SerializeField] Text roleText;      // 역할(USER / ADMIN / SUPERADMIN)

    [Header("Buttons")]
    [SerializeField] Button startProblemButton;   // 문제풀이 시작 버튼
    [SerializeField] Button historyButton;       // 과거 기록 보기(나중에 쓰일 수 있음)
    [SerializeField] Button logoutButton;        // 로그아웃

    // 컨트롤러에서 구독할 이벤트
    public event Action OnStartProblemRequested;
    public event Action OnHistoryRequested;
    public event Action OnLogoutRequested;
    public event Action OnAdminPanelRequested;

    void Awake()
    {
        if (startProblemButton) startProblemButton.onClick.AddListener(ClickStartProblem);
        if (historyButton) historyButton.onClick.AddListener(ClickHistory);
        if (logoutButton) logoutButton.onClick.AddListener(ClickLogout);

    }

    // ── 버튼 콜백 ───────────────────────────────────────────────

    public void ClickStartProblem() => OnStartProblemRequested?.Invoke();
    public void ClickHistory() => OnHistoryRequested?.Invoke();
    public void ClickLogout() => OnLogoutRequested?.Invoke();
    public void ClickAdminPanel() => OnAdminPanelRequested?.Invoke();

    // ── 표시용 메서드 ───────────────────────────────────────────

    public void BindUser(User user)
    {
        if (user == null)
        {
            SetWelcomeText("로그인 정보를 불러올 수 없습니다.");
            SetNameText("");
            SetRoleText("");
            return;
        }

        // 이름/역할 텍스트 설정
        SetNameText(user.Name);
        SetRoleText(GetRoleKor(user.Role));

        // 환영 문구
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


    // 필요하면 나중에 로딩 중 상태 같은 거 막을 때 사용
    public void SetInteractable(bool on)
    {
        if (startProblemButton) startProblemButton.interactable = on;
        if (historyButton) historyButton.interactable = on;
        if (logoutButton) logoutButton.interactable = on;
    }

    // ── 내부 유틸 ───────────────────────────────────────────────

    string GetRoleKor(UserRole role)
    {
        switch (role)
        {
            case UserRole.SUPERADMIN: return "최고관리자";
            case UserRole.ADMIN: return "관리자";
            case UserRole.USER:
            default: return "사용자";
        }
    }

    public void MoveProblemScene()
    {

    }
}
