using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem1 / Step2
/// - 이 클래스는 UI 바인딩 + 프로퍼티 매핑만 담당.
/// - 실제 로직(클릭, 플래시, 알파, 게이트)은
///   Director_Problem1_Step2_Logic 쪽에서 모두 처리.
/// </summary>
public class Director_Problem1_Step2 : Director_Problem1_Step2_Logic
{
    [Header("필름 목록")]
    [SerializeField] private FilmFragment[] films;

    [Header("알파 세팅 (흐림/선명)")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.4f;
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 1f;

    [Header("완료 게이트 (프로그레스/다음 버튼)")]
    [SerializeField] private StepCompletionGate completionGate;

    // === 베이스 추상 프로퍼티 매핑 ===
    protected override FilmFragment[] Films => films;
    protected override float DimAlpha => dimAlpha;
    protected override float NormalAlpha => normalAlpha;
    protected override StepCompletionGate CompletionGate => completionGate;
}
