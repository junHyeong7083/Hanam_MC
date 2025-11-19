using System;
using LiteDB;

// LiteDB(mc.db)에 대한 공통 헬퍼 및 컬렉션 이름 정의.
public partial class DBGateway
{
    // 컬렉션 이름 상수
    const string CUsers = "users";
    const string CSessions = "sessions";
    const string CResults = "results";
    const string CAttempts = "attempts";
    const string CProblems = "problems";
    const string CFeedback = "feedback";

    // ─────────────────────────────
    // 공용 헬퍼 (DBHelper 래핑)
    // ─────────────────────────────

    public T WithDb<T>(Func<LiteDatabase, T> func)
    {
        return DBHelper.With(func);
    }

    public void WithDb(Action<LiteDatabase> action)
    {
        DBHelper.With(action);
    }
}
