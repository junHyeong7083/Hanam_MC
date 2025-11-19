using UnityEngine;

[RequireComponent(typeof(LoginFormUI))]
public class LoginController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] SceneNavigator navigator;              // Bootstrap 전역 네비게이터
    [Header("Tabs")]
    [SerializeField] RegisterTabsController tabs;           // RegisterScene의 탭 관리자
    [Header("Texts (Optional)")]
    [SerializeField] AuthUIText texts;

    private LoginFormUI view;
    private IAuthService auth;

    void Awake()
    {
        view = GetComponent<LoginFormUI>();
        auth = new AuthService(); // 나중에 RemoteAuthService로 교체 가능

        // 탭 전환: 회원가입 탭으로
        view.OnGoSignupRequested += HandleGoSignup;

        // 로그인 시도 (버튼은 항상 눌림)
        view.OnLoginRequested += HandleLogin;

        view.Show(texts ? texts.required : "필수 항목을 입력하세요.");
    }

    void OnDestroy()
    {
        if (!view) return;
        view.OnLoginRequested -= HandleLogin;
        view.OnGoSignupRequested -= HandleGoSignup;
    }

    void HandleGoSignup() => tabs?.ShowSignup();

    void HandleLogin(string email, string pw)
    {
        var res = auth.Login(email, pw);

        if (!res.Ok || res.Value == null)
        {
            view.Show(texts ? texts.loginFail : "이메일 또는 비밀번호가 올바르지 않습니다.");
            return;
        }

        if (SessionManager.Instance != null)
            SessionManager.Instance.SignIn(res.Value);

        var user = res.Value;
        if (navigator != null)
        {
            // ADMIN 이상 → ResultScene
            if (user.Role == UserRole.ADMIN || user.Role == UserRole.SUPERADMIN)
            {
                navigator.GoTo(ScreenId.RESULT);
            }
            // USER → HomeScene
            else
            {
                navigator.GoTo(ScreenId.HOME);
            }
        }
    }
}
