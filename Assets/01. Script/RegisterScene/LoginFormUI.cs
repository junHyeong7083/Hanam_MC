using System;
using TMPro;
using UnityEngine;

/// <summary>
/// LoginFormUI - 로그인 폼의 순수 UI 레이어 (View)
///
/// 【역할】 이메일/비밀번호 입력 필드와 메시지 텍스트를 관리하며,
///         버튼 클릭 시 OnLoginRequested / OnGoSignupRequested 이벤트를 발행한다.
///         UI 상태(텍스트, 상호작용)만 담당하고 로직은 LoginController가 처리한다.
/// 【씬】 RegisterScene (로그인/회원가입 화면)
/// 【참조하는 곳】 LoginController (이벤트 구독 및 UI 제어)
/// 【참조되는 곳】 없음 (이벤트 기반으로 외부에 알림)
/// 【흐름】 버튼 클릭 → ClickLogin()/ClickGoSignup() → 이벤트 발행 → LoginController에서 처리
/// </summary>
public class LoginFormUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] TMP_InputField emailInput, passwordInput;  // 이메일/비밀번호 입력 필드

    [Header("Feedback")]
    [SerializeField] TMP_Text messageText;  // 상태 메시지 표시 텍스트 (로그인 중, 에러 등)

    /// <summary>로그인 버튼 클릭 시 발행. 매개변수: (이메일, 비밀번호)</summary>
    public event Action<string, string> OnLoginRequested;
    /// <summary>회원가입 화면 이동 버튼 클릭 시 발행</summary>
    public event Action OnGoSignupRequested;

    /// <summary>로그인 버튼 클릭 핸들러 - OnLoginRequested 이벤트 발행 (이메일 trim 처리)</summary>
    public void ClickLogin() => OnLoginRequested?.Invoke(emailInput.text.Trim(), passwordInput.text);
    /// <summary>회원가입 화면 이동 버튼 클릭 핸들러 - OnGoSignupRequested 이벤트 발행</summary>
    public void ClickGoSignup() => OnGoSignupRequested?.Invoke();

    /// <summary>상태 메시지를 표시한다 (컨트롤러에서 호출)</summary>
    public void Show(string msg) { if (messageText) messageText.text = msg; }
    /// <summary>모든 입력 필드와 메시지를 초기화한다</summary>
    public void Clear()
    {
        if (emailInput) emailInput.text = "";
        if (passwordInput) passwordInput.text = "";
        Show("");
    }
    /// <summary>이메일/비밀번호 입력 필드의 상호작용 가능 여부를 설정한다 (로그인 중 비활성화용)</summary>
    public void SetInteractable(bool on)
    {
        if (emailInput) emailInput.interactable = on;
        if (passwordInput) passwordInput.interactable = on;
    }
}
