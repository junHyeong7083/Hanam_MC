using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// [선택적] InputField에 붙여서 가상 키보드와 연동
/// - VirtualKeyboardController가 자동 감지하므로 이 스크립트는 선택 사항
/// - 특정 InputField에 추가 동작이 필요할 때만 사용
/// </summary>
public class VirtualKeyboardInputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("===== 컨트롤러 참조 =====")]
    [SerializeField] private VirtualKeyboardController keyboardController;

    private TMP_InputField _inputField;

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();

        // 물리 키보드 입력 비활성화 (키오스크용)
        if (_inputField != null)
            _inputField.shouldHideMobileInput = true;
    }

    private void Start()
    {
        if (keyboardController == null)
            keyboardController = FindObjectOfType<VirtualKeyboardController>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (keyboardController != null)
            keyboardController.ShowKeyboard(_inputField);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Controller가 자동 처리하므로 여기선 아무것도 안함
    }

    /// <summary>
    /// 수동으로 키보드 표시
    /// </summary>
    public void ShowKeyboard()
    {
        if (keyboardController != null)
            keyboardController.ShowKeyboard(_inputField);
    }

    /// <summary>
    /// 수동으로 키보드 숨김
    /// </summary>
    public void HideKeyboard()
    {
        if (keyboardController != null)
            keyboardController.HideKeyboard();
    }
}
