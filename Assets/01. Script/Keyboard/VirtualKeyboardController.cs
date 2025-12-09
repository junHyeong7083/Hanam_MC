using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 가상 키보드 컨테이너 컨트롤러
/// - 키보드 표시/숨김 관리
/// - InputField 자동 감지
/// - Enter 시 키보드 숨김
/// </summary>
/// 

public class VirtualKeyboardController : MonoBehaviour
{
    [Header("===== 키보드 컨테이너 =====")]
    [SerializeField] private GameObject keyboardContainer;

    [Header("===== 가상 키보드 =====")]
    [SerializeField] private VirtualKeyboard virtualKeyboard;

    [Header("===== 설정 =====")]
    [Tooltip("시작 시 키보드 숨김")]
    [SerializeField] private bool hideOnStart = true;

    // 자동 감지용
    private GameObject _lastSelectedObject;
    private TMP_InputField _currentInputField;

    private void Start()
    {
        // 시작 시 키보드 숨김
        if (hideOnStart && keyboardContainer != null)
            keyboardContainer.SetActive(false);

        // Enter 이벤트 구독
        if (virtualKeyboard != null)
            virtualKeyboard.OnEnterPressed += OnEnterPressed;
    }

    private void OnDestroy()
    {
        if (virtualKeyboard != null)
            virtualKeyboard.OnEnterPressed -= OnEnterPressed;
    }

    private void Update()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        var currentSelected = eventSystem.currentSelectedGameObject;

        // 선택된 오브젝트가 변경되었을 때
        if (currentSelected != _lastSelectedObject)
        {
            _lastSelectedObject = currentSelected;
            OnSelectionChanged(currentSelected);
        }

        // 키보드 영역 밖 클릭 시 숨김
        CheckOutsideClick();
    }

    private void CheckOutsideClick()
    {
        // 키보드가 안 보이면 무시
        if (keyboardContainer == null || !keyboardContainer.activeSelf) return;

        // 클릭/터치 감지
        if (!Input.GetMouseButtonDown(0)) return;

        // 키보드 컨테이너의 RectTransform
        RectTransform keyboardRect = keyboardContainer.GetComponent<RectTransform>();
        if (keyboardRect == null) return;

        // 클릭 위치가 키보드 영역 안인지 확인
        Vector2 mousePos = Input.mousePosition;
        if (!RectTransformUtility.RectangleContainsScreenPoint(keyboardRect, mousePos, null))
        {
            // InputField 클릭인지 확인 (InputField 클릭 시에는 숨기지 않음)
            var pointerData = new PointerEventData(EventSystem.current) { position = mousePos };
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject.GetComponent<TMP_InputField>() != null)
                    return;  // InputField 클릭이면 숨기지 않음
            }

            // 키보드 밖 클릭 → 숨김
            HideKeyboard();
        }
    }

    private void OnSelectionChanged(GameObject selected)
    {
        if (selected == null)
        {
            return;
        }

        // 키보드 내부 버튼 클릭은 무시
        if (IsPartOfKeyboard(selected))
        {
            return;
        }

        // TMP_InputField인지 확인
        var inputField = selected.GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            _currentInputField = inputField;
            ShowKeyboard(inputField);
        }
    }

    private bool IsPartOfKeyboard(GameObject obj)
    {
        if (keyboardContainer == null || obj == null) return false;

        Transform t = obj.transform;
        while (t != null)
        {
            if (t.gameObject == keyboardContainer)
                return true;
            t = t.parent;
        }
        return false;
    }

    #region Public API

    public void ShowKeyboard(TMP_InputField inputField = null)
    {
        if (inputField != null && virtualKeyboard != null)
            virtualKeyboard.SetTargetInputField(inputField);

        if (keyboardContainer != null)
            keyboardContainer.SetActive(true);
    }

    public void HideKeyboard()
    {
        if (keyboardContainer != null)
            keyboardContainer.SetActive(false);
    }

    #endregion

    private void OnEnterPressed()
    {
        HideKeyboard();
    }
}
