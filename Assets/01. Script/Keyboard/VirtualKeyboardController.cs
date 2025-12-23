using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Text = UnityEngine.UI.Text;

/// <summary>
/// 가상 키보드 컨테이너 컨트롤러
/// - 키보드 표시/숨김 관리
/// - InputField 자동 감지
/// - 미러 InputField (딤 위에 표시, 입력하면 원본에 동기화)
/// - 배경 딤 처리
/// - 슬라이드 애니메이션
/// </summary>
public class VirtualKeyboardController : MonoBehaviour
{
    [Header("===== 키보드 컨테이너 =====")]
    [SerializeField] private GameObject keyboardContainer;
    [SerializeField] private RectTransform keyboardRect;

    [Header("===== 가상 키보드 =====")]
    [SerializeField] private VirtualKeyboard virtualKeyboard;

    [Header("===== 미러 입력 필드 (딤 위에 표시) =====")]
    [Tooltip("라벨 텍스트 (예: 이메일, 비밀번호)")]
    [SerializeField] private TMP_Text mirrorLabel;
    [Tooltip("미러 InputField (여기에 입력하면 원본에 동기화)")]
    [SerializeField] private TMP_InputField mirrorInputField;
    [Tooltip("미러 영역 루트 (라벨 + InputField)")]
    [SerializeField] private GameObject mirrorRoot;
    [SerializeField] private RectTransform mirrorRect;

    [Header("===== 배경 딤 =====")]
    [SerializeField] private Image dimBackground;
    [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.5f);

    [Header("===== 애니메이션 =====")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private Ease showEase = Ease.OutQuad;
    [SerializeField] private Ease hideEase = Ease.InQuad;

    [Header("===== 설정 =====")]
    [Tooltip("시작 시 키보드 숨김")]
    [SerializeField] private bool hideOnStart = true;

    // 자동 감지용
    private GameObject _lastSelectedObject;
    private TMP_InputField _originalInputField; // 원본 InputField (가려진 상태)
    private bool _isSyncing; // 무한 루프 방지

    // 애니메이션용
    private Vector2 _keyboardHiddenPos;
    private Vector2 _keyboardShownPos;
    private Vector2 _mirrorHiddenPos;
    private Vector2 _mirrorShownPos;
    private bool _isAnimating;
    private Sequence _currentSequence;

    private void Start()
    {
        // 키보드 위치 저장
        if (keyboardRect == null && keyboardContainer != null)
            keyboardRect = keyboardContainer.GetComponent<RectTransform>();

        if (keyboardRect != null)
        {
            _keyboardShownPos = keyboardRect.anchoredPosition;
            _keyboardHiddenPos = new Vector2(_keyboardShownPos.x, _keyboardShownPos.y - keyboardRect.rect.height - 50f);
        }

        // 미러 위치 저장
        if (mirrorRect == null && mirrorRoot != null)
            mirrorRect = mirrorRoot.GetComponent<RectTransform>();

        if (mirrorRect != null)
        {
            _mirrorShownPos = mirrorRect.anchoredPosition;
            _mirrorHiddenPos = new Vector2(_mirrorShownPos.x, _mirrorShownPos.y + mirrorRect.rect.height + 50f);
        }

        // 시작 시 키보드 숨김
        if (hideOnStart)
        {
            if (keyboardContainer != null)
                keyboardContainer.SetActive(false);
            if (dimBackground != null)
            {
                dimBackground.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);
                dimBackground.gameObject.SetActive(false);
            }
            if (mirrorRoot != null)
                mirrorRoot.SetActive(false);
        }

        // Enter 이벤트 구독
        if (virtualKeyboard != null)
        {
            virtualKeyboard.OnEnterPressed += OnEnterPressed;
        }

        // 미러 InputField 입력 시 원본에 동기화
        if (mirrorInputField != null)
        {
            mirrorInputField.onValueChanged.AddListener(OnMirrorValueChanged);
        }

        // 딤 배경 클릭 시 키보드 숨김
        if (dimBackground != null)
        {
            var dimButton = dimBackground.GetComponent<Button>();
            if (dimButton == null)
                dimButton = dimBackground.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(HideKeyboard);
        }
    }

