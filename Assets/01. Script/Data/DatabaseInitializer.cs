using LiteDB;
using UnityEngine;

/// <summary>
/// DatabaseInitializer - 데이터베이스 인덱스 일괄 초기화 유틸리티
///
/// 【역할】 앱 시작 시 한 번만 호출되어 LiteDB의 모든 컬렉션에 필요한 인덱스를 생성한다.
///          각 Repository에서 매번 EnsureIndex를 호출하는 것보다 효율적이고 일관성 있다.
///          인덱스가 이미 존재하면 LiteDB가 내부적으로 무시하므로 중복 호출해도 안전하다.
/// 【참조하는 곳】 Bootstrap.Awake() (앱 최초 시작 시 한 번 호출)
/// 【참조되는 곳】 DBHelper (DB 커넥션), 각 Model 클래스 (User, ResultDoc, Problem 등)
/// 【흐름】 Bootstrap.Awake() → DatabaseInitializer.InitializeIndexes()
///         → DBHelper.With()로 DB 열기 → 7개 컬렉션 인덱스 생성
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>중복 초기화 방지 플래그. 앱 생명주기 동안 한 번만 true가 됨</summary>
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

    /// <summary>users 컬렉션 인덱스: Id(유니크), Email(유니크), Role, Name, LowerName, NameChosung</summary>
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

    /// <summary>problems 컬렉션 인덱스: Id(유니크), Theme, Index</summary>
    private static void InitializeProblemIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<Problem>("problems");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.Theme);
        col.EnsureIndex(x => x.Index);
    }

    /// <summary>results 컬렉션 인덱스: Id(유니크), UserId, Theme, ProblemIndex</summary>
    private static void InitializeResultIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<ResultDoc>("results");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.Theme);
        col.EnsureIndex(x => x.ProblemIndex);
    }

    /// <summary>attempts 컬렉션 인덱스: Id(유니크), UserId, UserEmail, ProblemId, Theme</summary>
    private static void InitializeAttemptIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<Attempt>("attempts");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.UserEmail);
        col.EnsureIndex(x => x.ProblemId);
        col.EnsureIndex(x => x.Theme);
    }

    /// <summary>progress 컬렉션 인덱스: UserEmail(유니크)</summary>
    private static void InitializeProgressIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<UserProgress>("progress");
        col.EnsureIndex(x => x.UserEmail, true);
    }

    /// <summary>inventory 컬렉션 인덱스: Id(유니크), UserId, UserEmail, ItemId</summary>
    private static void InitializeInventoryIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<InventoryItem>("inventory");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.UserEmail);
        col.EnsureIndex(x => x.ItemId);
    }

    /// <summary>sessions 컬렉션 인덱스: Id(유니크), UserId, UserEmail</summary>
    private static void InitializeSessionIndexes(LiteDatabase db)
    {
        var col = db.GetCollection<SessionRecord>("sessions");
        col.EnsureIndex(x => x.Id, true);
        col.EnsureIndex(x => x.UserId);
        col.EnsureIndex(x => x.UserEmail);
    }
}
