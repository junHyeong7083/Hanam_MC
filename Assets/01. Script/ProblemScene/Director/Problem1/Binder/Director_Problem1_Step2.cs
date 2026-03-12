using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem1_Step2 - 문제1 스텝2의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 필름 조각 배열, 어둡기 설정, 완료 게이트 등을 바인딩한다.
///         실제 필름 클릭/체크/플래시/완료 로직은 부모(Director_Problem1_Step2_Logic)에 있다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝2 (메인 활동 - 필름 조각 찾기)
/// 【부모 클래스】 Director_Problem1_Step2_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Step2 GameObject에 부착
/// 【참조되는 곳】 Director_Problem1_Step2_Logic (abstract property 구현 제공)
/// </summary>
public class Director_Problem1_Step2 : Director_Problem1_Step2_Logic
{
    [Header("필름 목록")]
    [SerializeField] private FilmFragment[] films;    // 필름 조각 배열 (ID, 체크마크, 플래시 등 포함)

    [Header("밝기 설정 (어둠/밝음)")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.4f;    // 터치 전 어둡기 알파값
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 1f;   // 터치 후 밝기 알파값

    [Header("완료 게이트 (프로그래스/다음 버튼)")]
    [SerializeField] private StepCompletionGate completionGate;       // 모든 필름 터치 시 다음 스텝 진행

    // === 베이스 추상 프로퍼티 구현 ===
    protected override FilmFragment[] Films => films;
    protected override float DimAlpha => dimAlpha;
    protected override float NormalAlpha => normalAlpha;

    [HideInInspector] // 직렬화하면서 숨겨야 하는 필드 → 부모가 참조하면 인스펙터가 꼬여서 이렇게 처리
    protected override StepCompletionGate CompletionGate => completionGate;
}
