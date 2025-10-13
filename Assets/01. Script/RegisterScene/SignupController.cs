using UnityEngine;

[RequireComponent(typeof(SignupFormUI))]
public class SignupController : MonoBehaviour
{
    SignupFormUI view;
    AuthService auth;
    void Awake()
    {
        view = GetComponent<SignupFormUI>();
        auth = new AuthService();

        view.OnCheckEmailRequested += HandleCheckEmail;
        view.OnSignupRequested += HandleSignup;
        view.OnCancelRequested += HandleCancel;

        // 실시간 힌트 갱신
        view.OnPasswordChanged += HandlePasswordChanged;
        view.OnConfirmChanged += HandleConfirmChanged;

        view.SetSubmitInteractable(false);
        view.Show("필수 항목을 입력하세요.");
    }

    void OnDestroy()
    {
        view.OnCheckEmailRequested -= HandleCheckEmail;
        view.OnSignupRequested -= HandleSignup;
        view.OnCancelRequested -= HandleCancel;
        view.OnPasswordChanged -= HandlePasswordChanged;
        view.OnConfirmChanged -= HandleConfirmChanged;
    }

    // 이메일 체크 버튼
    void HandleCheckEmail(string email)
    {
        email = (email ?? "").Trim().ToLower();

        if (!AuthValidator.IsValidEmail(email))
        {
            view.SetEmailHint("이메일 형식 오류", false);
            view.SetSubmitInteractable(false);
            return;
        }

        bool exists = auth.Exists(email);
        if (exists)
        {
            view.SetEmailHint("이미 사용 중인 이메일입니다.", false);
            view.SetSubmitInteractable(false);
        }
        else
        {
            view.SetEmailHint("사용 가능한 이메일입니다.", true);
            // 제출은 비번/확인 통과 후 결정
        }
    }

    // 가입 버튼
    void HandleSignup(string name, string email, string pw)
    {
        name = (name ?? "").Trim();
        email = (email ?? "").Trim().ToLower();

        if (string.IsNullOrWhiteSpace(name)) { view.Show("이름을 입력하세요."); return; }
        if (!AuthValidator.IsValidEmail(email)) { view.SetEmailHint("이메일 형식 오류", false); view.Show("이메일을 확인하세요."); return; }
        if (auth.Exists(email)) { view.SetEmailHint("이미 사용 중", false); view.Show("다른 이메일을 입력하세요."); return; }
        view.SetEmailHint("OK", true);

        if (!AuthValidator.IsStrongPassword(pw)) { view.SetPasswordHint("최소 8자, 문자+숫자 포함", false); view.Show("비밀번호 규칙을 확인하세요."); return; }
        view.SetPasswordHint("안전한 비밀번호", true);

        if (view.CurrentConfirm != pw) { view.SetConfirmHint("불일치", false); view.Show("비밀번호 확인이 일치하지 않습니다."); return; }
        view.SetConfirmHint("일치", true);

        view.SetSubmitInteractable(true);

        bool ok = auth.SignUp(name, email, pw);
        if (!ok) { view.Show("가입 실패(중복/정책 위반). 다시 시도하세요."); return; }

        var user = auth.Login(email, pw);
        if (user == null) { view.Show("자동 로그인 실패"); return; }

        view.Show("가입 완료");
        // ScreenManager.GoTo(ScreenID.HOME);
    }

    // 비밀번호 입력 즉시 힌트 반영
    void HandlePasswordChanged(string pw)
    {
        if (string.IsNullOrEmpty(pw))
        {
            view.SetPasswordHint("", false);
            return;
        }

        if (AuthValidator.IsStrongPassword(pw))
            view.SetPasswordHint("안전한 비밀번호", true);
        else
        {
            // 상세 이유 안내(가벼운 UX)
            var msg = pw.Length < 8 ? "8자 이상 필요" : "문자+숫자 포함";
            view.SetPasswordHint(msg, false);
        }

        // 확인 입력과의 일치 여부도 즉시 반영
        HandleConfirmChanged(view.CurrentConfirm);
    }

    // 비밀번호 확인 입력 즉시 힌트 반영
    void HandleConfirmChanged(string confirm)
    {
        if (string.IsNullOrEmpty(confirm))
        {
            view.SetConfirmHint("", false);
            return;
        }
        bool ok = confirm == view.CurrentPass;
        view.SetConfirmHint(ok ? "일치" : "불일치", ok);
    }

    void HandleCancel()
    {
        // ScreenManager.GoTo(ScreenID.LOGIN);
        view.Clear();
    }
}
