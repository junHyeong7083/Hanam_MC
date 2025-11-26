using System;
using System.Linq;
public interface IResultRepository
{
    void InsertResult(ResultDoc result);
    void UpdateResult(ResultDoc result);
    ResultDoc[] GetResultsByUser(string userEmail);
    ResultDoc GetResultById(string resultId);
}

public class ResultRepository : IResultRepository
{
    private readonly IDBGateway _db;
    private const string CUsers = "users";
    private const string CResults = "results";

    public ResultRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void InsertResult(ResultDoc result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<ResultDoc>(CResults);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.UserId);
            col.EnsureIndex(x => x.Theme);
            col.EnsureIndex(x => x.ProblemIndex);

            col.Insert(result);
        });
    }

    public void UpdateResult(ResultDoc result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<ResultDoc>(CResults);
            col.Update(result);
        });
    }

    public ResultDoc[] GetResultsByUser(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return Array.Empty<ResultDoc>();

        return _db.WithDb(db =>
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

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<ResultDoc>(CResults);
            col.EnsureIndex(x => x.Id, true);
            return col.FindById(resultId);
        });
    }
}
