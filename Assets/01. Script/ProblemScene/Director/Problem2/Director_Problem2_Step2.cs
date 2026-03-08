using UnityEngine;

public class Director_Problem2_Step2 : Director_Problem2_Step2_Logic
{
    [Header("Emotion Slots")]
    [SerializeField] private EmotionSlot[] slots;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override EmotionSlot[] Slots => slots;
    protected override StepCompletionGate CompletionGate => completionGate;
}