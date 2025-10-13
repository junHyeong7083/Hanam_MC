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

    // 결과 조회(세션 기준) — 현재 로컬은 ResultDoc으로 매핑
    Result<ResultDoc> FetchResult(string sessionId);
}

public class LocalUserDataService : IUserDataService
{
    // 컬렉션 이름
    const string CProblems = "problems";
    const string CSessions = "sessions";
    const string CResults = "results";   // ResultDoc 저장
    const string CAttempts = "attempts";

    public Result<UserProgress> FetchProgress(string userEmail)
    {
        try
        {
            var pg = DBHelper.With(db =>
            {
                var sessions = db.GetCollection<SessionRecord>(CSessions);
                var results = db.GetCollection<ResultDoc>(CResults);

                var mySessions = sessions.Find(s => s.UserEmail == userEmail).ToArray();
                var myResults = results.Find(r => r.UserId == userEmail || r.MetaJson.Contains(userEmail)).ToArray(); // 필요시 수정

                return new UserProgress
                {
                    UserEmail = userEmail,
                    TotalSessions = mySessions.Length,
                    TotalSolved = myResults.Length,
                    LastSessionAt = mySessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.CreatedAt
                };
            });
            return Result<UserProgress>.Success(pg);
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
            var p = DBHelper.With(db => db.GetCollection<Problem>(CProblems).FindById(problemId));
            return p != null ? Result<Problem>.Success(p) : Result<Problem>.Fail(AuthError.NotFoundOrInactive);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProblem: {e}");
            return Result<Problem>.Fail(AuthError.Internal);
        }
    }

    public Result SaveAttempt(Attempt attempt)
    {
        try
        {
            attempt.Id ??= ObjectId.NewObjectId().ToString();
            attempt.CreatedAt = DateTime.UtcNow;

            DBHelper.With(db =>
            {
                var col = db.GetCollection<Attempt>(CAttempts);
                col.EnsureIndex(x => x.Id, true);
                col.EnsureIndex(x => x.SessionId);
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

    public Result<ResultDoc> FetchResult(string sessionId)
    {
        try
        {
            var r = DBHelper.With(db =>
                db.GetCollection<ResultDoc>(CResults).FindOne(x => x.MetaJson.Contains(sessionId) || x.Id == sessionId) // 임시 조건
            );
            return r != null ? Result<ResultDoc>.Success(r) : Result<ResultDoc>.Fail(AuthError.NotFoundOrInactive);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchResult: {e}");
            return Result<ResultDoc>.Fail(AuthError.Internal);
        }
    }
}
