using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VirtualNumpad - 숫자 전용 가상 키패드 (0~9 + 백스페이스 + 전체삭제 + 확인)
///
/// 【역할】 전화번호, 인증번호 등 숫자만 입력해야 하는 상황에서 사용하는 간단한 숫자 키패드.
///         VirtualKeyboard(전체 키보드)와 별도로, 숫자 입력 전용으로 가볍게 동작한다.
///         - 0~9 숫자 버튼, 백스페이스(마지막 자리 삭제), 전체 삭제, 확인 버튼
///         - maxLength로 최대 입력 자릿수 제한
/// 【씬】 숫자 입력이 필요한 씬 (RegisterScene 등)
/// 【참조하는 곳】 씬 내 InputField와 연동하여 사용
/// 【참조되는 곳】 없음 (이벤트 기반으로 외부에 알림)
/// 【흐름】 Show() → 키패드 표시 → 숫자 클릭 → InputField에 추가 → OnTextChanged 발행
///         → 확인 클릭 → OnConfirmed 발행 → Hide()
/// </summary>
public class VirtualNumpad : MonoBehaviour
{
    [Header("===== 키패드 루트 =====")]
    [SerializeField] private GameObject numpadRoot;          // 키패드 전체 루트 오브젝트

    [Header("===== 연결된 InputField =====")]
    [SerializeField] private InputField targetInputField;    // 입력 대상 InputField (Legacy UI)

    [Header("===== 숫자 버튼 (0-9) =====")]
    [SerializeField] private Button[] numberButtons;         // 0~9 숫자 버튼 배열 (인덱스 = 숫자)

    [Header("===== 특수 버튼 =====")]
    [SerializeField] private Button backspaceButton;         // 마지막 자리 삭제 버튼
    [SerializeField] private Button clearButton;             // 전체 삭제 버튼
    [SerializeField] private Button confirmButton;           // 확인 버튼

    [Header("===== 입력 제한 =====")]
    [SerializeField] private int maxLength = 11;             // 최대 입력 자릿수

    /// <summary>텍스트 변경 시 발행. 매개변수: 현재 전체 텍스트</summary>
    public event Action<string> OnTextChanged;
    /// <summary>확인 버튼 클릭 시 발행</summary>
    public event Action OnConfirmed;

    /// <summary>키패드가 현재 보이는 상태인지 여부</summary>
    public bool IsNumpadVisible => numpadRoot != null && numpadRoot.activeSelf;

    private void Start()
    {
        SetupNumberButtons();
        SetupSpecialButtons();

        if (numpadRoot != null)
            numpadRoot.SetActive(false);
    }

    #region Public API

    /// <summary>키패드를 표시한다. inputField가 지정되면 대상 InputField도 변경한다.</summary>
    public void Show(InputField inputField = null)
    {
        if (inputField != null)
            targetInputField = inputField;

        if (numpadRoot != null)
            numpadRoot.SetActive(true);
    }

    /// <summary>키패드를 숨긴다</summary>
    public void Hide()
    {
        if (numpadRoot != null)
            numpadRoot.SetActive(false);
    }

    /// <summary>입력 대상 InputField를 설정한다</summary>
    public void SetTargetInputField(InputField inputField)
    {
        targetInputField = inputField;
    }

    /// <summary>현재 InputField의 텍스트를 반환한다</summary>
    public string GetText()
    {
        return targetInputField != null ? targetInputField.text : string.Empty;
    }

    /// <summary>InputField의 텍스트를 전체 삭제하고 OnTextChanged 이벤트를 발행한다</summary>
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