    private void OnDestroy()
    {
        if (virtualKeyboard != null)
        {
            virtualKeyboard.OnEnterPressed -= OnEnterPressed;
        }

        if (mirrorInputField != null)
        {
            mirrorInputField.onValueChanged.RemoveListener(OnMirrorValueChanged);
        }

        _currentSequence?.Kill();
    }

    private void Update()
    {
        if (_isAnimating) return;

        var eventSystem = EventSystem.current;
        if (eventSystem == null) return;

        var currentSelected = eventSystem.currentSelectedGameObject;

        // 선택된 오브젝트가 변경되었을 때
        if (currentSelected != _lastSelectedObject)
        {
            _lastSelectedObject = currentSelected;
            OnSelectionChanged(currentSelected);
        }
    }

    private void OnSelectionChanged(GameObject selected)
    {
        if (selected == null) return;

        // 키보드 내부 버튼 클릭은 무시
        if (IsPartOfKeyboard(selected)) return;

        // 딤 배경도 무시
        if (dimBackground != null && selected == dimBackground.gameObject) return;

        // 미러 InputField 자체 선택은 무시
        if (mirrorInputField != null && selected == mirrorInputField.gameObject) return;

        // TMP_InputField인지 확인
        var inputField = selected.GetComponent<TMP_InputField>();
        if (inputField != null && inputField != mirrorInputField)
        {
            _originalInputField = inputField;
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
        if (_isAnimating) return;

        if (inputField != null && inputField != mirrorInputField)
        {
            _originalInputField = inputField;
            SetupMirror(inputField);
        }

        if (keyboardContainer == null) return;

        // 이미 보이는 상태면 미러만 업데이트
        if (keyboardContainer.activeSelf)
        {
            if (_originalInputField != null)
                SetupMirror(_originalInputField);
            return;
        }

        if (useAnimation)
            PlayShowAnimation();
        else
        {
            keyboardContainer.SetActive(true);
            if (dimBackground != null)
            {
                dimBackground.gameObject.SetActive(true);
                dimBackground.color = dimColor;
            }
            if (mirrorRoot != null)
                mirrorRoot.SetActive(true);

            // 미러에 포커스
            ActivateMirrorInputField();
        }
    }

    public void HideKeyboard()
    {
        if (_isAnimating) return;
        if (keyboardContainer == null || !keyboardContainer.activeSelf) return;

        // 닫기 전 최종 동기화
        SyncToOriginal();

        if (useAnimation)
            PlayHideAnimation();
        else
        {
            keyboardContainer.SetActive(false);
            if (dimBackground != null)
                dimBackground.gameObject.SetActive(false);
            if (mirrorRoot != null)
                mirrorRoot.SetActive(false);
        }

        _originalInputField = null;
    }

    #endregion

    #region Mirror Setup

    private void SetupMirror(TMP_InputField original)
    {
        if (mirrorRoot == null || mirrorInputField == null) return;

        mirrorRoot.SetActive(true);

        // 라벨 설정
        if (mirrorLabel != null)
        {
            string label = GetInputFieldLabel(original);
            mirrorLabel.text = label;
        }

        // 미러 InputField 설정
        _isSyncing = true;

        // contentType 복사 (비밀번호 마스킹 등)
        mirrorInputField.contentType = original.contentType;
        mirrorInputField.inputType = original.inputType;
        mirrorInputField.characterLimit = original.characterLimit;

        // 현재 값 복사
        mirrorInputField.text = original.text;

        _isSyncing = false;

        // VirtualKeyboard의 타겟을 미러로 설정
        if (virtualKeyboard != null)
            virtualKeyboard.SetTargetInputField(mirrorInputField);
    }

    private void ActivateMirrorInputField()
    {
        if (mirrorInputField == null) return;

        // 다음 프레임에 포커스 (UI가 활성화된 후)
        StartCoroutine(ActivateMirrorNextFrame());
    }

    private System.Collections.IEnumerator ActivateMirrorNextFrame()
    {
        yield return null;
        if (mirrorInputField != null)
        {
            mirrorInputField.ActivateInputField();
            mirrorInputField.caretPosition = mirrorInputField.text.Length;
        }
    }

    private string GetInputFieldLabel(TMP_InputField inputField)
    {
        // 1. placeholder에서 가져오기
        if (inputField.placeholder != null)
        {
            var tmpPlaceholder = inputField.placeholder as TMP_Text;
            if (tmpPlaceholder != null && !string.IsNullOrEmpty(tmpPlaceholder.text))
                return tmpPlaceholder.text;

            var textPlaceholder = inputField.placeholder as Text;
            if (textPlaceholder != null && !string.IsNullOrEmpty(textPlaceholder.text))
                return textPlaceholder.text;
        }

        // 2. 부모에서 "Label" 이름의 Text 찾기
        var parent = inputField.transform.parent;
        if (parent != null)
        {
            var tmpLabel = parent.Find("Label")?.GetComponent<TMP_Text>();
            if (tmpLabel != null)
                return tmpLabel.text;

            var textLabel = parent.Find("Label")?.GetComponent<Text>();
            if (textLabel != null)
                return textLabel.text;
        }

        // 3. 오브젝트 이름 사용
        return inputField.gameObject.name;
    }

    #endregion

    #region Sync

    private void OnMirrorValueChanged(string text)
    {
        if (_isSyncing) return;

        // 미러 입력 → 원본에 동기화
        SyncToOriginal();
    }

    private void SyncToOriginal()
    {
        if (_originalInputField == null || mirrorInputField == null) return;

        _isSyncing = true;
        _originalInputField.text = mirrorInputField.text;
        _isSyncing = false;
    }

    #endregion

    #region Animation

    private void PlayShowAnimation()
    {
        _isAnimating = true;
        _currentSequence?.Kill();

        // 초기 상태
        keyboardContainer.SetActive(true);
        if (keyboardRect != null)
            keyboardRect.anchoredPosition = _keyboardHiddenPos;

        if (dimBackground != null)
        {
            dimBackground.gameObject.SetActive(true);
            dimBackground.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);
        }

        if (mirrorRoot != null)
        {
            mirrorRoot.SetActive(true);
            if (mirrorRect != null)
                mirrorRect.anchoredPosition = _mirrorHiddenPos;
        }

        _currentSequence = DOTween.Sequence();

        // 딤 페이드 인
        if (dimBackground != null)
            _currentSequence.Join(dimBackground.DOColor(dimColor, animationDuration));

        // 키보드 슬라이드 업 (아래 → 위)
        if (keyboardRect != null)
            _currentSequence.Join(keyboardRect.DOAnchorPos(_keyboardShownPos, animationDuration).SetEase(showEase));

        // 미러 슬라이드 다운 (위 → 아래)
        if (mirrorRect != null)
            _currentSequence.Join(mirrorRect.DOAnchorPos(_mirrorShownPos, animationDuration).SetEase(showEase));

        _currentSequence.OnComplete(() =>
        {
            _isAnimating = false;
            ActivateMirrorInputField();
        });
    }

    private void PlayHideAnimation()
    {
        _isAnimating = true;
        _currentSequence?.Kill();

        _currentSequence = DOTween.Sequence();

        // 딤 페이드 아웃
        if (dimBackground != null)
            _currentSequence.Join(dimBackground.DOColor(new Color(dimColor.r, dimColor.g, dimColor.b, 0f), animationDuration));

        // 키보드 슬라이드 다운 (위 → 아래로 사라짐)
        if (keyboardRect != null)
            _currentSequence.Join(keyboardRect.DOAnchorPos(_keyboardHiddenPos, animationDuration).SetEase(hideEase));

        // 미러 슬라이드 업 (아래 → 위로 사라짐)
        if (mirrorRect != null)
            _currentSequence.Join(mirrorRect.DOAnchorPos(_mirrorHiddenPos, animationDuration).SetEase(hideEase));

        _currentSequence.OnComplete(() =>
        {
            keyboardContainer.SetActive(false);
            if (dimBackground != null)
                dimBackground.gameObject.SetActive(false);
            if (mirrorRoot != null)
                mirrorRoot.SetActive(false);
            _isAnimating = false;
        });
    }

    #endregion

    private void OnEnterPressed()
    {
        HideKeyboard();
    }
}
