using System;
using System.Linq;
using LiteDB;
using UnityEngine;

public interface IUserDataService
{
    // 나의 진행 요약
    Result<UserProgress> FetchProgress(string userEmail);

    // 문제 데이터 조회
    Result<Problem> FetchProblem(string problemId);

    // 제출 저장(시도/답안 등)
    Result SaveAttempt(Attempt attempt);

    // 결과 조회(세션 기준)
    Result<ResultDoc> FetchResult(string sessionId);

    // 특정 유저가 특정 테마에서 이미 푼 문제 번호들(1~10)
    Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme);
}

public class LocalUserDataService : IUserDataService
{
    // 컬렉션 이름
    const string CProblems = "problems";
    const string CSessions = "sessions";
    const string CResults = "results";
    const string CAttempts = "attempts";

    /// <summary>
    /// 유저별 전체 진행 요약
    /// </summary>
    public Result<UserProgress> FetchProgress(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Result<UserProgress>.Fail(AuthError.Internal);

        try
        {
            var progress = DBHelper.With(db =>
            {
                var sessions = db.GetCollection<SessionRecord>(CSessions);
                var results = db.GetCollection<ResultDoc>(CResults);

                var mySessions = sessions.Find(s => s.UserEmail == userEmail).ToArray();
                var myResults = results.Find(r => r.UserId == userEmail).ToArray();

                return new UserProgress
                {
                    UserEmail = userEmail,
                    TotalSessions = mySessions.Length,
                    TotalSolved = myResults.Length,
                    LastSessionAt = mySessions
                        .OrderByDescending(s => s.CreatedAt)
                        .FirstOrDefault()
                        ?.CreatedAt
                };
            });

            return Result<UserProgress>.Success(progress);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProgress: {e}");
            return Result<UserProgress>.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 문제 데이터 조회
    /// </summary>
    public Result<Problem> FetchProblem(string problemId)
    {
        if (string.IsNullOrWhiteSpace(problemId))
            return Result<Problem>.Fail(AuthError.Internal);

        try
        {
            var problem = DBHelper.With(db =>
            {
                var col = db.GetCollection<Problem>(CProblems);
                return col.FindById(problemId);
            });

            if (problem == null)
                return Result<Problem>.Fail(AuthError.NotFoundOrInactive);

            return Result<Problem>.Success(problem);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProblem: {e}");
            return Result<Problem>.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 시도/답안 저장
    /// </summary>
    public Result SaveAttempt(Attempt attempt)
    {
        if (attempt == null || string.IsNullOrWhiteSpace(attempt.UserEmail))
            return Result.Fail(AuthError.Internal);

        try
        {
            DBHelper.With(db =>
            {
                var col = db.GetCollection<Attempt>(CAttempts);

                // 인덱스 설정
                col.EnsureIndex(x => x.Id, true);
                col.EnsureIndex(x => x.SessionId);
                col.EnsureIndex(x => x.UserEmail);
                col.EnsureIndex(x => x.Theme);
                col.EnsureIndex(x => x.ProblemIndex);

                if (string.IsNullOrEmpty(attempt.Id))
                    attempt.Id = ObjectId.NewObjectId().ToString();

                attempt.CreatedAt = DateTime.UtcNow;

                col.Insert(attempt);
            });

            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] SaveAttempt: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 특정 세션 기준 결과 조회
    /// (지금은 단순히 ResultDoc.Id == sessionId 인 것만 찾는 형태로 둠)
    /// </summary>
    public Result<ResultDoc> FetchResult(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return Result<ResultDoc>.Fail(AuthError.Internal);

        try
        {
            var result = DBHelper.With(db =>
            {
                var col = db.GetCollection<ResultDoc>(CResults);
                return col.FindById(sessionId);
            });

            if (result == null)
                return Result<ResultDoc>.Fail(AuthError.NotFoundOrInactive);

            return Result<ResultDoc>.Success(result);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchResult: {e}");
            return Result<ResultDoc>.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 특정 유저가 특정 테마에서 이미 푼 문제 번호들(1~10)을 반환
    ///  - Director 테마에서 1,2번 풀었으면 [1,2] 반환
    ///  - 이후 패널에서
    ///      - 한 바퀴 다 돌기 전 → "다음 문제"만 열어주기
    ///      - 1~10 다 풀었으면 → 1~10 전부 다시 선택 가능
    /// </summary>
    public Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Result<int[]>.Fail(AuthError.Internal);

        try
        {
            var solved = DBHelper.With(db =>
            {
                var col = db.GetCollection<Attempt>(CAttempts);

                return col.Find(a =>
                            a.UserEmail == userEmail &&
                            a.Theme == theme
                        )
                        .Where(a => a.ProblemIndex.HasValue)
                        .Select(a => a.ProblemIndex.Value)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
            });

            return Result<int[]>.Success(solved);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchSolvedProblemIndexes: {e}");
            return Result<int[]>.Fail(AuthError.Internal);
        }
    }
}
