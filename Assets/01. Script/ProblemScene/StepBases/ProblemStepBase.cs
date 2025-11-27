using UnityEngine;

public abstract class ProblemStepBase : MonoBehaviour
{
    [Header("DB 저장 사용 여부")]
    [SerializeField] private bool useDBSave = true;

    [Header("공용 Problem 컨텍스트")]
    [SerializeField] protected ProblemContext context;

    [Header("이 스텝의 고유 키 (Enum 기반)")]
    [SerializeField] protected StepKeyConfig stepKeyConfig;

    protected virtual void OnEnable() => OnStepEnter();
    protected virtual void OnDisable() => OnStepExit();

    protected abstract void OnStepEnter();
    protected virtual void OnStepExit() { }

    // 🔹 여기서만 string으로 변환
    protected string BuildStepKey()
    {
        if (context == null)
        {
            Debug.LogWarning("[ProblemStepBase] context 없음 - BuildStepKey 실패");
            return null;
        }
        return stepKeyConfig.BuildKey(context);
    }

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
