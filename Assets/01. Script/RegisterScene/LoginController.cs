using System;
using UnityEngine;

/// <summary>
/// LoginController - 로그인 폼의 비즈니스 로직을 담당하는 컨트롤러
///
/// 【역할】 LoginFormUI(View)에서 발행하는 이벤트를 수신하여:
///         1) IAuthService.Login()으로 인증 처리
///         2) 성공 시 SessionManager에 세션 저장 후 역할에 따라 씬 이동
///            - USER → HomeScene, ADMIN/SUPERADMIN → ResultScene
///         3) 실패 시 에러 메시지 표시
/// 【씬】 RegisterScene (로그인/회원가입 화면)
/// 【참조하는 곳】 RegisterScene의 로그인 패널에 부착 (LoginFormUI와 같은 GameObject)
/// 【참조되는 곳】 DataService.Auth (인증 서비스), SessionManager (세션 저장),
///               SceneNavigator (씬 이동), RegisterTabsController (탭 전환)
/// 【흐름】 사용자 입력 → LoginFormUI.OnLoginRequested → HandleLogin() → AuthService.Login()
///         → 성공: SessionManager.SignIn() → SceneNavigator.GoTo()
///         → 실패: 에러 메시지 표시
/// </summary>
[RequireComponent(typeof(LoginFormUI))]
public class LoginController : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] RegisterTabsController tabs;       // 로그인/회원가입 탭 전환 컨트롤러
    [Header("Texts (Optional)")]
    [SerializeField] AuthUIText texts;                  // UI 텍스트 리소스 (ScriptableObject, 선택 사항)

    private LoginFormUI view;   // 로그인 폼 UI (같은 GameObject에서 자동 참조)
    private IAuthService auth;  // 인증 서비스 인터페이스 (DataService에서 가져옴)

    void Awake()
    {
        view = GetComponent<LoginFormUI>();

        // DataService에서 Auth 가져오기 (유일한 진실 소스)
        if (DataService.Instance == null || DataService.Instance.Auth == null)
        {
            Debug.LogError("[LoginController] DataService.Auth 없음. DataService 세팅을 먼저 확인하세요.");
            enabled = false;
            return;
        }

        auth = DataService.Instance.Auth;

        view.OnGoSignupRequested += HandleGoSignup;
        view.OnLoginRequested += HandleLogin;

        if (texts != null)
            view.Show(texts.required);
        else
            view.Show("이메일과 비밀번호를 입력해주세요.");
    }

    void OnDestroy()
    {
        if (view == null) return;
        view.OnGoSignupRequested -= HandleGoSignup;
        view.OnLoginRequested -= HandleLogin;
    }

    void HandleGoSignup()
    {
        if (tabs != null)
            tabs.ShowSignup();
    }

    void HandleLogin(string email, string password)
    {
        if (auth == null)
        {
            Debug.LogError("[LoginController] auth 서비스가 null");
            return;
        }

        view.SetInteractable(false);
        view.Show(texts ? texts.loginInProgress : "로그인 중...");

        var res = auth.Login(email, password);
        if (!res.Ok || res.Value == null)
        {
            view.SetInteractable(true);
            view.Show(texts ? texts.loginFail : "아이디 또는 비밀번호가 올바르지 않습니다.");
            return;
        }

        var user = res.Value;

        // 세션 저장
        if (SessionManager.Instance != null)
            SessionManager.Instance.SignIn(user);

        // USER → HOME, ADMIN/SUPERADMIN → RESULT
        if (SceneNavigator.Instance != null)
        {
            if (user.Role >= UserRole.ADMIN)
                SceneNavigator.Instance.GoTo(ScreenId.RESULT);
            else
                SceneNavigator.Instance.GoTo(ScreenId.HOME);
        }
    }
}
