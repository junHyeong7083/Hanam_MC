using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼에 붙여서 사용하는 스프라이트 스왑 컴포넌트.
/// - 기본 / 호버 / 눌림 스프라이트를 인스펙터에서 지정
/// - Button.interactable이 false면 동작하지 않음
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteSwapButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;

    private Image _image;
    private Button _button;
    private bool _isHovering;

    private void Awake()
    {
        _image  = GetComponent<Image>();
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _isHovering = false;
        ApplySprite(normalSprite);
    }

    private bool IsInteractable => _button == null || _button.interactable;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        _isHovering = true;
        ApplySprite(hoverSprite);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        if (!IsInteractable) return;
        ApplySprite(normalSprite);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        ApplySprite(pressedSprite);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable) return;
        ApplySprite(_isHovering ? hoverSprite : normalSprite);
    }

    private void ApplySprite(Sprite sprite)
    {
        if (_image == null || sprite == null) return;
        _image.sprite = sprite;
    }
}
