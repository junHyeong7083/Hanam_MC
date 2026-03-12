using System;
using LiteDB;

/// <summary>
/// IDBGateway - LiteDB 데이터베이스 접근 인터페이스
///
/// 【역할】 LiteDatabase 인스턴스를 빌려서 사용하는 패턴(Loan Pattern)을 정의한다.
///          모든 Repository가 이 인터페이스를 통해 DB에 접근한다.
/// 【참조하는 곳】 모든 Repository (UserRepository, ProgressRepository, InventoryRepository 등)
/// 【참조되는 곳】 DBGateway (구현체), DBHelper (실제 연결)
/// </summary>
public interface IDBGateway
{
    /// <summary>DB 연결을 열고 함수를 실행한 후 결과를 반환. using 블록으로 자동 해제됨</summary>
    T WithDb<T>(Func<LiteDatabase, T> func);
    /// <summary>DB 연결을 열고 액션을 실행. 반환값 없는 버전 (Insert, Update, Delete 등)</summary>
    void WithDb(Action<LiteDatabase> action);
}


/// <summary>
/// DBGateway.Core - IDBGateway 인터페이스의 구현부 (partial 클래스)
///
/// 【역할】 LiteDB(mc.db) 파일에 대한 실제 접근을 DBHelper에 위임한다.
///          모든 Repository는 이 클래스의 WithDb()를 통해 DB 커넥션을 얻는다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, 각 Repository 생성자에 주입됨
/// 【참조되는 곳】 DBHelper.With() (LiteDB 커넥션 풀/생성)
/// 【흐름】 Repository.메서드() → DBGateway.WithDb(lambda) → DBHelper.With(lambda) → LiteDatabase
/// </summary>
public partial class DBGateway : IDBGateway
{
    /// <summary>
    /// DB 커넥션을 열고 전달된 함수를 실행하여 결과를 반환한다.
    /// 내부적으로 DBHelper.With()를 호출하며, using 블록으로 커넥션이 자동 해제된다.
    /// </summary>
    public T WithDb<T>(Func<LiteDatabase, T> func)
    {
        return DBHelper.With(func);
    }

    /// <summary>
    /// DB 커넥션을 열고 전달된 액션을 실행한다. 반환값이 없는 쓰기 작업용.
    /// </summary>
    public void WithDb(Action<LiteDatabase> action)
    {
        DBHelper.With(action);
    }

}
