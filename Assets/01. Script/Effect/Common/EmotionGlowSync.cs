using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스프라이트 색상을 글로우 셰이더에 동기화
/// - spriteImage의 색상을 glowImage 머티리얼에 적용
/// - 머티리얼 인스턴스로 각 오브젝트별 독립 색상
///
/// [사용처]
/// - Problem2 Step2: 감정 조명 글로우
///
/// [구조]
/// EmotionLight (빈 오브젝트 + 이 스크립트)
/// ├── SpriteImage (원형 스프라이트)
/// └── GlowImage (OuterGlow 머티리얼)
/// </summary>
public class EmotionGlowSync : MonoBehaviour
{
    [Header("이미지 참조")]
    [SerializeField] private Image spriteImage;  // 색상 소스 (원형 스프라이트)
    [SerializeField] private Image glowImage;    // 글로우 셰이더 적용 대상

    [Header("셰이더 프로퍼티")]
    [Tooltip("EmotionPulse: _BaseColor, OuterGlow: _GlowColor")]
    [SerializeField] private string colorPropertyName = "_GlowColor";

    // 머티리얼 인스턴스 (공유 방지)
    private Material _materialInstance;
    private RectTransform _glowRectTransform;

    private void Awake()
    {
        if (glowImage != null)
        {
            _glowRectTransform = glowImage.GetComponent<RectTransform>();

            if (glowImage.material != null)
            {
                // 머티리얼 인스턴스 생성 (다른 글로우와 색상 공유 안 되게)
                _materialInstance = Instantiate(glowImage.material);
                glowImage.material = _materialInstance;
            }
        }
    }

    private void OnEnable()
    {
        SyncColor();
    }

    private void Start()
    {
        // OnEnable보다 늦게 호출 - Image.color가 다른 스크립트에서 설정된 후 동기화
        SyncColor();
        SyncAspect();
    }

    /// <summary>
    /// GlowImage의 RectTransform 비율을 머티리얼에 자동 동기화
    /// </summary>
    public void SyncAspect()
    {
        if (_glowRectTransform == null || _materialInstance == null) return;

        Vector2 size = _glowRectTransform.rect.size;
        if (size.y > 0)
        {
            float aspect = size.x / size.y;
            _materialInstance.SetFloat("_Aspect", aspect);
        }
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }

    /// <summary>
    /// SpriteImage 색상을 GlowImage 머티리얼에 동기화
    /// </summary>
    public void SyncColor()
    {
        if (spriteImage == null || _materialInstance == null) return;

        _materialInstance.SetColor(colorPropertyName, spriteImage.color);
    }

    /// <summary>
    /// 외부에서 색상 설정 (SpriteImage + GlowImage 머티리얼 동시 변경)
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteImage != null)
            spriteImage.color = color;

        if (_materialInstance != null)
            _materialInstance.SetColor(colorPropertyName, color);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 색상 변경 시 즉시 반영
        if (Application.isPlaying && _materialInstance != null)
        {
            SyncColor();
        }
    }
#endif
}
