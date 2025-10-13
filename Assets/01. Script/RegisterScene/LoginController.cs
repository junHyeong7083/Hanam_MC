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
        auth = new AuthService();

        // 탭 전환: 회원가입 탭으로
        view.OnGoSignupRequested += () => tabs?.ShowSignup();

        // 로그인 시도 (버튼은 항상 눌림)
        view.OnLoginRequested += HandleLogin;
        view.Show(texts ? texts.required : "필수 항목을 입력하세요.");
    }

    void OnDestroy()
    {
        if (!view) return;
        view.OnLoginRequested -= HandleLogin;
        view.OnGoSignupRequested -= () => tabs?.ShowSignup();
    }

    void HandleLogin(string email, string pw)
    {
        // 검증 실패도 messageText로만 표시 (버튼 비활성화 X)
        var res = auth.Login(email, pw);
        if (!res.Ok || res.Value == null)
        {
            // 실패 원인에 따라 메시지(원하면 switch로 더 세분화)
            view.Show(texts ? texts.loginFail : "이메일 또는 비밀번호가 올바르지 않습니다.");
            return;
        }

        // 성공
        view.Show(texts ? texts.loginDone : "로그인 성공");
        if (navigator) navigator.IsAuthenticated = true;
        navigator?.GoTo(ScreenId.HOME);
    }
}
