using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Text = UnityEngine.UI.Text;

/// <summary>
/// InputFieldLabel - InputField와 표시할 라벨 문자열을 매핑하는 직렬화 가능 클래스.
/// VirtualKeyboardController의 customLabels 배열에서 사용하여,
/// 특정 InputField가 선택되었을 때 미러 영역에 어떤 라벨을 표시할지 결정한다.
/// 예: 이메일 InputField → "이메일", 비밀번호 InputField → "비밀번호"
/// </summary>
[System.Serializable]
public class InputFieldLabel
{
    [Tooltip("대상 InputField")]
    public TMP_InputField inputField;     // 이 라벨이 적용될 InputField
    [Tooltip("표시할 라벨 (예: 이메일, 비밀번호)")]
    public string label;                   // 미러 영역에 표시할 라벨 텍스트
}

/// <summary>
/// VirtualKeyboardController - 가상 키보드의 표시/숨김 및 InputField 연동을 총괄하는 컨트롤러
///
/// 【역할】 키오스크 환경에서:
///         1) EventSystem의 선택 변화를 감지하여 TMP_InputField 클릭 시 자동으로 키보드 표시
///         2) "미러" InputField 패턴: 딤 배경 위에 별도 InputField를 표시하고,
///            여기에 입력하면 원본 InputField에 동기화 (사용자 경험 향상)
///         3) DOTween 슬라이드 애니메이션으로 키보드/미러를 부드럽게 열고 닫음
///         4) 딤 배경 클릭 또는 Enter 키로 키보드 닫기
///         5) InputField별 커스텀 라벨 매핑 (placeholder, 부모 Label, 직접 설정)
/// 【씬】 RegisterScene, HomeScene 등 텍스트 입력이 필요한 모든 씬
/// 【참조하는 곳】 VirtualKeyboardInputField (수동 표시/숨김), 씬 내 자동 감지
/// 【참조되는 곳】 VirtualKeyboard (Enter 이벤트), DOTween (애니메이션)
/// 【흐름】 InputField 선택 감지 → ShowKeyboard() → 미러 설정 + 애니메이션
///         → 입력(미러→원본 동기화) → Enter/딤 클릭 → HideKeyboard()
/// </summary>
public class VirtualKeyboardController : MonoBehaviour
{
    [Header("===== 키보드 컨테이너 =====")]
    [SerializeField] private GameObject keyboardContainer;     // 키보드 전체 루트 오브젝트
    [SerializeField] private RectTransform keyboardRect;       // 키보드 RectTransform (애니메이션용)

    [Header("===== 가상 키보드 =====")]
    [SerializeField] private VirtualKeyboard virtualKeyboard;  // VirtualKeyboard 컴포넌트 참조

    [Header("===== 미러 입력 필드 (딤 위에 표시) =====")]
    [Tooltip("라벨 텍스트 (예: 이메일, 비밀번호)")]
    [SerializeField] private TMP_Text mirrorLabel;             // 미러 영역 상단 라벨 텍스트
    [Tooltip("미러 InputField (여기에 입력하면 원본에 동기화)")]
    [SerializeField] private TMP_InputField mirrorInputField;  // 미러 InputField (실제 입력 대상)
    [Tooltip("미러 영역 루트 (라벨 + InputField)")]
    [SerializeField] private GameObject mirrorRoot;            // 미러 영역 루트 오브젝트
    [SerializeField] private RectTransform mirrorRect;         // 미러 RectTransform (애니메이션용)

    [Header("===== 배경 딤 =====")]
    [SerializeField] private Image dimBackground;              // 배경 딤 이미지 (반투명 검정)
    [SerializeField] private Color dimColor = new Color(0, 0, 0, 0.5f);  // 딤 색상

    [Header("===== 애니메이션 =====")]
    [SerializeField] private bool useAnimation = true;          // 애니메이션 사용 여부
    [SerializeField] private float animationDuration = 0.25f;   // 애니메이션 시간(초)
    [SerializeField] private Ease showEase = Ease.OutQuad;      // 표시 애니메이션 이징
    [SerializeField] private Ease hideEase = Ease.InQuad;       // 숨김 애니메이션 이징

    [Header("===== 설정 =====")]
    [Tooltip("시작 시 키보드 숨김")]
    [SerializeField] private bool hideOnStart = true;           // 시작 시 키보드 숨김 여부

