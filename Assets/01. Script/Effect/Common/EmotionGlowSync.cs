using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EmotionGlowSync - 스프라이트 색상을 글로우 셰이더 머티리얼에 자동 동기화하는 컴포넌트
///
/// 【역할】 spriteImage(원형 감정 조명)의 색상을 glowImage의 셰이더 머티리얼 프로퍼티에 실시간 반영.
///          머티리얼 인스턴스를 생성하여 여러 감정 조명이 독립적인 색상을 가질 수 있도록 처리.
///          GlowImage의 종횡비(Aspect)도 자동 계산하여 셰이더에 전달.
/// 【사용 위치】 Problem2 Step2(감정 조명 글로우) - EmotionLight 오브젝트에 부착
/// 【트리거】 OnEnable/Start에서 자동 동기화, 또는 외부에서 SyncColor()/SetColor() 호출
/// 【의존성】 spriteImage(색상 소스 Image), glowImage(셰이더 적용 대상 Image),
///          OuterGlow 또는 EmotionPulse 커스텀 셰이더
///
/// 【계층 구조】
/// EmotionLight (이 스크립트)
/// ├── SpriteImage (원형 스프라이트 - 색상 소스)
/// └── GlowImage (OuterGlow 머티리얼 - 글로우 효과)
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
