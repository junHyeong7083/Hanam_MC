using UnityEngine;
using System;
public static class AttemptLogger
{
    static SessionManager Sess => SessionManager.Instance;
    static DataService DS => DataService.Instance;

    public static void SaveStepAttempt(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        string stepKey,
        object payload
    )
    {
        if (DS == null || DS.User == null || Sess == null || !Sess.IsSignedIn)
        {
            Debug.LogWarning("[AttemptLogger] 환경 미구성 - 저장 스킵");
            return;
        }

        // Content 안에 우리가 원하는 구조로 감싸기
        var wrapper = new
        {
            stepKey,
            theme = theme.ToString(),
            problemIndex = problemIndex,
            body = payload
        };

        string contentJson = JsonUtility.ToJson(wrapper);

        var attempt = new Attempt
        {
            SessionId = Sess.SessionId,
            UserEmail = Sess.CurrentUser.Email,
            Content = contentJson,
            ProblemId = problemId,      // 필요 없으면 null 넣어도 됨
            Theme = theme,
            ProblemIndex = problemIndex
        };

        var result = DS.User.SaveAttempt(attempt);
        if (!result.Ok)
        {
            Debug.LogWarning("[AttemptLogger] SaveAttempt 실패: " + result.Error);
        }
    }

    public static void SaveReward(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        string stepKey,
        string itemId,
        string itemName
    )
    {
        if (DS == null || DS.User == null || Sess == null || !Sess.IsSignedIn)
        {
            Debug.LogWarning("[AttemptLogger] 환경 미구성 - 보상 저장 스킵");
            return;
        }

        // 1) Attempt 로그
        var payload = new
        {
            items = new[]
            {
                new { itemId, itemName, unlocked = true }
            }
        };

        SaveStepAttempt(theme, problemIndex, problemId, stepKey, payload);

        // 2) 인벤토리 지급
        var invItem = new InventoryItem
        {
            UserEmail = Sess.CurrentUser.Email,
            ItemId = itemId,
            ItemName = itemName,
            Theme = theme,
            AcquiredAt = DateTime.UtcNow
        };

        var invResult = DS.User.GrantInventoryItem(Sess.CurrentUser.Email, invItem);
        if (!invResult.Ok)
        {
            Debug.LogWarning("[AttemptLogger] 인벤토리 저장 실패: " + invResult.Error);
        }
    }
}