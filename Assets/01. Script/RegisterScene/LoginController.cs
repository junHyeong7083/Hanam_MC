using UnityEngine;
public class LoginController : MonoBehaviour
{
    LoginFormUI view;
    AuthService auth;
    private void Awake()
    {
        view = GetComponent<LoginFormUI>();
        auth = new AuthService();

        view.OnLoginRequested += HandleLogin;
      //  view.OnGoSignupRequested += () => screenm
    }


    void HandleLogin(string email, string password)
    {
        view.SetInteractable(false);

        var user = auth.Login(email, password);
        if(user == null)
        {
            view.Show("이메일 또는 비밀번호가 올바르지 않습니다.");
            view.SetInteractable(true);
            return;
        }
        view.Show("로그인 성공");

       // ScreenManager.GoTo(ScreenID.HOME);
    }
}
