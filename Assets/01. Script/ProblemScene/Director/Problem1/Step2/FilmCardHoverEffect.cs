using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 필름 카드용 호버/클릭 스케일 애니메이션.
/// - 마우스 올리면 살짝 확대
/// - 누르는 동안 살짝 축소
/// - 뗐을 때 다시 원래/호버 스케일로 복귀
/// 
/// UI 배치/텍스트/OnClick은 전부 Button에서 그대로 사용하면 되고,
/// 이 스크립트는 localScale만 건드린다.
/// </summary>
public class FilmCardHoverEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform target;   // 애니메이션 줄 대상. 비우면 자기 RectTransform
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float lerpSpeed = 10f;  // 클수록 빠르게 따라감

    private Vector3 _baseScale;
    private Vector3 _targetScale;
    private bool _isPointerInside;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        if (target != null)
        {
            _baseScale = target.localScale;
            _targetScale = _baseScale;
        }
    }

    private void Update()
    {
        if (target == null) return;

        // 지수 lerp로 부드럽게 보간
        float t = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
        target.localScale = Vector3.Lerp(target.localScale, _targetScale, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        _targetScale = _baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        _targetScale = _baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = _baseScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _targetScale = _isPointerInside ? _baseScale * hoverScale : _baseScale;
    }
}
