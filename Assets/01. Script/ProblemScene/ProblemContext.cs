using UnityEngine;

[CreateAssetMenu(menuName = "MindMovie/Problem Context", fileName = "ProblemContext")]
public class ProblemContext : ScriptableObject
{
    [Header("문제 메타")]
    public ProblemTheme Theme = ProblemTheme.Director;
    public int ProblemIndex = 1;
    public string ProblemId;

    [Header("현재 Step Key (로그용)")]
    public string CurrentStepKey;

    /// <summary>
    /// 이 컨텍스트 기준으로 Attempt 저장.
    /// body에는 "이 스텝 전용 데이터 구조"만 넣어준다.
    /// </summary>
    public void SaveStepAttempt(object body)
    {
        var ds = DataService.Instance;
        if (ds == null || ds.User == null)
        {
            Debug.LogWarning("[ProblemContext] DataService.User 없음 - SaveStepAttempt 스킵");
            return;
        }

        // 공통으로 쓰는 wrapper를 한 번만 정의
        var payload = new
        {
            stepKey = CurrentStepKey,
            theme = Theme.ToString(),
            problemIndex = ProblemIndex,
            body
        };

        var result = ds.User.SaveStepAttemptForCurrentUser(
            Theme,
            ProblemIndex,
            ProblemId,
            payload
        );

        if (!result.Ok)
            Debug.LogWarning("[ProblemContext] SaveStepAttempt 실패: " + result.Error);
        Debug.Log("DB저장 완료");
    }

    /// <summary>
    /// Attempt + 인벤토리 보상을 함께 저장.
    /// body에는 이 스텝 전용 데이터 구조만 넣어준다.
    /// </summary>
    public void SaveReward(object body, string itemId, string itemName)
    {
        var ds = DataService.Instance;
        if (ds == null || ds.User == null)
        {
            Debug.LogWarning("[ProblemContext] DataService.User 없음 - SaveReward 스킵");
            return;
        }

        var payload = new
        {
            stepKey = CurrentStepKey,
            theme = Theme.ToString(),
            problemIndex = ProblemIndex,
            body
        };

        var result = ds.User.SaveRewardForCurrentUser(
            Theme,
            ProblemIndex,
            ProblemId,
            payload,
            itemId,
            itemName
        );

        if (!result.Ok)
            Debug.LogWarning("[ProblemContext] SaveReward 실패: " + result.Error);
    }
}
