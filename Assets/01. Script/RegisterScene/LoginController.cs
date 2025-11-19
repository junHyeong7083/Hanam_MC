using UnityEngine;

[RequireComponent(typeof(LoginFormUI))]
public class LoginController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] SceneNavigator navigator;          // 씬 이동
    [Header("Tabs")]
    [SerializeField] RegisterTabsController tabs;       // 로그인/회원가입 탭
    [Header("Texts (Optional)")]
    [SerializeField] AuthUIText texts;

    private LoginFormUI view;
    private IAuthService auth;

    void Awake()
    {
        view = GetComponent<LoginFormUI>();

        // 🔗 DataService에서 Auth 가져오기
        if (DataService.Instance != null && DataService.Instance.Auth != null)
        {
            auth = DataService.Instance.Auth;
        }
        else
        {
            Debug.LogWarning("[LoginController] DataService.Auth 없음, 임시 AuthService 생성");
            auth = new AuthService(new UserRepository());        
        }

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
        if (navigator != null)
        {
            if (user.Role >= UserRole.ADMIN)
                navigator.GoTo(ScreenId.RESULT);
            else
                navigator.GoTo(ScreenId.HOME);
        }
    }
}
