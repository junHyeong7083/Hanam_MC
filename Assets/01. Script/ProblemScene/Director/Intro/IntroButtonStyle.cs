using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IntroButtonStyle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Image targetImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.65f, 1f);
    [SerializeField] private Color pressedColor = new Color(1f, 0.85f, 0.15f, 1f);

    private bool _isPressed = false;

    private void Reset()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        ApplyNormal();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
        ApplyPressed();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        ApplyNormal();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isPressed) return;
        _isPressed = false;
        ApplyNormal();
    }

    private void ApplyNormal()
    {
        if (targetImage != null)
            targetImage.color = normalColor;
    }

    private void ApplyPressed()
    {
        if (targetImage != null)
            targetImage.color = pressedColor;
    }
}