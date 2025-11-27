using UnityEngine;

[CreateAssetMenu(menuName = "MindMovie/Problem Context", fileName = "ProblemContext")]
public class ProblemContext : ScriptableObject
{
    [Header("문제 메타")]
    public ProblemTheme Theme = ProblemTheme.Director;

    [Tooltip("테마 안에서의 문제 번호 (1부터 시작, 예: 1..10)")]
    public int ProblemIndex = 1;

    // 나중에 서버/DB Problem 테이블과 연결할 때 사용할 Id (지금은 비워둬도 됨)
    public string ProblemId;

    [Header("현재 Step Key (로그용, 문자열)")]
    public string CurrentStepKey;

    /// <summary>
    /// 이 컨텍스트 기준으로 Attempt 저장.
    /// body에는 "이 스텝 전용 데이터 구조"만 넣어준다.
    /// </summary>
    public void SaveStepAttempt(object body)
    {
        var ds = DataService.Instance;
        if (ds == null || ds.Progress == null)
        {
            Debug.LogWarning("[ProblemContext] DataService.Progress 없음 - SaveStepAttempt 스킵");
            return;
        }

        var payload = new
        {
            stepKey = CurrentStepKey,
            theme = Theme.ToString(),
            problemIndex = ProblemIndex,
            body
        };

        var result = ds.Progress.SaveStepAttemptForCurrentUser(
            Theme,
            ProblemIndex,
            ProblemId,
            payload
        );

        if (!result.Ok)
            Debug.LogWarning("[ProblemContext] SaveStepAttempt 실패: " + result.Error);
        else
            Debug.Log("[ProblemContext] SaveStepAttempt DB 저장 완료");
    }

    /// <summary>
    /// Attempt + 인벤토리 보상을 함께 저장.
    /// body에는 이 스텝 전용 데이터 구조만 넣어준다.
    /// </summary>
    public void SaveReward(object body, string itemId, string itemName)
    {
        var ds = DataService.Instance;
        if (ds == null || ds.Reward == null)
        {
            Debug.LogWarning("[ProblemContext] DataService.Reward 없음 - SaveReward 스킵");
            return;
        }

        var payload = new
        {
            stepKey = CurrentStepKey,
            theme = Theme.ToString(),
            problemIndex = ProblemIndex,
            body
        };

        var result = ds.Reward.SaveRewardForCurrentUser(
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
