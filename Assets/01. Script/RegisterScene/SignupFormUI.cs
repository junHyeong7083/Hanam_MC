using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SignupFormUI - 회원가입 폼의 순수 UI 레이어 (View)
///
/// 【역할】 이메일/비밀번호/비밀번호 확인/이름 입력 필드를 관리하고,
///         중복확인/가입/취소 버튼 클릭과 실시간 입력 변화를 이벤트로 발행한다.
///         힌트 텍스트(이메일/비밀번호/확인)의 내용과 색상을 컨트롤러 요청에 따라 표시한다.
/// 【씬】 RegisterScene (로그인/회원가입 화면)
/// 【참조하는 곳】 SignupController (이벤트 구독 및 힌트/메시지 제어)
/// 【참조되는 곳】 없음 (이벤트 기반으로 외부에 알림)
/// 【흐름】 입력 변화 → OnPasswordChanged/OnConfirmChanged 발행 (실시간 힌트)
///         버튼 클릭 → OnCheckEmailRequested/OnSignupRequested/OnCancelRequested 발행
/// </summary>
public class SignupFormUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] TMP_InputField emailInput, passwordInput, confirmInput, nameInput;  // 이메일/비밀번호/확인/이름 입력 필드

    [Header("Buttons")]
    [SerializeField] Button checkEmailButton, submitButton, cancelButton;  // 중복확인/가입/취소 버튼

    [Header("Hints")]
    [SerializeField] TMP_Text emailHint, passwordHint, confirmHint, messageText;  // 힌트 및 상태 메시지 텍스트

    /// <summary>이메일 중복 확인 버튼 클릭 시 발행. 매개변수: 이메일</summary>
    public event Action<string> OnCheckEmailRequested;
    /// <summary>가입 버튼 클릭 시 발행. 매개변수: (이름, 이메일, 비밀번호)</summary>
    public event Action<string, string, string> OnSignupRequested;
    /// <summary>취소 버튼 클릭 시 발행</summary>
    public event Action OnCancelRequested;

    /// <summary>비밀번호 입력 실시간 변화 이벤트 (컨트롤러에서 강도 힌트 갱신용)</summary>
    public event Action<string> OnPasswordChanged;
    /// <summary>비밀번호 확인 입력 실시간 변화 이벤트 (컨트롤러에서 일치 힌트 갱신용)</summary>
    public event Action<string> OnConfirmChanged;

    void Awake()
    {
        // 인스펙터 OnClick 잔여 리스너 완전 제거 후, 코드로만 바인딩
        if (checkEmailButton) { checkEmailButton.onClick.RemoveAllListeners(); checkEmailButton.onClick.AddListener(InvokeCheckEmail); }
        if (submitButton) { submitButton.onClick.RemoveAllListeners(); submitButton.onClick.AddListener(InvokeSignup); }
        if (cancelButton) { cancelButton.onClick.RemoveAllListeners(); cancelButton.onClick.AddListener(InvokeCancel); }

        // 실시간 입력 변화 → 컨트롤러로 전달
        if (passwordInput) passwordInput.onValueChanged.AddListener(v => OnPasswordChanged?.Invoke(v));
        if (confirmInput) confirmInput.onValueChanged.AddListener(v => OnConfirmChanged?.Invoke(v));

        // 버튼은 항상 눌리도록: 초기 비활성화 코드 없음
    }

    void OnDestroy()
    {
        if (checkEmailButton) checkEmailButton.onClick.RemoveListener(InvokeCheckEmail);
        if (submitButton) submitButton.onClick.RemoveListener(InvokeSignup);
        if (cancelButton) cancelButton.onClick.RemoveListener(InvokeCancel);
    }

    // ── 버튼 래퍼 ─────────────────────────────────────────────
    void InvokeCheckEmail() => OnCheckEmailRequested?.Invoke(CurrentEmail);

    void InvokeSignup()
    {
        Debug.Log("[SignupFormUI] Submit clicked");
        OnSignupRequested?.Invoke(CurrentName, CurrentEmail, CurrentPass);
    }

    void InvokeCancel()
    {
        Debug.Log("[SignupFormUI] Cancel clicked");
        OnCancelRequested?.Invoke();
    }

    // ── 컨트롤러에서 UI만 제어 ────────────────────────────────
    /// <summary>이메일 힌트 텍스트와 색상 설정 (good=true: 파란색, false: 빨간색)</summary>
    public void SetEmailHint(string msg, bool good = false)
    {
        if (emailHint) { emailHint.text = msg; emailHint.color = good ? Color.blue : Color.red; }
    }
    /// <summary>비밀번호 힌트 텍스트와 색상 설정</summary>
    public void SetPasswordHint(string msg, bool good = false)
    {
        if (passwordHint) { passwordHint.text = msg; passwordHint.color = good ? Color.blue : Color.red; }
    }
    /// <summary>비밀번호 확인 힌트 텍스트와 색상 설정</summary>
    public void SetConfirmHint(string msg, bool good = false)
    {
        if (confirmHint) { confirmHint.text = msg; confirmHint.color = good ? Color.blue : Color.red; }
    }

    /// <summary>가입 버튼의 상호작용 가능 여부 설정 (현재 정책상 항상 활성화)</summary>
    public void SetSubmitInteractable(bool on)
    {
        if (submitButton) submitButton.interactable = on;
    }

    /// <summary>상태 메시지를 표시한다</summary>
    public void Show(string msg) { if (messageText) messageText.text = msg ?? ""; }

    /// <summary>모든 입력 필드, 힌트, 메시지를 초기화한다</summary>
    public void Clear()
    {
        if (emailInput) emailInput.text = "";
        if (passwordInput) passwordInput.text = "";
        if (confirmInput) confirmInput.text = "";
        if (nameInput) nameInput.text = "";
        SetEmailHint(""); SetPasswordHint(""); SetConfirmHint(""); Show("");
        // 버튼 비활성화하지 않음
    }

    // ── 컨트롤러 편의 프로퍼티 (현재 입력값 읽기) ────────────────
    /// <summary>현재 입력된 이메일 (trim 처리)</summary>
    public string CurrentEmail => emailInput ? emailInput.text.Trim() : "";
    /// <summary>현재 입력된 이름 (trim 처리)</summary>
    public string CurrentName => nameInput ? nameInput.text.Trim() : "";
    /// <summary>현재 입력된 비밀번호 (원본 그대로)</summary>
    public string CurrentPass => passwordInput ? passwordInput.text : "";
    /// <summary>현재 입력된 비밀번호 확인 (원본 그대로)</summary>
    public string CurrentConfirm => confirmInput ? confirmInput.text : "";
}
