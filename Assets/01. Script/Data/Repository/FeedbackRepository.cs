using System;
using System.Linq;

/// <summary>
/// IFeedbackRepository - 관리자 피드백 데이터 접근 인터페이스
///
/// 【역할】 Feedback 엔티티의 CRUD 인터페이스를 정의한다.
/// 【참조하는 곳】 LocalAdminDataService (피드백 저장/조회)
/// </summary>
public interface IFeedbackRepository
{
    /// <summary>피드백을 DB에 저장한다</summary>
    void InsertFeedback(Feedback feedback);
    /// <summary>특정 ResultDoc에 달린 모든 피드백을 조회한다 (시간순 정렬)</summary>
    Feedback[] GetFeedbacksByResult(string resultId);
}

/// <summary>
/// FeedbackRepository - IFeedbackRepository의 LiteDB 구현체
///
/// 【역할】 LiteDB "feedback" 컬렉션에 대한 CRUD 작업을 수행한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, LocalAdminDataService에서 사용
/// 【참조되는 곳】 IDBGateway (DB 커넥션)
/// 【컬렉션명】 "feedback"
/// </summary>
public class FeedbackRepository : IFeedbackRepository
{
    /// <summary>DB 접근 게이트웨이</summary>
    private readonly IDBGateway _db;
    /// <summary>LiteDB 컬렉션명</summary>
    private const string CFeedback = "feedback";

    public FeedbackRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// 피드백을 DB에 저장한다. Id(유니크)와 ResultId에 인덱스를 생성한다.
    /// </summary>
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

    /// <summary>
    /// 특정 ResultDoc ID에 달린 모든 피드백을 시간순(CreatedAt)으로 조회한다.
    /// resultId가 비어있으면 빈 배열을 반환한다.
    /// </summary>
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
