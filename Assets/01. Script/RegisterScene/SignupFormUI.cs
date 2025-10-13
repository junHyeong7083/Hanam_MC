using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignupFormUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] TMP_InputField emailInput, passwordInput, confirmInput, nameInput;

    [Header("Buttons")]
    [SerializeField] Button checkEmailButton, submitButton, cancelButton;

    [Header("Hints")]
    [SerializeField] TMP_Text emailHint, passwordHint, confirmHint, messageText;

    // 컨트롤러가 구독할 이벤트
    public event Action<string> OnCheckEmailRequested;                 // email
    public event Action<string, string, string> OnSignupRequested;     // name, email, password
    public event Action OnCancelRequested;

    // 실시간 입력 변화 이벤트 (컨트롤러에서 힌트 갱신)
    public event Action<string> OnPasswordChanged;
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
    public void SetEmailHint(string msg, bool good = false)
    {
        if (emailHint) { emailHint.text = msg; emailHint.color = good ? Color.blue : Color.red; }
    }
    public void SetPasswordHint(string msg, bool good = false)
    {
        if (passwordHint) { passwordHint.text = msg; passwordHint.color = good ? Color.blue : Color.red; }
    }
    public void SetConfirmHint(string msg, bool good = false)
    {
        if (confirmHint) { confirmHint.text = msg; confirmHint.color = good ? Color.blue : Color.red; }
    }

    // 남겨두지만 현재 정책상 사용 안 함(항상 눌림)
    public void SetSubmitInteractable(bool on)
    {
        if (submitButton) submitButton.interactable = on;
    }

    public void Show(string msg) { if (messageText) messageText.text = msg ?? ""; }

    public void Clear()
    {
        if (emailInput) emailInput.text = "";
        if (passwordInput) passwordInput.text = "";
        if (confirmInput) confirmInput.text = "";
        if (nameInput) nameInput.text = "";
        SetEmailHint(""); SetPasswordHint(""); SetConfirmHint(""); Show("");
        // 버튼 비활성화하지 않음
    }

    // ── 컨트롤러 편의 프로퍼티 ────────────────────────────────
    public string CurrentEmail => emailInput ? emailInput.text.Trim() : "";
    public string CurrentName => nameInput ? nameInput.text.Trim() : "";
    public string CurrentPass => passwordInput ? passwordInput.text : "";
    public string CurrentConfirm => confirmInput ? confirmInput.text : "";
}
