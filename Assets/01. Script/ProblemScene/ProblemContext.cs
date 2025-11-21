using System;
using UnityEngine;

/// <summary>
/// 한 문제(Theme + Index)에 대한 공용 컨텍스트.
/// - Theme, ProblemIndex, ProblemId 같은 메타 정보
/// - SessionId, UserEmail (가능하면 SessionManager에서 자동 보정)
/// - CurrentStepKey: 현재 저장 중인 Step 식별자 (예: "Director_Problem1_Step3")
/// - SaveStepAttempt / SaveReward 헬퍼 제공
/// </summary>
[CreateAssetMenu(menuName = "Problem/Problem Context", fileName = "ProblemContext")]
public class ProblemContext : ScriptableObject
{
    [Header("문제 메타")]
    public ProblemTheme Theme = ProblemTheme.Director;
    public int ProblemIndex = 1;
    public string ProblemId;

    [HideInInspector]
    public string SessionId;
    [HideInInspector]
    public string UserEmail;

    [Header("현재 Step Key (로그용)")]
    public string CurrentStepKey;

    /// <summary>
    /// 코드에서 한 번에 메타를 세팅하고 싶을 때 사용.
    /// </summary>
    public void Configure(ProblemTheme theme, int problemIndex, string problemId = null)
    {
        Theme = theme;
        ProblemIndex = problemIndex;
        ProblemId = problemId;
    }

    /// <summary>
    /// SessionManager에서 SessionId / UserEmail을 끌어와 채운다.
    /// </summary>
    public void SyncFromSession()
    {
        if (SessionManager.Instance == null) return;

        SessionId = SessionManager.Instance.SessionId;

        if (SessionManager.Instance.CurrentUser != null)
            UserEmail = SessionManager.Instance.CurrentUser.Email;
    }

    private bool EnsureEnvironment()
    {
        if (DataService.Instance == null || DataService.Instance.User == null)
        {
            Debug.LogWarning("[ProblemContext] DataService.Instance.User 없음 - 저장 불가");
            return false;
        }

        if (string.IsNullOrEmpty(UserEmail) || string.IsNullOrEmpty(SessionId))
        {
            // 비어 있으면 SessionManager에서 한 번 더 동기화 시도
            SyncFromSession();
        }

        if (string.IsNullOrEmpty(UserEmail))
        {
            Debug.LogWarning("[ProblemContext] UserEmail 없음 - 저장 불가");
            return false;
        }

        if (string.IsNullOrEmpty(SessionId))
        {
            Debug.LogWarning("[ProblemContext] SessionId 없음 - 저장 불가");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 공통 Attempt 저장 헬퍼.
    /// body에는 "이 스텝 전용 데이터 구조"만 넣어주면 된다.
    /// 실제 Content에는 stepKey/theme/problemIndex/body 형태로 감싸서 저장한다.
    /// </summary>
    public void SaveStepAttempt(object body)
    {
        if (!EnsureEnvironment()) return;

        var wrapper = new
        {
            stepKey = CurrentStepKey,
            theme = Theme.ToString(),
            problemIndex = ProblemIndex,
            body
        };

        string contentJson = JsonUtility.ToJson(wrapper);

        var attempt = new Attempt
        {
            SessionId = SessionId,
            UserEmail = UserEmail,
            Content = contentJson,
            ProblemId = string.IsNullOrEmpty(ProblemId) ? null : ProblemId,
            Theme = Theme,
            ProblemIndex = ProblemIndex
        };

        DataService.Instance.User.SaveAttempt(attempt);
    }

    /// <summary>
    /// Attempt + 인벤토리 보상을 함께 처리하는 헬퍼.
    /// body에는 이 스텝 전용 데이터 구조만 넣어주면 된다.
    /// </summary>
    public void SaveReward(object body, string itemId, string itemName)
    {
        if (!EnsureEnvironment()) return;

        // 1) Attempt 로그
        SaveStepAttempt(body);

        // 2) 인벤토리 지급
        var invItem = new InventoryItem
        {
            UserEmail = UserEmail,
            ItemId = itemId,
            ItemName = itemName,
            Theme = Theme,
            AcquiredAt = DateTime.UtcNow
        };

        DataService.Instance.User.GrantInventoryItem(UserEmail, invItem);
    }
}
