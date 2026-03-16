using UnityEngine;

/// <summary>
/// ProblemContext - 문제별 런타임 컨텍스트 데이터 (ScriptableObject)
///
/// 【역할】 각 문제(Problem)의 메타 정보를 담는 ScriptableObject.
///          테마, 문제 번호, 고유 ID, 현재 스텝 키를 보유한다.
/// 【참조하는 곳】 ProblemStepBase (context 필드로 참조), 각 Problem Director의 Logic 클래스
/// 【참조되는 곳】 ProblemStepBase.BuildStepKey(), SaveAttempt(), SaveReward()에서 활용
/// 【흐름】 인스펙터에서 각 Problem_N 오브젝트에 할당 → ProblemStepBase가 참조
/// </summary>
[CreateAssetMenu(menuName = "MindMovie/Problem Context", fileName = "ProblemContext")]
public class ProblemContext : ScriptableObject
{
    [Header("문제 메타")]
    public ProblemTheme Theme = ProblemTheme.Director; // 테마: Director 또는 Gardener

    [Tooltip("그룹 안에서의 문제 번호 (1부터 시작, 예: 1..10)")]
    public int ProblemIndex = 1; // 문제 번호 (1-based)

    // 추후 서버/DB Problem 테이블과 연동할 때 사용할 고유 ID (현재는 비어있어도 됨)
    public string ProblemId;

    [Header("현재 Step Key (로그용, 문자열)")]
    public string CurrentStepKey; // 현재 진행 중인 스텝의 키 문자열 (예: "Director_P1_Step2")
    // ※ SaveAttempt / SaveReward는 ProblemStepBase에서 StepKeyConfig 기반으로 처리함
}
