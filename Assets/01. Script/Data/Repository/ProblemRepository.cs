using System;

/// <summary>
/// IProblemRepository - 문제(Problem) 데이터 접근 인터페이스
///
/// 【역할】 Problem 엔티티의 조회 기능을 정의한다.
/// 【참조하는 곳】 LocalProblemQueryService, LocalUserDataService
/// </summary>
public interface IProblemRepository
{
    /// <summary>문제 ID로 Problem을 조회한다</summary>
    Problem GetProblemById(string problemId);
    /// <summary>테마와 문제 번호로 Problem을 조회한다</summary>
    Problem GetProblemByThemeAndIndex(ProblemTheme theme, int index);
}

/// <summary>
/// ProblemRepository - IProblemRepository의 LiteDB 구현체
///
/// 【역할】 LiteDB "problems" 컬렉션에서 문제 데이터를 조회한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, LocalProblemQueryService에서 사용
/// 【참조되는 곳】 IDBGateway (DB 커넥션)
/// 【컬렉션명】 "problems"
/// </summary>
public class ProblemRepository : IProblemRepository
{
    /// <summary>DB 접근 게이트웨이</summary>
    private readonly IDBGateway _db;
    /// <summary>LiteDB 컬렉션명</summary>
    private const string CProblems = "problems";

    public ProblemRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// 문제 ID(GUID)로 Problem을 조회한다. 존재하지 않으면 null 반환.
    /// </summary>
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

    /// <summary>
    /// 테마와 문제 번호(1~10)로 Problem을 조회한다.
    /// index가 0 이하이면 null을 반환한다.
    /// </summary>
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
