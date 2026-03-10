using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 호버 + (선택) 3상태 스프라이트 시스템
/// - hover: scale 확대, (옵션) x 이동, (옵션) outline
/// - isDialogueButton=true면 Resources/Buttons 에서
///   기본/누르는중/선택 스프라이트를 불러와 상태에 따라 Image.sprite를 변경
/// </summary>
public class ButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("===== 호버 설정 =====")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("===== X 이동 (옵션) =====")]
    [SerializeField] private bool enableMoveX = false;
    [SerializeField] private float moveXDistance = 10f;

    [Header("===== Outline (옵션) =====")]
    [SerializeField] private Outline outline;

    [Header("===== Dialogue Button Sprite (옵션) =====")]
    [SerializeField] private bool isDialogueButton = false;

    [Tooltip("버튼 기본 상태 스프라이트 (Resources/Buttons/...)")]
    [SerializeField] private string spritePathNormal = "Buttons/button_01";

    [Tooltip("버튼 타겟팅(누르는중) 상태 스프라이트 (Resources/Buttons/...)")]
    [SerializeField] private string spritePathPressed = "Buttons/button_02";

    [Tooltip("버튼 선택(손 뗌) 상태 스프라이트 (Resources/Buttons/...)")]
    [SerializeField] private string spritePathSelected = "Buttons/button_03";

    private string btnsfx = "SFX_btn";
    // 내부
    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Vector2 _originalPosition;

    private bool _isHovering;
    private bool _isInteractable = true;
    private bool _isPressing;

    private Image _image;
    private Sprite _sprNormal;
    private Sprite _sprPressed;
    private Sprite _sprSelected;

    // 녹음 등 외부에서 강제로 적용할 스프라이트 오버라이드
    private Sprite _spriteOverride;

    private void OnEnable()
    {
        var s = transform.localScale;
        if (s.x != 0f && s.y != 0f && s.z != 0f)
            _originalScale = s;
        else if (_originalScale == Vector3.zero)
            _originalScale = Vector3.one;

        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _originalPosition = _rectTransform.anchoredPosition;

        if (outline != null) outline.enabled = false;

        _image = GetComponent<Image>();

        if (isDialogueButton)
            LoadDialogueSprites();

        ApplySpriteNormal();
    }

    private void Update()
    {
        if (!_isInteractable || _spriteOverride != null) return;
        if (_originalScale.sqrMagnitude < 0.0001f) return;  // NaN 방지

        float targetScale = _isHovering ? hoverScale : 1f;
        float currentScale = transform.localScale.x / _originalScale.x;
        if (float.IsNaN(currentScale) || float.IsInfinity(currentScale))
        {
            transform.localScale = _originalScale;
            return;
        }
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * animationSpeed);
        transform.localScale = _originalScale * newScale;

        if (enableMoveX && _rectTransform != null)
        {
            float targetX = _isHovering ? _originalPosition.x + moveXDistance : _originalPosition.x;
            float currentX = _rectTransform.anchoredPosition.x;
            float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * animationSpeed);
            _rectTransform.anchoredPosition = new Vector2(newX, _rectTransform.anchoredPosition.y);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isInteractable || _spriteOverride != null) return;

        _isHovering = true;
        if (outline != null) outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_spriteOverride != null) return;

        _isHovering = false;
        if (outline != null) outline.enabled = false;

        if (!_isPressing)
            ApplySpriteNormal();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isInteractable || _spriteOverride != null) return;

        _isPressing = true;
        ApplyPressed();

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isInteractable || _spriteOverride != null) return;

        _isPressing = false;
        ApplySpriteSelected();
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;

        if (!interactable)
        {
            _isHovering = false;
            _isPressing = false;

            transform.localScale = _originalScale;
            ResetPosition();

            if (outline != null) outline.enabled = false;

            ApplySpriteNormal();
        }
    }

    public void ResetScale()
    {
        transform.localScale = _originalScale;
        _isHovering = false;
        _isPressing = false;
        ResetPosition();
        ApplySpriteNormal();
    }

    private void ResetPosition()
    {
        if (enableMoveX && _rectTransform != null)
            _rectTransform.anchoredPosition = new Vector2(_originalPosition.x, _rectTransform.anchoredPosition.y);
    }

    private void OnDisable()
    {
        _isHovering = false;
        _isPressing = false;

        if (_originalScale != Vector3.zero)
            transform.localScale = _originalScale;

        ResetPosition();

        if (outline != null) outline.enabled = false;
    }

    private void LoadDialogueSprites()
    {
        if (_sprNormal == null && !string.IsNullOrEmpty(spritePathNormal))
            _sprNormal = Resources.Load<Sprite>(spritePathNormal);

        if (_sprPressed == null && !string.IsNullOrEmpty(spritePathPressed))
            _sprPressed = Resources.Load<Sprite>(spritePathPressed);

        if (_sprSelected == null && !string.IsNullOrEmpty(spritePathSelected))
            _sprSelected = Resources.Load<Sprite>(spritePathSelected);

        if (_image == null)
        {
            Debug.LogWarning("[ButtonHover] Image 컴포넌트를 찾지 못했습니다. (스프라이트 적용 불가)");
            return;
        }

        if (_sprNormal == null) Debug.LogWarning($"[ButtonHover] Normal Sprite Load 실패: {spritePathNormal}");
        if (_sprPressed == null) Debug.LogWarning($"[ButtonHover] Pressed Sprite Load 실패: {spritePathPressed}");
        if (_sprSelected == null) Debug.LogWarning($"[ButtonHover] Selected Sprite Load 실패: {spritePathSelected}");
    }

    /// <summary>
    /// 외부에서 스프라이트를 강제로 고정 (녹음 중 등)
    /// 오버라이드 중에는 Normal/Pressed/Selected 전환이 적용되지 않음
    /// </summary>
    public void SetSpriteOverride(Sprite spr)
    {
        _spriteOverride = spr;
        if (_image != null && spr != null)
            _image.sprite = spr;
    }

    /// <summary>
    /// 스프라이트 오버라이드 해제 → Normal 상태로 복귀
    /// </summary>
    public void ClearSpriteOverride()
    {
        _spriteOverride = null;
        _isHovering = false;
        _isPressing = false;
        if (outline != null) outline.enabled = false;
        transform.localScale = _originalScale;
        ApplySpriteNormal();
    }

    private void ApplySpriteNormal()
    {
        if (!isDialogueButton) return;
        if (_image == null) return;
        if (_spriteOverride != null) return; // 오버라이드 중 무시
        if (_sprNormal == null) return;

        _image.sprite = _sprNormal;
    }
    private void ApplyPressed()
    {
        ApplySpritePressed();
        ApplySFXPressed();
    }
    private void ApplySpritePressed()
    {
        if (!isDialogueButton) return;
        if (_image == null) return;
        if (_spriteOverride != null) return; // 오버라이드 중 무시
        if (_sprPressed == null) return;

        _image.sprite = _sprPressed;
    }
    
    private void ApplySFXPressed()
    {
        if (!string.IsNullOrEmpty(btnsfx))
        {
            var sm = SoundManager.Instance;
            if (sm != null) sm.PlaySFX(btnsfx);
        }
    }
    private void ApplySpriteSelected()
    {
        if (!isDialogueButton) return;
        if (_image == null) return;
        if (_spriteOverride != null) return; // 오버라이드 중 무시
        if (_sprSelected == null) return;

        _image.sprite = _sprSelected;
    }
}