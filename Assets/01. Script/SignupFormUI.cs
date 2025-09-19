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

    // 버튼은 인스펙터에서 연결
    public void ClickCheckEmail() => OnCheckEmailRequested?.Invoke(emailInput.text.Trim());
    public void ClickSignup() => OnSignupRequested?.Invoke(nameInput.text.Trim(), emailInput.text.Trim(), passwordInput.text);
    public void ClickCancel() => OnCancelRequested?.Invoke();

    // 컨트롤러에서 UI만 제어
    public void SetEmailHint(string msg, bool good = false) { if (emailHint) { emailHint.text = msg; emailHint.color = good ? Color.cyan : Color.red; } }
    public void SetPasswordHint(string msg, bool good = false) { if (passwordHint) { passwordHint.text = msg; passwordHint.color = good ? Color.cyan : Color.red; } }
    public void SetConfirmHint(string msg, bool good = false) { if (confirmHint) { confirmHint.text = msg; confirmHint.color = good ? Color.cyan : Color.red; } }
    public void SetSubmitInteractable(bool on) { if (submitButton) submitButton.interactable = on; }
    public void Show(string msg) { if (messageText) messageText.text = msg; }
    public void Clear()
    {
        if (emailInput) emailInput.text = "";
        if (passwordInput) passwordInput.text = "";
        if (confirmInput) confirmInput.text = "";
        if (nameInput) nameInput.text = "";
        SetEmailHint(""); SetPasswordHint(""); SetConfirmHint(""); Show("");
        SetSubmitInteractable(false);
    }
    public string CurrentConfirm => confirmInput ? confirmInput.text : "";
}
