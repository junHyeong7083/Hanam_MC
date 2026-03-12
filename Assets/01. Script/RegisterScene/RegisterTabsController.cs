using UnityEngine;

/// <summary>로그인/회원가입 탭 구분 열거형</summary>
public enum RegisterTab { Login, Signup }

/// <summary>
/// RegisterTabsController - 로그인/회원가입 탭 전환 컨트롤러
///
/// 【역할】 RegisterScene에서 로그인 패널과 회원가입 패널 간의 탭 전환을 관리한다.
///         한쪽 패널을 활성화하면 다른 쪽은 비활성화된다.
/// 【씬】 RegisterScene (로그인/회원가입 화면)
/// 【참조하는 곳】 LoginController, SignupController (탭 전환 요청 시)
/// 【참조되는 곳】 없음 (외부에서 ShowLogin() / ShowSignup() 호출)
/// 【흐름】 초기화 시 defaultTab에 따라 표시 → ShowLogin() / ShowSignup()으로 탭 전환
/// </summary>
public class RegisterTabsController : MonoBehaviour
{
    [SerializeField] GameObject loginPanel;    // 로그인 패널 루트 오브젝트
    [SerializeField] GameObject signupPanel;   // 회원가입 패널 루트 오브젝트

    [Header("Initial Tab")]
    [SerializeField] RegisterTab defaultTab = RegisterTab.Login;  // 시작 시 표시할 기본 탭

    void Awake()
    {
        // �ʱ� ���� ���⼭ '�� ����' ����
        if (defaultTab == RegisterTab.Login) ShowLogin();
        else ShowSignup();
    }

    /// <summary>로그인 패널을 표시하고 회원가입 패널을 숨긴다</summary>
    public void ShowLogin()
    {
        Debug.Log("[Tabs] ShowLogin");
        if (loginPanel) loginPanel.SetActive(true);
        if (signupPanel) signupPanel.SetActive(false);
    }

    /// <summary>회원가입 패널을 표시하고 로그인 패널을 숨긴다</summary>
    public void ShowSignup()
    {
        Debug.Log("[Tabs] ShowSignup");
        if (loginPanel) loginPanel.SetActive(false);
        if (signupPanel) signupPanel.SetActive(true);
    }
}
