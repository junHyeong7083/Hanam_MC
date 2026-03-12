using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// VirtualKeyboardInputField - 개별 InputField에 부착하여 가상 키보드와 명시적으로 연동하는 컴포넌트
///
/// 【역할】 TMP_InputField에 부착하여:
///         1) 물리 키보드 입력 비활성화 (키오스크 환경용)
///         2) 선택(OnSelect) 시 VirtualKeyboardController를 통해 키보드 표시
///         3) 수동으로 ShowKeyboard()/HideKeyboard() 호출 가능
///         참고: VirtualKeyboardController가 EventSystem을 통해 자동 감지하므로
///               이 스크립트는 선택 사항이며, 추가 커스텀 동작이 필요할 때만 사용한다.
/// 【씬】 텍스트 입력이 필요한 씬 (선택적으로 사용)
/// 【참조하는 곳】 개별 InputField에 부착
/// 【참조되는 곳】 VirtualKeyboardController (키보드 표시/숨김)
/// 【흐름】 InputField 선택 → OnSelect() → VirtualKeyboardController.ShowKeyboard()
/// </summary>
public class VirtualKeyboardInputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("===== 컨트롤러 참조 =====")]
    [SerializeField] private VirtualKeyboardController keyboardController;  // 키보드 컨트롤러 (미설정 시 자동 탐색)

    private TMP_InputField _inputField;  // 이 GameObject의 TMP_InputField 컴포넌트

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

    /// <summary>InputField가 선택되었을 때 호출 (ISelectHandler). 키보드를 자동으로 표시한다.</summary>
    public void OnSelect(BaseEventData eventData)
    {
        if (keyboardController != null)
            keyboardController.ShowKeyboard(_inputField);
    }

    /// <summary>InputField 선택 해제 시 호출 (IDeselectHandler). Controller가 자동 처리하므로 여기선 무동작.</summary>
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
