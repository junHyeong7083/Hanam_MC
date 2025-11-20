using System;
using UnityEngine;

/// <summary>
/// 사용자 문제 풀이 / 진행도용 로컬 데이터 서비스.
/// 실제 DB 접근은 DbGateway를 통해서만 한다.
/// </summary>
public interface IUserDataService
{
    // 나의 진행 요약
    Result<UserProgress> FetchProgress(string userEmail);

    // 문제 데이터 조회 (ID 기준)
    Result<Problem> FetchProblem(string problemId);

    // 제출 저장(시도/답안 등)
    Result SaveAttempt(Attempt attempt);

    // 결과 조회(세션 또는 ResultId 기준)
    Result<ResultDoc> FetchResult(string resultIdOrSessionId);

    // 사용자가 푼 문제 번호 목록
    Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme);
}

public class LocalUserDataService : IUserDataService
{
    readonly DBGateway _db;

    public LocalUserDataService(DBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Result<UserProgress> FetchProgress(string userEmail)
    {
        try
        {
            var progress = _db.GetUserProgress(userEmail);
            return Result<UserProgress>.Success(progress);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProgress: {e}");
            return Result<UserProgress>.Fail(AuthError.Internal);
        }
    }

    public Result<Problem> FetchProblem(string problemId)
    {
        try
        {
            var p = _db.GetProblemById(problemId);
            if (p == null)
                return Result<Problem>.Fail(AuthError.NotFoundOrInactive);

            return Result<Problem>.Success(p);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProblem: {e}");
            return Result<Problem>.Fail(AuthError.Internal);
        }
    }

    public Result SaveAttempt(Attempt attempt)
    {
        if (attempt == null)
            return Result.Fail(AuthError.Internal, "Attempt is null");

        try
        {
            if (string.IsNullOrEmpty(attempt.Id))
                attempt.Id = Guid.NewGuid().ToString();
            if (attempt.CreatedAt == default)
                attempt.CreatedAt = DateTime.UtcNow;

            _db.InsertAttempt(attempt);
            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] SaveAttempt: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }

    public Result<ResultDoc> FetchResult(string resultIdOrSessionId)
    {
        if (string.IsNullOrWhiteSpace(resultIdOrSessionId))
            return Result<ResultDoc>.Fail(AuthError.NotFoundOrInactive);

        try
        {
            // 1) Id로 직접 조회
            var r = _db.GetResultById(resultIdOrSessionId);
            if (r != null)
                return Result<ResultDoc>.Success(r);

            // 2) 나중에 필요하면 MetaJson 등에 세션ID를 저장해서 추가 검색 로직 확장 가능
            return Result<ResultDoc>.Fail(AuthError.NotFoundOrInactive);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchResult: {e}");
            return Result<ResultDoc>.Fail(AuthError.Internal);
        }
    }

    public Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme)
    {
        try
        {
            string themeKey = theme.ToString();

            var indexes = _db.GetSolvedProblemIndexes(userEmail, themeKey)
                         ?? Array.Empty<int>();

            return Result<int[]>.Success(indexes);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchSolvedProblemIndexes: {e}");
            return Result<int[]>.Fail(AuthError.Internal);
        }
    }
}
