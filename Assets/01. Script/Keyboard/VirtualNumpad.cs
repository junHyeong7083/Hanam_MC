using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 숫자 전용 가상 키패드
/// - 0-9 숫자 입력
/// - 백스페이스, 확인 버튼
/// </summary>
public class VirtualNumpad : MonoBehaviour
{
    [Header("===== 키패드 루트 =====")]
    [SerializeField] private GameObject numpadRoot;

    [Header("===== 연결된 InputField =====")]
    [SerializeField] private InputField targetInputField;

    [Header("===== 숫자 버튼 (0-9) =====")]
    [SerializeField] private Button[] numberButtons;

    [Header("===== 특수 버튼 =====")]
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button confirmButton;

    [Header("===== 입력 제한 =====")]
    [SerializeField] private int maxLength = 11;

    public event Action<string> OnTextChanged;
    public event Action OnConfirmed;

    public bool IsNumpadVisible => numpadRoot != null && numpadRoot.activeSelf;

    private void Start()
    {
        SetupNumberButtons();
        SetupSpecialButtons();

        if (numpadRoot != null)
            numpadRoot.SetActive(false);
    }

    #region Public API

    public void Show(InputField inputField = null)
    {
        if (inputField != null)
            targetInputField = inputField;

        if (numpadRoot != null)
            numpadRoot.SetActive(true);
    }

    public void Hide()
    {
        if (numpadRoot != null)
            numpadRoot.SetActive(false);
    }

    public void SetTargetInputField(InputField inputField)
    {
        targetInputField = inputField;
    }

    public string GetText()
    {
        return targetInputField != null ? targetInputField.text : string.Empty;
    }

    public void Clear()
    {
        if (targetInputField != null)
        {
            targetInputField.text = string.Empty;
            OnTextChanged?.Invoke(string.Empty);
        }
    }

    #endregion

    #region Setup

    private void SetupNumberButtons()
    {
        if (numberButtons == null) return;

        for (int i = 0; i < numberButtons.Length && i < 10; i++)
        {
            int num = i;
            var btn = numberButtons[i];
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnNumberPressed(num));
            }
        }
    }

    private void SetupSpecialButtons()
    {
        if (backspaceButton != null)
        {
            backspaceButton.onClick.RemoveAllListeners();
            backspaceButton.onClick.AddListener(OnBackspacePressed);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(Clear);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmPressed);
        }
    }

    #endregion

    #region Handlers

    private void OnNumberPressed(int number)
    {
        if (targetInputField == null) return;

        if (targetInputField.text.Length >= maxLength) return;

        targetInputField.text += number.ToString();
        OnTextChanged?.Invoke(targetInputField.text);
    }

    private void OnBackspacePressed()
    {
        if (targetInputField == null) return;

        string text = targetInputField.text;
        if (text.Length > 0)
        {
            targetInputField.text = text.Substring(0, text.Length - 1);
            OnTextChanged?.Invoke(targetInputField.text);
        }
    }

    private void OnConfirmPressed()
    {
        OnConfirmed?.Invoke();
    }

    #endregion
}
