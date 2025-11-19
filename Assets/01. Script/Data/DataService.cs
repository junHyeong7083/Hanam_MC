using UnityEngine;

/// <summary>
/// 앱 전체에서 사용하는 데이터/서비스 싱글톤 허브.
/// - 씬 어디서나 DataService.Instance를 통해 Auth/User/Admin 서비스 접근
/// - 실제 DB 접근은 DbGateway 내부에서만 수행된다.
/// </summary>
public class DataService : MonoBehaviour
{
    public static DataService Instance { get; private set; }

    [SerializeField] bool useRemote = false;
    [SerializeField] string baseUrl = "https://api.example.com";

    public DBGateway Db { get; private set; }
    public IUserRepository UserRepo { get; private set; }
    public IAuthService Auth { get; private set; }
    public IUserDataService User { get; private set; }
    public IAdminDataService Admin { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 현재는 로컬 DB만 사용.
        // 나중에 useRemote가 true일 때는 HTTP 기반 서비스로 갈아끼우면 됨.
        Db = new DBGateway();
        UserRepo = new UserRepository(Db);
        Auth = new AuthService(UserRepo);
        User = new LocalUserDataService(Db);
        Admin = new LocalAdminDataService(Db, UserRepo);
    }
}
