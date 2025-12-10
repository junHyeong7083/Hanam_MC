using System;
public interface IProblemRepository
{
    Problem GetProblemById(string problemId);
    Problem GetProblemByThemeAndIndex(ProblemTheme theme, int index);
}
public class ProblemRepository : IProblemRepository
{
    private readonly IDBGateway _db;
    private const string CProblems = "problems";

    public ProblemRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Problem GetProblemById(string problemId)
    {
        if (string.IsNullOrWhiteSpace(problemId)) return null;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<Problem>(CProblems);
            col.EnsureIndex(x => x.Id, true);
            return col.FindById(problemId);
        });
    }

    public Problem GetProblemByThemeAndIndex(ProblemTheme theme, int index)
    {
        if (index <= 0)
            return null;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<Problem>(CProblems);
            col.EnsureIndex(x => x.Theme);
            col.EnsureIndex(x => x.Index);
            return col.FindOne(p => p.Theme == theme && p.Index == index);
        });
    }
}
