using System;
using UnityEngine;

/// <summary>
/// IAdminDataService - 관리자용 데이터 서비스 인터페이스
///
/// 【역할】 관리자가 사용하는 기능 정의: 사용자 검색, 결과 조회, 피드백 저장.
/// 【참조하는 곳】 관리자 UI 컨트롤러 (현재 미구현)
/// 【참조되는 곳】 User/Result/Feedback Repository
/// </summary>
public interface IAdminDataService
{
    /// <summary>사용자 검색 (이메일/이름 부분 일치)</summary>
    Result<UserSummary[]> SearchUsers(string query);

    /// <summary>특정 사용자의 모든 풀이 결과 조회</summary>
    Result<ResultDoc[]> FetchResultsByUser(string userEmail);

    /// <summary>결과에 대한 관리자 피드백 저장</summary>
    Result SubmitFeedback(string resultId, Feedback feedback);
}

/// <summary>
/// LocalAdminDataService - IAdminDataService의 로컬(LiteDB) 구현체
///
/// 【역할】 관리자 전용 데이터 조회/저장 기능을 제공한다.
///          사용자 검색, 결과 목록 조회, 피드백 작성이 가능하다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, 관리자 UI (미구현)
/// 【참조되는 곳】 IUserRepository (사용자 검색), IResultRepository (결과 조회),
///                IFeedbackRepository (피드백 저장)
/// 【흐름】 DataService.Instance.Admin.SearchUsers(query)
///         → UserRepository.SearchUsersFriendly(query) → UserSummary[] 반환
/// </summary>
public class LocalAdminDataService : IAdminDataService
{
    /// <summary>사용자 데이터 조회/검색용 Repository</summary>
    private readonly IUserRepository _users;
    /// <summary>결과 데이터 조회용 Repository</summary>
    private readonly IResultRepository _results;
    /// <summary>피드백 저장용 Repository</summary>
    private readonly IFeedbackRepository _feedback;

    /// <summary>
    /// 생성자. DataService.Awake()에서 Repository들을 주입받아 생성된다.
    /// </summary>
    public LocalAdminDataService(
        IUserRepository users,
        IResultRepository results,
        IFeedbackRepository feedback)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
    }

    /// <summary>
    /// 사용자 검색. 이메일 또는 이름이 query 문자열을 포함하는 사용자 목록을 반환한다.
    /// 빈 query를 전달하면 전체 사용자를 반환한다.
    /// </summary>
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

    /// <summary>
    /// 특정 사용자의 모든 문제 풀이 결과를 조회한다.
    /// userEmail로 User를 찾고, 해당 UserId의 ResultDoc들을 반환한다.
    /// </summary>
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

    /// <summary>
    /// 특정 결과(ResultDoc)에 대한 관리자 피드백을 저장한다.
    /// Feedback.Id와 CreatedAt이 비어있으면 자동으로 채운다.
    /// </summary>
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
