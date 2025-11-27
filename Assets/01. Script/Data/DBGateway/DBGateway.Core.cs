using System;
using LiteDB;

public interface IDBGateway
{
    T WithDb<T>(Func<LiteDatabase, T> func);
    void WithDb(Action<LiteDatabase> action);
}


// LiteDB(mc.db)에 대한 공통 헬퍼 및 컬렉션 이름 정의.
public partial class DBGateway : IDBGateway
{
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
