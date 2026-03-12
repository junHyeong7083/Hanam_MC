using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RadialBurstEffect - UI Image에 방사형 광선(스타버스트) 이펙트를 적용하는 컴포넌트
///
/// 【역할】 UI/RadialBurst 커스텀 셰이더를 사용하여 중심에서 바깥으로 뻗어나가는 광선 효과 생성.
///          광선 수, 너비, 부드러움, 회전 속도, 펄스 등을 인스펙터에서 조절 가능.
///          머티리얼 인스턴스를 생성하여 다른 오브젝트와 셰이더 파라미터 공유 방지.
/// 【사용 위치】 보상 화면, 완료 연출 등 임팩트가 필요한 장면
/// 【트리거】 playOnEnable=true 시 OnEnable에서 자동 재생, 또는 외부에서 Play()/Stop() 호출
/// 【의존성】 Image(RequireComponent), UI/RadialBurst 셰이더 (Assets/07. Shader/)
/// </summary>
[RequireComponent(typeof(Image))]
public class RadialBurstEffect : MonoBehaviour
{
    [Header("광선 설정")]
    [Tooltip("광선 색상")]
    public Color rayColor = new Color(1f, 0.95f, 0.5f, 1f);

    [Tooltip("광선 개수")]
    [Range(4, 40)]
    public int rayCount = 12;

    [Tooltip("광선 너비 (0=얇음, 1=넓음)")]
    [Range(0.01f, 1f)]
    public float rayWidth = 0.5f;

    [Tooltip("광선 경계 부드러움")]
    [Range(0.001f, 0.5f)]
    public float raySoftness = 0.05f;

    [Header("페이드")]
    [Tooltip("안쪽 페이드 시작 반경")]
    [Range(0f, 0.5f)]
    public float innerRadius = 0.05f;

    [Tooltip("바깥 페이드 끝 반경")]
    [Range(0.1f, 1f)]
    public float outerRadius = 0.7f;

    [Header("중심 원")]
    [Tooltip("중심 원 색상")]
    public Color centerColor = Color.white;

    [Tooltip("중심 글로우 강도")]
    [Range(0f, 3f)]
    public float centerGlow = 1f;

    [Header("회전")]
    [Tooltip("회전 속도 (음수=반시계)")]
    [Range(-3f, 3f)]
    public float rotateSpeed = 0.3f;

    [Header("펄스")]
    public bool enablePulse;

    [Range(0f, 5f)]
    public float pulseSpeed = 2f;

    [Range(0f, 1f)]
    public float pulseMin = 0.6f;

    [Range(0f, 1f)]
    public float pulseMax = 1f;

    [Header("자동 재생")]
    public bool playOnEnable = true;

    private Image _image;
    private Material _matInstance;

    private static readonly int PropRayColor = Shader.PropertyToID("_RayColor");
    private static readonly int PropRayCount = Shader.PropertyToID("_RayCount");
    private static readonly int PropRayWidth = Shader.PropertyToID("_RayWidth");
    private static readonly int PropRaySoftness = Shader.PropertyToID("_RaySoftness");
    private static readonly int PropInnerRadius = Shader.PropertyToID("_InnerRadius");
    private static readonly int PropOuterRadius = Shader.PropertyToID("_OuterRadius");
    private static readonly int PropCenterColor = Shader.PropertyToID("_CenterColor");
    private static readonly int PropCenterGlow = Shader.PropertyToID("_CenterGlow");
    private static readonly int PropRotateSpeed = Shader.PropertyToID("_RotateSpeed");
    private static readonly int PropEnablePulse = Shader.PropertyToID("_EnablePulse");
    private static readonly int PropPulseSpeed = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PropPulseMin = Shader.PropertyToID("_PulseMin");
    private static readonly int PropPulseMax = Shader.PropertyToID("_PulseMax");

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        if (_image == null) return;

        if (_image.material == null || _image.material.shader.name != "UI/RadialBurst")
        {
            var shader = Shader.Find("UI/RadialBurst");
            if (shader == null)
            {
                Debug.LogWarning("[RadialBurstEffect] UI/RadialBurst 셰이더를 찾을 수 없습니다.");
                return;
            }
            _image.material = new Material(shader);
        }

        _matInstance = Instantiate(_image.material);
        _image.material = _matInstance;
        _image.SetMaterialDirty();

        ApplyProperties();

        if (!playOnEnable)
            _image.enabled = false;
    }

    private void OnDisable()
    {
        if (_matInstance != null)
        {
            Destroy(_matInstance);
            _matInstance = null;
        }
    }

    public void Play()
    {
        if (_image != null)
            _image.enabled = true;
    }

    public void Stop()
    {
        if (_image != null)
            _image.enabled = false;
    }

    /// <summary>
    /// 현재 인스펙터 값을 머티리얼에 반영
    /// </summary>
    public void ApplyProperties()
    {
        if (_matInstance == null) return;

        _matInstance.SetColor(PropRayColor, rayColor);
        _matInstance.SetFloat(PropRayCount, rayCount);
        _matInstance.SetFloat(PropRayWidth, rayWidth);
        _matInstance.SetFloat(PropRaySoftness, raySoftness);
        _matInstance.SetFloat(PropInnerRadius, innerRadius);
        _matInstance.SetFloat(PropOuterRadius, outerRadius);
        _matInstance.SetColor(PropCenterColor, centerColor);
        _matInstance.SetFloat(PropCenterGlow, centerGlow);
        _matInstance.SetFloat(PropRotateSpeed, rotateSpeed);
        _matInstance.SetFloat(PropEnablePulse, enablePulse ? 1f : 0f);
        _matInstance.SetFloat(PropPulseSpeed, pulseSpeed);
        _matInstance.SetFloat(PropPulseMin, pulseMin);
        _matInstance.SetFloat(PropPulseMax, pulseMax);

        if (_image != null)
            _image.SetMaterialDirty();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_matInstance != null)
            ApplyProperties();
    }
#endif
}
