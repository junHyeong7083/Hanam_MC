using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMPro는 사용하지 않지만, 원본 파일에 있었으므로 유지

/// <summary>
/// Director / Problem2 / Step2
/// - 이 클래스는 UI 바인딩 + 프로퍼티 매핑만 담당.
/// - 실제 로직은 Director_Problem2_Step2_Logic(베이스)에서 처리.
/// </summary>
public class Director_Problem2_Step2 : Director_Problem2_Step2_Logic
{
    // === 원본과 동일한 [SerializeField] 필드 유지 ===

    [Header("Emotion Light Slots")]
    [SerializeField] private EmotionLightSlot[] slots; // Logic 클래스의 protected EmotionLightSlot 사용

    [Header("Line Animation Settings")]
    [SerializeField] private float lineDrawDuration = 0.35f;   // 선이 그려지는 시간
    [SerializeField] private float lineHoldDuration = 0.4f;    // 다 그려진 후 유지 시간
    [SerializeField] private float lineFadeDuration = 0.25f;   // 줄어들며 사라지는 시간
    [SerializeField] private AnimationCurve lineWidthCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private float lineMaxThickness = 4f;      // UI 픽셀 단위 두께 (sizeDelta.y)

    [Header("Light 등장 애니메이션 (옵션)")]
    [SerializeField] private bool playLightAppearAnimation = true;
    [SerializeField] private float lightAppearDuration = 0.25f;
    [SerializeField] private float lightAppearScale = 1.15f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;


    // === 베이스 추상 프로퍼티 매핑 ===

    protected override EmotionLightSlot[] Slots => slots;

    protected override float LineDrawDuration => lineDrawDuration;
    protected override float LineHoldDuration => lineHoldDuration;
    protected override float LineFadeDuration => lineFadeDuration;
    protected override AnimationCurve LineWidthCurve => lineWidthCurve;
    protected override float LineMaxThickness => lineMaxThickness;

    protected override bool PlayLightAppearAnimation => playLightAppearAnimation;
    protected override float LightAppearDuration => lightAppearDuration;
    protected override float LightAppearScale => lightAppearScale;

    protected override StepCompletionGate CompletionGate => completionGate;
}