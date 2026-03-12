using UnityEngine;

/// <summary>
/// Director_Problem2_Step2 - 문제2 스텝2의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 감정 슬롯(EmotionSlot) 배열과 완료 게이트를 바인딩한다.
///         실제 리빌 로직은 부모(Director_Problem2_Step2_Logic)에 있다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제2 / 스텝2 (메인 활동 - 감정 필름 리빌)
/// 【부모 클래스】 Director_Problem2_Step2_Logic → ProblemStepBase
/// </summary>
public class Director_Problem2_Step2 : Director_Problem2_Step2_Logic
{
    [Header("Emotion Slots")]
    [SerializeField] private EmotionSlot[] slots;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override EmotionSlot[] Slots => slots;
    protected override StepCompletionGate CompletionGate => completionGate;
}