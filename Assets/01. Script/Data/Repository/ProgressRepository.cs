using System;
using System.Linq;

public interface IProgressRepository
{
    UserProgress GetUserProgress(string userEmail);
    void InsertAttempt(Attempt attempt);
    int[] GetSolvedProblemIndexes(string userEmail, string theme = null);
}


public class ProgressRepository : IProgressRepository
{
    private readonly IDBGateway _db;
    private const string CUsers = "users";
    private const string CSessions = "sessions";
    private const string CResults = "results";
    private const string CAttempts = "attempts";

    public ProgressRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

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

