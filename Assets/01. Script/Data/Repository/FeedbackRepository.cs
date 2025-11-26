using System;
using System.Linq;
public interface IFeedbackRepository
{
    void InsertFeedback(Feedback feedback);
    Feedback[] GetFeedbacksByResult(string resultId);
}

public class FeedbackRepository : IFeedbackRepository
{
    private readonly IDBGateway _db;
    private const string CFeedback = "feedback";

    public FeedbackRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public void InsertFeedback(Feedback feedback)
    {
        if (feedback == null) throw new ArgumentNullException(nameof(feedback));

        _db.WithDb(db =>
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

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<Feedback>(CFeedback);
            col.EnsureIndex(x => x.ResultId);
            var q = col.Find(f => f.ResultId == resultId)
                       .OrderBy(f => f.CreatedAt);
            return q.ToArray();
        });
    }
}
