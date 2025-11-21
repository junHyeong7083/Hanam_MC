using System;
using System.Linq;

// 유저 진행도, 세션, 문제, 시도(Attempt) 관련 DB 접근
public partial class DBGateway
{
    public UserProgress GetUserProgress(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return new UserProgress
            {
                UserEmail = userEmail,
                TotalSessions = 0,
                TotalSolved = 0,
                LastSessionAt = null
            };
        }

        return WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);
            var sessions = db.GetCollection<SessionRecord>(CSessions);
            var results = db.GetCollection<ResultDoc>(CResults);

            users.EnsureIndex(x => x.Email, true);
            sessions.EnsureIndex(x => x.UserEmail);
            results.EnsureIndex(x => x.UserId);

            var user = users.FindOne(u => u.Email == userEmail);
            string uid = user?.Id;

            int totalSessions = sessions.Count(s => s.UserEmail == userEmail);
            int totalSolved = 0;
            if (!string.IsNullOrEmpty(uid))
                totalSolved = results.Count(r => r.UserId == uid);

            DateTime? lastSessionAt = null;
            var lastSession = sessions.Find(s => s.UserEmail == userEmail)
                                      .OrderByDescending(s => s.CreatedAt)
                                      .FirstOrDefault();
            if (lastSession != null)
                lastSessionAt = lastSession.CreatedAt;

            return new UserProgress
            {
                UserEmail = userEmail,
                TotalSessions = totalSessions,
                TotalSolved = totalSolved,
                LastSessionAt = lastSessionAt
            };
        });
    }
    public Problem GetProblemById(string problemId)
    {
        if (string.IsNullOrWhiteSpace(problemId)) return null;

        return WithDb(db =>
        {
            var col = db.GetCollection<Problem>(CProblems);
            col.EnsureIndex(x => x.Id, true);
            return col.FindById(problemId);
        });
    }

    /// <summary>
    /// (선택) 테마/번호로 문제를 가져오고 싶을 때 쓸 수 있는 확장용 메서드.
    /// 현재 Problem에 Index 정보가 없으면 나중에 스키마에 맞게 수정해야 함.
    /// </summary>
    public Problem GetProblemByThemeAndIndex(ProblemTheme theme, int index)
    {
        if (index <= 0)
            return null;

        return WithDb(db =>
        {
            var col = db.GetCollection<Problem>(CProblems);
            col.EnsureIndex(x => x.Theme);
            // TODO: Problem에 Stage/Index 필드 생기면 index까지 같이 비교
            return col.FindOne(p => p.Theme == theme);
        });
    }

    public void InsertAttempt(Attempt attempt)
    {
        if (attempt == null) throw new ArgumentNullException(nameof(attempt));

        WithDb(db =>
        {
            var col = db.GetCollection<Attempt>(CAttempts);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.UserEmail);
            col.Insert(attempt);
        });
    }

    /// <summary>
    /// 사용자가 푼 문제 번호(Stage) 목록.
    /// TODO: 테마별로 나누고 싶으면 ResultDoc/Problem 구조를 확장해서 theme 기준 필터 추가.
    /// </summary>
    public int[] GetSolvedProblemIndexes(string userEmail, string theme = null)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Array.Empty<int>();

        return WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);
            var results = db.GetCollection<ResultDoc>(CResults);

            users.EnsureIndex(x => x.Email, true);
            results.EnsureIndex(x => x.UserId);
            results.EnsureIndex(x => x.Stage);
            results.EnsureIndex(x => x.Theme);

            var user = users.FindOne(u => u.Email == userEmail);
            if (user == null) return Array.Empty<int>();

            var q = results.Find(r =>
                r.UserId == user.Id &&
                (string.IsNullOrEmpty(theme) || r.Theme == theme)    
            );

            var indexes = q.Select(r => r.Stage)
                           .Distinct()
                           .OrderBy(i => i)
                           .ToArray();
            return indexes;
        });
    }
}
