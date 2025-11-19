using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SignupFormUI))]
public class SignupController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] SceneNavigator navigator;              // (필요시) 씬 이동
    [Header("Tabs")]
    [SerializeField] RegisterTabsController tabs;           // RegisterScene 탭 관리자
    [Header("Texts (Optional)")]
    [SerializeField] AuthUIText texts;

    private SignupFormUI view;
    private IAuthService auth;

    void Awake()
    {
        view = GetComponent<SignupFormUI>();

        //  DataService에서 Auth 가져오기
        if (DataService.Instance != null && DataService.Instance.Auth != null)
        {
            auth = DataService.Instance.Auth;
        }
        else // TODO : 추후 베포시 ELSE부분 삭제하기
        {
            Debug.LogWarning("[SignupController] DataService.Auth 없음, 임시 AuthService 생성");
            auth = new AuthService(new UserRepository());
        }

        // 이벤트 바인딩
        view.OnCheckEmailRequested += HandleCheckEmail;
        view.OnSignupRequested += HandleSignup;
        view.OnCancelRequested += HandleCancel;
        view.OnPasswordChanged += HandlePasswordChanged;
        view.OnConfirmChanged += HandleConfirmChanged;
    }

    void OnDestroy()
    {
        if (view == null) return;

        view.OnCheckEmailRequested -= HandleCheckEmail;
        view.OnSignupRequested -= HandleSignup;
        view.OnCancelRequested -= HandleCancel;
        view.OnPasswordChanged -= HandlePasswordChanged;
        view.OnConfirmChanged -= HandleConfirmChanged;
    }

    // ── 이메일 중복 체크 ───────────────────────────────────
    void HandleCheckEmail(string email)
    {
        if (auth == null) return;

        email = (email ?? "").Trim();

        if (!AuthValidator.IsValidEmail(email))
        {
            view.SetEmailHint(texts ? texts.emailFormatError : "이메일 형식이 올바르지 않습니다.", false);
            return;
        }

        var res = auth.Exists(email);
        if (!res.Ok)
        {
            view.SetEmailHint(texts ? texts.signupFail : "이메일 확인 중 오류가 발생했습니다.", false);
            return;
        }

        if (res.Value)
            view.SetEmailHint(texts ? texts.emailDuplicate : "이미 사용 중인 이메일입니다.", false);
        else
            view.SetEmailHint(texts ? texts.emailAvailable : "사용 가능한 이메일입니다.", true);
    }

    // ── 회원가입 ───────────────────────────────────────────
    void HandleSignup(string name, string email, string password)
    {
        if (auth == null) return;

        name = (name ?? "").Trim();
        email = (email ?? "").Trim();

        if (string.IsNullOrEmpty(name))
        {
            view.Show(texts ? texts.nameEmpty : "이름을 입력해 주세요.");
            return;
        }

        if (!AuthValidator.IsValidEmail(email))
        {
            view.Show(texts ? texts.emailFormatError : "이메일 형식이 올바르지 않습니다.");
            return;
        }

        if (!AuthValidator.IsStrongPassword(password))
        {
            view.Show(texts ? texts.pwWeak : "비밀번호가 너무 약합니다.");
            return;
        }

        var res = auth.SignUp(name, email, password);
        if (!res.Ok)
        {
            view.Show(!string.IsNullOrEmpty(res.Message)
                ? res.Message
                : (texts ? texts.signupFail : "회원가입에 실패했습니다."));
            return;
        }

        view.Show(texts ? texts.signupDone : "회원가입이 완료되었습니다.");

        // 가입 후 로그인 탭으로
        if (tabs != null)
            tabs.ShowLogin();
    }

    // ── 비밀번호 강도 / 일치 여부 힌트 ─────────────────────
    void HandlePasswordChanged(string pw)
    {
        if (string.IsNullOrEmpty(pw))
        {
            view.SetPasswordHint("", false);
            return;
        }

        if (AuthValidator.IsStrongPassword(pw))
            view.SetPasswordHint(texts ? texts.pwStrong : "안전한 비밀번호입니다.", true);
        else
            view.SetPasswordHint(texts ? texts.pwWeak : "최소 8자, 문자+숫자를 포함해야 합니다.", false);

        HandleConfirmChanged(view.CurrentConfirm);
    }

    void HandleConfirmChanged(string confirm)
    {
        if (string.IsNullOrEmpty(confirm))
        {
            view.SetConfirmHint("", false);
            return;
        }

        bool ok = confirm == view.CurrentPass;
        view.SetConfirmHint(ok
            ? "일치"
            : (texts ? texts.pwConfirmMismatch : "비밀번호가 일치하지 않습니다."),
            ok);
    }

    // ── 취소 버튼 ─────────────────────────────────────────
    void HandleCancel()
    {
        if (tabs != null)
            tabs.ShowLogin();
        else if (navigator != null)
            navigator.GoTo(ScreenId.REGISTER);
    }
}
