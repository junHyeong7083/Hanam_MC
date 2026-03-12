using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SpriteSwapButton - 포인터 상태에 따라 버튼 스프라이트를 교체하는 컴포넌트
///
/// 【역할】 기본/호버/눌림 3가지 스프라이트를 인스펙터에서 지정하면,
///          포인터 Enter/Exit/Down/Up 이벤트에 따라 Image.sprite를 자동 교체한다.
///          Button 컴포넌트가 있고 interactable=false이면 동작하지 않음.
/// 【사용 위치】 커스텀 스프라이트 전환이 필요한 버튼 (Unity 기본 SpriteState 대안)
/// 【트리거】 IPointerEnter/Exit/Down/Up 이벤트 (EventSystem)
/// 【의존성】 Image(RequireComponent), Button(선택 - interactable 체크용)
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteSwapButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private Sprite normalSprite;   // 기본 상태 스프라이트
    [SerializeField] private Sprite hoverSprite;    // 마우스 호버 상태 스프라이트
    [SerializeField] private Sprite pressedSprite;  // 눌림 상태 스프라이트

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
