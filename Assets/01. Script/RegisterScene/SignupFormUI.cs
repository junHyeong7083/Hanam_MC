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

    public event Action<string> OnCheckEmailRequested;
    public event Action<string, string, string> OnSignupRequested; // name, email, password
    public event Action OnCancelRequested;

    // 실시간 입력 변화 이벤트
    public event Action<string> OnPasswordChanged;
    public event Action<string> OnConfirmChanged;

    void Awake()
    {
        SetSubmitInteractable(false);

        // 버튼 이벤트 등록
        if (checkEmailButton) checkEmailButton.onClick.AddListener(InvokeCheckEmail);
        if (submitButton) submitButton.onClick.AddListener(InvokeSignup);
        if (cancelButton) cancelButton.onClick.AddListener(InvokeCancel);
        // 입력 변화 감지 → 컨트롤러로 전달
        if (passwordInput) passwordInput.onValueChanged.AddListener(v => OnPasswordChanged?.Invoke(v));
        if (confirmInput) confirmInput.onValueChanged.AddListener(v => OnConfirmChanged?.Invoke(v));
    }
    void InvokeCheckEmail() => OnCheckEmailRequested?.Invoke(CurrentEmail);
    void InvokeSignup() => OnSignupRequested?.Invoke(CurrentName, CurrentEmail, CurrentPass);
    void InvokeCancel() => OnCancelRequested?.Invoke();


    public void SetEmailHint(string msg, bool good = false) { if (emailHint) { emailHint.text = msg; emailHint.color = good ? Color.blue : Color.red; } }
    public void SetPasswordHint(string msg, bool good = false) { if (passwordHint) { passwordHint.text = msg; passwordHint.color = good ? Color.blue : Color.red; } }
    public void SetConfirmHint(string msg, bool good = false) { if (confirmHint) { confirmHint.text = msg; confirmHint.color = good ? Color.blue : Color.red; } }
    public void SetSubmitInteractable(bool on) { if (submitButton) submitButton.interactable = on; }

    public void Show(string msg) { if (messageText) messageText.text = msg ?? ""; }

    public void Clear()
    {
        if (emailInput) emailInput.text = "";
        if (passwordInput) passwordInput.text = "";
        if (confirmInput) confirmInput.text = "";
        if (nameInput) nameInput.text = "";
        SetEmailHint(""); SetPasswordHint(""); SetConfirmHint(""); Show("");
        SetSubmitInteractable(false);
    }

    // 컨트롤러 편의 프로퍼티
    public string CurrentEmail => emailInput ? emailInput.text.Trim() : "";
    public string CurrentName => nameInput ? nameInput.text.Trim() : "";
    public string CurrentPass => passwordInput ? passwordInput.text : "";
    public string CurrentConfirm => confirmInput ? confirmInput.text : "";
}
