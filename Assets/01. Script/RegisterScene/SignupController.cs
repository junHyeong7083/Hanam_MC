using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SignupFormUI))]
public class SignupController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] SceneNavigator navigator;              // Bootstrap 전역 네비게이터
    [Header("Tabs")]
    [SerializeField] RegisterTabsController tabs;           // RegisterScene의 탭 관리자
    [Header("Texts (Optional)")]
    [SerializeField] AuthUIText texts;

    private SignupFormUI view;
    private IAuthService auth;

    private Coroutine emailCheckCo;
    private const float EmailDebounceSec = 0.25f;

    void Awake()
    {
        view = GetComponent<SignupFormUI>();
        auth = new AuthService();

        view.OnCheckEmailRequested += HandleCheckEmail;
        view.OnSignupRequested += HandleSignup;
        view.OnCancelRequested += () => tabs?.ShowLogin();

        // 실시간 힌트(UX)
        view.OnPasswordChanged += HandlePasswordChanged;
        view.OnConfirmChanged += HandleConfirmChanged;

        // 버튼은 항상 눌리게 두고, 메시지로만 실패 안내
        view.Show(texts ? texts.required : "필수 항목을 입력하세요.");
    }

    void OnDestroy()
    {
        if (!view) return;
        view.OnCheckEmailRequested -= HandleCheckEmail;
        view.OnSignupRequested -= HandleSignup;
        view.OnCancelRequested -= () => tabs?.ShowLogin();
        view.OnPasswordChanged -= HandlePasswordChanged;
        view.OnConfirmChanged -= HandleConfirmChanged;
    }

    // ── 이메일 중복 확인(디바운스) ────────────────────────────
    void HandleCheckEmail(string email)
    {
        if (emailCheckCo != null) StopCoroutine(emailCheckCo);
        emailCheckCo = StartCoroutine(CoCheckEmailDebounced(email));
    }

    IEnumerator CoCheckEmailDebounced(string email)
    {
        yield return new WaitForSeconds(EmailDebounceSec);

        var r = auth.Exists(email);
        if (!r.Ok && r.Error == AuthError.EmailInvalid)
        {
            var msg = texts ? texts.emailFormatError : "이메일 형식 오류";
            view.SetEmailHint(msg, false);
            view.Show(msg);
            yield break;
        }

        if (!r.Ok && r.Error == AuthError.Internal)
        {
            view.SetEmailHint("잠시 후 다시 시도", false);
            view.Show("잠시 후 다시 시도");
            yield break;
        }

        if (r.Value)
        {
            var msg = texts ? texts.emailDuplicate : "이미 사용 중인 이메일입니다.";
            view.SetEmailHint(msg, false);
            view.Show(msg);
        }
        else
        {
            var msg = texts ? texts.emailAvailable : "사용 가능한 이메일입니다.";
            view.SetEmailHint(msg, true);
            view.Show(msg);
        }
    }

    // ── 회원가입 처리: 버튼은 항상 눌리지만, 실패는 messageText로만 안내 ──
    void HandleSignup(string name, string email, string pw)
    {
        // UX 프리체크(서버측 최종 검증은 AuthService에서 다시 수행)
        if (string.IsNullOrWhiteSpace(name))
        { view.Show(texts ? texts.nameEmpty : "이름을 입력하세요."); return; }

        if (!AuthValidator.IsValidEmail(email))
        { view.Show(texts ? texts.emailFormatError : "이메일 형식 오류"); return; }

        if (!AuthValidator.IsStrongPassword(pw))
        { view.Show(texts ? texts.pwWeak : "최소 8자, 문자+숫자 포함"); return; }

        if (view.CurrentConfirm != pw)
        { view.Show(texts ? texts.pwConfirmMismatch : "비밀번호 확인이 일치하지 않습니다."); return; }

        // 최종 서버측 정책/중복 포함 SignUp
        var res = auth.SignUp(name, email, pw);
        if (!res.Ok)
        {
            switch (res.Error)
            {
                case AuthError.NameEmpty: view.Show(texts ? texts.nameEmpty : "이름을 입력하세요."); break;
                case AuthError.EmailInvalid: view.Show(texts ? texts.emailFormatError : "이메일 형식 오류"); break;
                case AuthError.EmailDuplicate: view.Show(texts ? texts.emailDuplicate : "이미 사용 중인 이메일입니다."); break;
                case AuthError.PasswordWeak: view.Show(texts ? texts.pwWeak : "최소 8자, 문자+숫자 포함"); break;
                default: view.Show(texts ? texts.signupFail : "가입 실패. 다시 시도하세요."); break;
            }
            return;
        }

        // 성공
        view.Show(texts ? texts.signupDone : "가입 완료");
        if (navigator) navigator.IsAuthenticated = true;
        navigator?.GoTo(ScreenId.HOME);
    }

    // ── 실시간 힌트(선택) ────────────────────────────────────
    void HandlePasswordChanged(string pw)
    {
        if (string.IsNullOrEmpty(pw)) { view.SetPasswordHint("", false); return; }
        if (AuthValidator.IsStrongPassword(pw))
            view.SetPasswordHint(texts ? texts.pwStrong : "안전한 비밀번호", true);
        else
            view.SetPasswordHint(texts ? texts.pwWeak : "최소 8자, 문자+숫자 포함", false);

        HandleConfirmChanged(view.CurrentConfirm);
    }

    void HandleConfirmChanged(string confirm)
    {
        if (string.IsNullOrEmpty(confirm)) { view.SetConfirmHint("", false); return; }
        bool ok = confirm == view.CurrentPass;
        view.SetConfirmHint(ok ? "일치" : (texts ? texts.pwConfirmMismatch : "불일치"), ok);
    }
}
