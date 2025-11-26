using System;
using UnityEngine;

/// <summary>
/// 앱 전체에서 사용하는 데이터/서비스 싱글톤 허브.
/// - 씬 어디서나 DataService.Instance를 통해 Auth/User/Admin 서비스 접근
/// - 실제 DB 접근은 Repository들 내부에서만 수행된다.
/// </summary>
public class DataService : MonoBehaviour
{
    public static DataService Instance { get; private set; }

    [SerializeField] bool useRemote = false;
    [SerializeField] string baseUrl = "https://api.example.com";

    // DbGateway는 여전히 노출 (필요하면 다른 곳에서 직접 사용 가능)
    public DBGateway Db { get; private set; }

    // ===== Repositories =====
    public IInventoryRepository InventoryRepository { get; private set; }
    public IUserRepository UserRepository { get; private set; }
    public IProgressRepository ProgressRepository { get; private set; }
    public IProblemRepository ProblemRepository { get; private set; }
    public IResultRepository ResultRepository { get; private set; }
    public IFeedbackRepository FeedbackRepository { get; private set; }

    // ===== Services =====
    public IAuthService Auth { get; private set; }
    public IUserDataService User { get; private set; }
    public IAdminDataService Admin { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 현재는 항상 로컬 LiteDB 사용.
        // 나중에 useRemote == true면 여기서 HTTP 기반 Repo/Service로 갈아끼우면 됨.
        Db = new DBGateway();

        // ----- Repository 조립 -----
        // DBGateway는 IDbGateway를 구현하고 있어야 함.
        var dbCore = (IDBGateway)Db;

        InventoryRepository = new InventoryRepository(dbCore);
        UserRepository = new UserRepository(dbCore);
        ProgressRepository = new ProgressRepository(dbCore);
        ProblemRepository = new ProblemRepository(dbCore);
        ResultRepository = new ResultRepository(dbCore);
        FeedbackRepository = new FeedbackRepository(dbCore);

        // ----- Service 조립 -----
        Auth = new AuthService(UserRepository);

        User = new LocalUserDataService(
            InventoryRepository,
            UserRepository,
            ProgressRepository,
            ProblemRepository,
            ResultRepository
        );

        Admin = new LocalAdminDataService(
            UserRepository,
            ResultRepository,
            FeedbackRepository
        );
    }
}
