using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// SignupController - 회원가입 폼의 비즈니스 로직을 담당하는 컨트롤러
///
/// 【역할】 SignupFormUI(View)에서 발행하는 이벤트를 수신하여:
///         1) 이메일 중복 체크 (HandleCheckEmail)
///         2) 입력 유효성 검증 (이름, 이메일 형식, 비밀번호 강도)
///         3) IAuthService.SignUp()으로 회원가입 처리
///         4) 비밀번호 강도/일치 여부 실시간 힌트 제공
///         5) 가입 완료 후 로그인 탭으로 자동 전환
/// 【씬】 RegisterScene (로그인/회원가입 화면)
/// 【참조하는 곳】 RegisterScene의 회원가입 패널에 부착 (SignupFormUI와 같은 GameObject)
/// 【참조되는 곳】 DataService.Auth (인증 서비스), AuthValidator (유효성 검증),
///               RegisterTabsController (탭 전환)
/// 【흐름】 입력 → 이메일 중복 체크 → 유효성 검증 → AuthService.SignUp() → 성공: 로그인 탭 전환
/// </summary>
[RequireComponent(typeof(SignupFormUI))]
public class SignupController : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] RegisterTabsController tabs;           // RegisterScene 탭 관리자 (가입 후 로그인 탭 전환)
    [Header("Texts (Optional)")]
    [SerializeField] AuthUIText texts;                      // UI 텍스트 리소스 (ScriptableObject, 선택 사항)

    private SignupFormUI view;   // 회원가입 폼 UI (같은 GameObject에서 자동 참조)
    private IAuthService auth;   // 인증 서비스 인터페이스 (DataService에서 가져옴)

    void Awake()
    {
        view = GetComponent<SignupFormUI>();

        // DataService에서 Auth 가져오기
        if (DataService.Instance == null || DataService.Instance.Auth == null)
        {
            Debug.LogError("[SignupController] DataService.Auth 없음. DataService 세팅을 먼저 확인하세요.");
            enabled = false;
            return;
        }

        auth = DataService.Instance.Auth;

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
            view.SetPasswordHint(texts ? texts.pwWeak : "최소 6자, 문자+숫자를 포함해야 합니다.", false);

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
        else if (SceneNavigator.Instance != null)
            SceneNavigator.Instance.GoTo(ScreenId.REGISTER);
    }
}
