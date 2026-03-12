/// <summary>
/// DBGateway - LiteDB(mc.db) 데이터베이스 접근을 위한 게이트웨이 (partial 클래스)
///
/// 【역할】 이 파일은 partial 클래스의 선언부이다. 실제 구현은 DBGateway.Core.cs에 있다.
///          원래 모든 DB 쿼리를 한 파일에 담았으나, 파일이 비대해져 partial로 분리함.
///          현재는 Repository 패턴으로 리팩토링되어 직접 쿼리는 거의 없고,
///          WithDb() 메서드만 IDBGateway 인터페이스로 노출한다.
/// 【참조하는 곳】 DataService (Awake에서 생성), 각 Repository (WithDb를 통해 DB 접근)
/// 【참조되는 곳】 DBHelper (실제 LiteDB 연결 관리)
/// </summary>
public partial class DBGateway
{ }
