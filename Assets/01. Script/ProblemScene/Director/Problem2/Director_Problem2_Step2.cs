using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMPro�� ������� ������, ���� ���Ͽ� �־����Ƿ� ����

/// <summary>
/// Director / Problem2 / Step2
/// - �� Ŭ������ UI ���ε� + ������Ƽ ���θ� ���.
/// - ���� ������ Director_Problem2_Step2_Logic(���̽�)���� ó��.
/// </summary>
public class Director_Problem2_Step2 : Director_Problem2_Step2_Logic
{
    // === ������ ������ [SerializeField] �ʵ� ���� ===

    [Header("Emotion Light Slots")]
    [SerializeField] private EmotionLightSlot[] slots; // Logic Ŭ������ protected EmotionLightSlot ���

    [Header("Light ���� �ִϸ��̼� (�ɼ�)")]
    [SerializeField] private bool playLightAppearAnimation = true;
    [SerializeField] private float lightAppearDuration = 0.25f;
    [SerializeField] private float lightAppearScale = 1.15f;

    [Header("�Ϸ� ����Ʈ")]
    [SerializeField] private StepCompletionGate completionGate;


    // === ���̽� �߻� ������Ƽ ���� ===

    protected override EmotionLightSlot[] Slots => slots;

    protected override bool PlayLightAppearAnimation => playLightAppearAnimation;
    protected override float LightAppearDuration => lightAppearDuration;
    protected override float LightAppearScale => lightAppearScale;

    protected override StepCompletionGate CompletionGate => completionGate;
}