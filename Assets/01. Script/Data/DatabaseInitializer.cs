using LiteDB;
using UnityEngine;

/// <summary>
/// 데이터베이스 인덱스 초기화
/// - 앱 시작 시 한 번만 호출하여 모든 컬렉션의 인덱스 생성
/// - 각 Repository에서 매번 EnsureIndex 호출하는 것보다 효율적
/// </summary>
public static class DatabaseInitializer
{
    private static bool _initialized = false;

    /// <summary>
    /// 모든 컬렉션의 인덱스를 초기화합니다.
    /// Bootstrap에서 앱 시작 시 한 번 호출하세요.
    /// </summary>
    public static void InitializeIndexes()
    {
        if (_initialized) return;

        DBHelper.With(db =>
        {
            InitializeUserIndexes(db);
            InitializeProblemIndexes(db);
            InitializeResultIndexes(db);
            InitializeAttemptIndexes(db);
            InitializeProgressIndexes(db);
            InitializeInventoryIndexes(db);
            InitializeSessionIndexes(db);
        });

        _initialized = true;
        Debug.Log("[DatabaseInitializer] 모든 인덱스 초기화 완료");
    }

    private static void InitializeUserIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<User>("users");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.Email, true);
        col.EnsureIndex(x => x.Role);
        col.EnsureIndex(x => x.Name);
        col.EnsureIndex(x => x.LowerName);
        col.EnsureIndex(x => x.NameChosung);
    }

    private static void InitializeProblemIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<Problem>("problems");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.Theme);
        col.EnsureIndex(x => x.Index);
    }

    private static void InitializeResultIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<ResultDoc>("results");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.Theme);
        col.EnsureIndex(x => x.ProblemIndex);
    }

    private static void InitializeAttemptIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<Attempt>("attempts");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.UserEmail);
        col.EnsureIndex(x => x.ProblemId);
        col.EnsureIndex(x => x.Theme);
    }

    private static void InitializeProgressIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<UserProgress>("progress");
        col.EnsureIndex(x => x.UserEmail, true);
    }

    private static void InitializeInventoryIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<InventoryItem>("inventory");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.UserEmail);
        col.EnsureIndex(x => x.ItemId);
    }

    private static void InitializeSessionIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<SessionRecord>("sessions");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.UserEmail);
    }
}
