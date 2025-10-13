using System;
using TMPro;
using UnityEngine;

public class LoginFormUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] TMP_InputField emailInput, passwordInput;

    [Header("Feedback")]
    [SerializeField] TMP_Text messageText;

    public event Action<string, string> OnLoginRequested;
    public event Action OnGoSignupRequested;

    public void ClickLogin() => OnLoginRequested?.Invoke(emailInput.text.Trim(), passwordInput.text);
    public void ClickGoSignup() => OnGoSignupRequested?.Invoke();

    // 컨트롤러에서 UI만 제어
    public void Show(string msg) { if (messageText) messageText.text = msg; }
    public void Clear()
    {
        if (emailInput) emailInput.text = "";
        if (passwordInput) passwordInput.text = "";
        Show("");
    }
    public void SetInteractable(bool on)
    {
        if (emailInput) emailInput.interactable = on;
        if (passwordInput) passwordInput.interactable = on;
    }
}
