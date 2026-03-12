using System;
using System.Linq;

/// <summary>
/// IProgressRepository - 사용자 진행도/시도 기록 데이터 접근 인터페이스
///
/// 【역할】 사용자 진행도 요약 조회, Attempt 저장, 풀이 완료 문제 번호 조회 기능을 정의한다.
/// 【참조하는 곳】 LocalProgressService, LocalUserDataService
/// </summary>
public interface IProgressRepository
{
    /// <summary>사용자 진행도 요약을 조회한다 (sessions/results 컬렉션 집계)</summary>
    UserProgress GetUserProgress(string userEmail);
    /// <summary>Attempt(시도) 기록을 DB에 저장한다</summary>
    void InsertAttempt(Attempt attempt);
    /// <summary>사용자가 풀이 완료한 문제 번호 목록을 조회한다 (테마 필터 선택적)</summary>
    int[] GetSolvedProblemIndexes(string userEmail, string theme = null);
}


/// <summary>
/// ProgressRepository - IProgressRepository의 LiteDB 구현체
///
/// 【역할】 사용자 진행도 관련 데이터를 LiteDB에서 조회/저장한다.
///          여러 컬렉션(users, sessions, results, attempts)을 크로스 조회하여
///          진행도 요약(UserProgress)을 조립한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, LocalProgressService에서 사용
/// 【참조되는 곳】 IDBGateway (DB 커넥션)
/// 【사용 컬렉션】 "users", "sessions", "results", "attempts"
/// </summary>
public class ProgressRepository : IProgressRepository
{
    /// <summary>DB 접근 게이트웨이</summary>
    private readonly IDBGateway _db;
    /// <summary>users 컬렉션명 (User 조회용)</summary>
    private const string CUsers = "users";
    /// <summary>sessions 컬렉션명 (세션 수 집계용)</summary>
    private const string CSessions = "sessions";
    /// <summary>results 컬렉션명 (풀이 완료 조회용)</summary>
    private const string CResults = "results";
    /// <summary>attempts 컬렉션명 (시도 기록 저장용)</summary>
    private const string CAttempts = "attempts";

    public ProgressRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// 사용자 진행도 요약을 조회한다.
    /// sessions 컬렉션에서 총 세션 수와 마지막 세션 시각을,
    /// results 컬렉션에서 총 풀이 완료 수를 집계하여 UserProgress DTO로 반환한다.
    /// </summary>
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

        return _db.WithDb(db =>
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

    /// <summary>
    /// Attempt(시도) 기록을 DB에 저장한다.
    /// Id(유니크), UserId, UserEmail, ProblemId, Theme, ProblemIndex에 인덱스를 생성한다.
    /// </summary>
    public void InsertAttempt(Attempt attempt)
    {
        if (attempt == null) throw new ArgumentNullException(nameof(attempt));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<Attempt>(CAttempts);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.UserId);
            col.EnsureIndex(x => x.UserEmail);
            col.EnsureIndex(x => x.ProblemId);
            col.EnsureIndex(x => x.Theme);
            col.EnsureIndex(x => x.ProblemIndex);

            col.Insert(attempt);
        });
    }

    /// <summary>
    /// 사용자가 풀이 완료한 문제 번호 목록을 오름차순으로 반환한다.
    /// theme이 null이면 모든 테마의 결과를, 지정하면 해당 테마만 필터링한다.
    /// users → results 크로스 조회 후, ProblemIndex를 Distinct하여 반환.
    /// </summary>
    public int[] GetSolvedProblemIndexes(string userEmail, string theme = null)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Array.Empty<int>();

        return _db.WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);
            var results = db.GetCollection<ResultDoc>(CResults);

            users.EnsureIndex(x => x.Email, true);
            results.EnsureIndex(x => x.UserId);
            results.EnsureIndex(x => x.ProblemIndex);
            results.EnsureIndex(x => x.Theme);

            var user = users.FindOne(u => u.Email == userEmail);
            if (user == null) return Array.Empty<int>();

            var q = results.Find(r =>
                r.UserId == user.Id &&
                (string.IsNullOrEmpty(theme) || r.Theme == theme)
            );

            var indexes = q.Select(r => r.ProblemIndex)
                           .Distinct()
                           .OrderBy(i => i)
                           .ToArray();
            return indexes;
        });
    }
}

