using System;
using System.Linq;

// 결과(ResultDoc), 피드백(Feedback) 관련 DB 접근
public partial class DBGateway
{
    public void InsertResult(ResultDoc result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        WithDb(db =>
        {
            var col = db.GetCollection<ResultDoc>(CResults);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.UserId);
            col.EnsureIndex(x => x.Stage);
            col.Insert(result);
        });
    }

    public ResultDoc[] GetResultsByUser(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Array.Empty<ResultDoc>();

        return WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);
            var results = db.GetCollection<ResultDoc>(CResults);

            users.EnsureIndex(x => x.Email, true);
            results.EnsureIndex(x => x.UserId);

            var user = users.FindOne(u => u.Email == userEmail);
            if (user == null) return Array.Empty<ResultDoc>();

            var q = results.Find(r => r.UserId == user.Id)
                           .OrderBy(r => r.CreatedAt);
            return q.ToArray();
        });
    }

    public ResultDoc GetResultById(string resultId)
    {
        if (string.IsNullOrWhiteSpace(resultId)) return null;

        return WithDb(db =>
        {
            var col = db.GetCollection<ResultDoc>(CResults);
            col.EnsureIndex(x => x.Id, true);
            return col.FindById(resultId);
        });
    }

    public void InsertFeedback(Feedback feedback)
    {
        if (feedback == null) throw new ArgumentNullException(nameof(feedback));

        WithDb(db =>
        {
            var col = db.GetCollection<Feedback>(CFeedback);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.ResultId);
            col.Insert(feedback);
        });
    }

    public Feedback[] GetFeedbacksByResult(string resultId)
    {
        if (string.IsNullOrWhiteSpace(resultId))
            return Array.Empty<Feedback>();

        return WithDb(db =>
        {
            var col = db.GetCollection<Feedback>(CFeedback);
            col.EnsureIndex(x => x.ResultId);
            var q = col.Find(f => f.ResultId == resultId)
                       .OrderBy(f => f.CreatedAt);
            return q.ToArray();
        });
    }
}