    [Header("===== 커스텀 라벨 매핑 =====")]
    [Tooltip("InputField별 표시할 라벨을 직접 설정")]
    [SerializeField] private InputFieldLabel[] customLabels;    // InputField → 라벨 매핑 배열

    // ── 자동 감지용 ──
    private GameObject _lastSelectedObject;       // 마지막으로 선택된 GameObject (변화 감지)
    private TMP_InputField _originalInputField;   // 원본 InputField (키보드가 덮고 있는 실제 필드)
    private bool _isSyncing;                      // 미러↔원본 동기화 중 플래그 (무한 루프 방지)

    // ── 애니메이션용 위치 ──
    private Vector2 _keyboardHiddenPos;   // 키보드 숨김 위치 (화면 아래)
    private Vector2 _keyboardShownPos;    // 키보드 표시 위치 (원래 위치)
    private Vector2 _mirrorHiddenPos;     // 미러 숨김 위치 (화면 위)
    private Vector2 _mirrorShownPos;      // 미러 표시 위치 (원래 위치)
    private bool _isAnimating;            // 애니메이션 진행 중 플래그
    private Sequence _currentSequence;    // 현재 DOTween 시퀀스

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

    /// <summary>
    /// EventSystem의 선택 오브젝트가 변경되었을 때 호출.
    /// 키보드 내부 버튼/딤/미러 자체 선택은 무시하고,
    /// TMP_InputField가 선택되면 키보드를 표시한다.
    /// </summary>
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

    /// <summary>지정된 GameObject가 키보드 컨테이너의 자식인지 확인한다</summary>
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

    /// <summary>
    /// 키보드를 표시한다. inputField가 지정되면 해당 필드의 미러를 설정한다.
    /// 이미 보이는 상태면 미러만 업데이트한다.
    /// </summary>
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

    /// <summary>키보드를 숨긴다. 닫기 전 미러→원본 최종 동기화를 수행한다.</summary>
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

    /// <summary>
    /// 원본 InputField의 설정(contentType, characterLimit 등)과 현재 값을
    /// 미러 InputField에 복사하고, VirtualKeyboard의 대상을 미러로 설정한다.
    /// </summary>
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

    /// <summary>미러 InputField에 포커스를 설정한다 (다음 프레임에 실행)</summary>
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

    /// <summary>
    /// InputField에 맞는 라벨 텍스트를 결정한다.
    /// 우선순위: 1) customLabels 매핑 → 2) placeholder 텍스트 → 3) 부모의 "Label" 오브젝트 → 4) 오브젝트 이름
    /// </summary>
    private string GetInputFieldLabel(TMP_InputField inputField)
    {
        // 1. 커스텀 라벨 매핑에서 먼저 찾기
        if (customLabels != null)
        {
            foreach (var mapping in customLabels)
            {
                if (mapping.inputField == inputField && !string.IsNullOrEmpty(mapping.label))
                    return mapping.label;
            }
        }

        // 2. placeholder에서 가져오기
        if (inputField.placeholder != null)
        {
            var tmpPlaceholder = inputField.placeholder as TMP_Text;
            if (tmpPlaceholder != null && !string.IsNullOrEmpty(tmpPlaceholder.text))
                return tmpPlaceholder.text;

            var textPlaceholder = inputField.placeholder as Text;
            if (textPlaceholder != null && !string.IsNullOrEmpty(textPlaceholder.text))
                return textPlaceholder.text;
        }

        // 3. 부모에서 "Label" 이름의 Text 찾기
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

        // 4. 오브젝트 이름 사용
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

    /// <summary>미러 InputField의 텍스트를 원본 InputField에 동기화한다 (_isSyncing으로 무한 루프 방지)</summary>
    private void SyncToOriginal()
    {
        if (_originalInputField == null || mirrorInputField == null) return;

        _isSyncing = true;
        _originalInputField.text = mirrorInputField.text;
        _isSyncing = false;
    }

    #endregion

    #region Animation

    /// <summary>키보드/딤/미러 표시 애니메이션 재생 (DOTween Sequence: 딤 페이드인 + 키보드 슬라이드업 + 미러 슬라이드다운)</summary>
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

    /// <summary>키보드/딤/미러 숨김 애니메이션 재생 (DOTween Sequence: 딤 페이드아웃 + 키보드 슬라이드다운 + 미러 슬라이드업)</summary>
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
