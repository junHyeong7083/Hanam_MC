using UnityEngine;

/// <summary>
/// ProblemStepBase - 모든 문제 스텝의 최상위 추상 부모 클래스
///
/// 【역할】 문제 풀이의 각 스텝(화면)이 공통으로 갖는 기능을 제공한다:
///          - OnEnable/OnDisable 시 OnStepEnter()/OnStepExit() 자동 호출
///          - ProblemContext와 StepKeyConfig를 통한 DB 키 생성
///          - SaveAttempt() / SaveReward()로 학습 시도 및 보상 데이터를 DB에 저장
/// 【참조하는 곳】 모든 스텝 클래스가 이 클래스를 상속:
///                StartStep, CommonRewardStep, MultipleChoiceStepBase,
///                RandomCardSequenceStepBase, InventoryDropTargetStepBase,
///                그리고 각 Problem Director의 Step 클래스들 (Director_Problem1_Step2 등)
/// 【참조되는 곳】 ProblemContext (컨텍스트 데이터), StepKeyConfig (키 생성),
///                DataService.Progress (시도 저장), DataService.Reward (보상 저장)
/// 【흐름】 StepFlowController가 패널 SetActive(true) → OnEnable() → OnStepEnter() (자식 구현)
///          → 사용자 상호작용 → SaveAttempt(body) 호출
///          → 패널 SetActive(false) → OnDisable() → OnStepExit() (자식 구현)
///
/// ※ 주의: 이 프로젝트에서는 Awake() 대신 OnEnable()에서 초기화해야 함 (스텝 활성화 순서 문제)
/// </summary>
public abstract class ProblemStepBase : MonoBehaviour
{
    [Header("DB 저장 사용 여부")]
    [SerializeField] private bool useDBSave = true; // false로 설정하면 SaveAttempt/SaveReward가 동작하지 않음 (테스트용)

    [Header("공용 Problem 컨텍스트")]
    [SerializeField] protected ProblemContext context; // 이 스텝이 속한 문제의 컨텍스트 (ScriptableObject)

    [Header("이 스텝의 고유 키 (Enum 기반)")]
    [SerializeField] protected StepKeyConfig stepKeyConfig; // DB 저장 시 사용하는 키 설정 (stepId enum)

    /// <summary>스텝 패널이 활성화될 때 호출. OnStepEnter()를 자동 실행한다.</summary>
    protected virtual void OnEnable()
    {
        OnStepEnter();
    }

    /// <summary>스텝 패널이 비활성화될 때 호출. OnStepExit()를 자동 실행한다.</summary>
    protected virtual void OnDisable()
    {
        OnStepExit();
    }

    /// <summary>스텝 진입 시 실행할 로직. 모든 자식 클래스에서 반드시 구현해야 한다.</summary>
    protected abstract void OnStepEnter();

    /// <summary>스텝 퇴장 시 실행할 로직. 필요 시 자식에서 override.</summary>
    protected virtual void OnStepExit() { }

    /// <summary>
    /// StepKeyConfig와 ProblemContext를 조합하여 DB 저장용 키 문자열을 생성한다.
    /// 예: "Director_P1_Step2"
    /// </summary>
    /// <returns>키 문자열. context가 없으면 null 반환.</returns>
    protected string BuildStepKey()
    {
        if (context == null)
        {
            Debug.LogWarning("[ProblemStepBase] context 없음 - BuildStepKey 실패");
            return null;
        }
        return stepKeyConfig.BuildKey(context);
    }

    /// <summary>
    /// 학습 시도(Attempt) 데이터를 DB에 저장한다.
    /// stepKey, theme, problemIndex, body를 포함한 payload를 DataService.Progress에 전달한다.
    /// </summary>
    /// <param name="body">스텝별 시도 데이터 (선택한 답, 시도 횟수 등 자유 형식 객체)</param>
    protected void SaveAttempt(object body)
    {
        if (!useDBSave || context == null)
            return;

        var ds = DataService.Instance;
        if (ds == null || ds.Progress == null)
        {
            Debug.LogWarning("[ProblemStepBase] DataService.Progress 없음 - SaveAttempt 스킵");
            return;
        }

        string stepKey = BuildStepKey();

        var payload = new
        {
            stepKey,
            theme = context.Theme.ToString(),
            problemIndex = context.ProblemIndex,
            body
        };

        var result = ds.Progress.SaveStepAttemptForCurrentUser(
            context.Theme,
            context.ProblemIndex,
            context.ProblemId,
            payload
        );

        if (!result.Ok)
            Debug.LogWarning("[ProblemStepBase] SaveAttempt 실패: " + result.Error);
    }

    /// <summary>
    /// 보상(Reward) 데이터를 DB에 저장한다. 아이템 획득 정보도 함께 저장된다.
    /// DataService.Reward.SaveRewardForCurrentUser()를 호출하여 인벤토리에 아이템을 추가한다.
    /// </summary>
    /// <param name="body">보상 관련 상세 데이터 (자유 형식 객체)</param>
    /// <param name="itemId">보상 아이템의 고유 ID (예: "mind_lens")</param>
    /// <param name="itemName">보상 아이템의 표시 이름 (예: "마음 렌즈")</param>
    protected void SaveReward(object body, string itemId, string itemName)
    {
        if (!useDBSave || context == null)
            return;

        var ds = DataService.Instance;
        if (ds == null || ds.Reward == null)
        {
            Debug.LogWarning("[ProblemStepBase] DataService.Reward 없음 - SaveReward 스킵");
            return;
        }

        string stepKey = BuildStepKey();

        var payload = new
        {
            stepKey,
            theme = context.Theme.ToString(),
            problemIndex = context.ProblemIndex,
            body
        };

        var result = ds.Reward.SaveRewardForCurrentUser(
            context.Theme,
            context.ProblemIndex,
            context.ProblemId,
            payload,
            itemId,
            itemName
        );

        if (!result.Ok)
            Debug.LogWarning("[ProblemStepBase] SaveReward 실패: " + result.Error);
    }
}
