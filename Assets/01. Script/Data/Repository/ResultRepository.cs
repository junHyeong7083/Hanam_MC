using System;
using System.Linq;

/// <summary>
/// IResultRepository - 문제 풀이 결과(ResultDoc) 데이터 접근 인터페이스
///
/// 【역할】 ResultDoc 엔티티의 CRUD 기능을 정의한다.
/// 【참조하는 곳】 LocalProgressService (결과 저장/조회), LocalAdminDataService (사용자별 결과 조회),
///                LocalResultQueryService (결과 ID 조회)
/// </summary>
public interface IResultRepository
{
    /// <summary>결과를 DB에 저장한다</summary>
    void InsertResult(ResultDoc result);
    /// <summary>기존 결과를 수정한다</summary>
    void UpdateResult(ResultDoc result);
    /// <summary>특정 사용자의 모든 결과를 조회한다 (시간순 정렬)</summary>
    ResultDoc[] GetResultsByUser(string userEmail);
    /// <summary>결과 ID로 ResultDoc을 조회한다</summary>
    ResultDoc GetResultById(string resultId);
}

/// <summary>
/// ResultRepository - IResultRepository의 LiteDB 구현체
///
/// 【역할】 LiteDB "results" 컬렉션에 대한 CRUD 작업을 수행한다.
///          사용자별 결과 조회 시 users 컬렉션과 크로스 조회한다 (Email → UserId → Results).
/// 【참조하는 곳】 DataService.Awake()에서 생성, LocalProgressService/LocalResultQueryService에서 사용
/// 【참조되는 곳】 IDBGateway (DB 커넥션)
/// 【사용 컬렉션】 "results", "users" (크로스 조회)
/// </summary>
public class ResultRepository : IResultRepository
{
    /// <summary>DB 접근 게이트웨이</summary>
    private readonly IDBGateway _db;
    /// <summary>users 컬렉션명 (Email → UserId 변환용)</summary>
    private const string CUsers = "users";
    /// <summary>results 컬렉션명</summary>
    private const string CResults = "results";

    public ResultRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// 결과를 DB에 저장한다. Id(유니크), UserId, Theme, ProblemIndex에 인덱스를 생성한다.
    /// </summary>
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

    /// <summary>기존 결과를 수정한다. LiteDB의 Update()를 사용하여 전체 문서를 교체한다.</summary>
    public void UpdateResult(ResultDoc result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<ResultDoc>(CResults);
            col.Update(result);
        });
    }

    /// <summary>
    /// 특정 사용자의 모든 결과를 시간순(CreatedAt)으로 조회한다.
    /// users 컬렉션에서 Email로 User를 찾고, UserId로 results를 검색한다.
    /// </summary>
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

    /// <summary>결과 ID(GUID)로 ResultDoc을 조회한다. 존재하지 않으면 null 반환.</summary>
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
