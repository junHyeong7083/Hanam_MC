using System;
using UnityEngine;

/// <summary>
/// 관리자용 데이터 서비스 (검색 / 결과 조회 / 피드백 등록).
/// 실제 DB 접근은 User/Result/Feedback Repository를 통해서만 한다.
/// </summary>
public interface IAdminDataService
{
    // 사용자 검색 (이메일/이름 일부 일치)
    Result<UserSummary[]> SearchUsers(string query);

    // 특정 사용자 결과 목록
    Result<ResultDoc[]> FetchResultsByUser(string userEmail);

    // 결과에 대한 피드백 등록
    Result SubmitFeedback(string resultId, Feedback feedback);
}

public class LocalAdminDataService : IAdminDataService
{
    private readonly IUserRepository _users;
    private readonly IResultRepository _results;
    private readonly IFeedbackRepository _feedback;

    public LocalAdminDataService(
        IUserRepository users,
        IResultRepository results,
        IFeedbackRepository feedback)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
    }

    public Result<UserSummary[]> SearchUsers(string query)
    {
        try
        {
            var items = _users
                .SearchUsersFriendly(query ?? string.Empty)
                ?? Array.Empty<UserSummary>();

            return Result<UserSummary[]>.Success(items);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalAdminData] SearchUsers: {e}");
            return Result<UserSummary[]>.Fail(AuthError.Internal);
        }
    }

    public Result<ResultDoc[]> FetchResultsByUser(string userEmail)
    {
        try
        {
            var items = _results.GetResultsByUser(userEmail) ?? Array.Empty<ResultDoc>();
            return Result<ResultDoc[]>.Success(items);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalAdminData] FetchResultsByUser: {e}");
            return Result<ResultDoc[]>.Fail(AuthError.Internal);
        }
    }

    public Result SubmitFeedback(string resultId, Feedback feedback)
    {
        if (string.IsNullOrWhiteSpace(resultId) || feedback == null)
            return Result.Fail(AuthError.Internal, "Invalid feedback");

        try
        {
            feedback.ResultId = resultId;
            if (string.IsNullOrEmpty(feedback.Id))
                feedback.Id = Guid.NewGuid().ToString();
            if (feedback.CreatedAt == default)
                feedback.CreatedAt = DateTime.UtcNow;

            _feedback.InsertFeedback(feedback);
            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalAdminData] SubmitFeedback: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }
}
