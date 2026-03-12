using System;
using UnityEngine;

/// <summary>
/// DataService - 앱 전체에서 사용하는 데이터/서비스 싱글톤 허브 (Composition Root)
///
/// 【역할】 Repository와 Service 계층을 생성하고 조립하는 의존성 주입(DI) 컨테이너 역할.
///          씬 어디서나 DataService.Instance를 통해 Auth, Progress, Reward, Admin 등의 서비스에 접근 가능.
///          실제 DB 접근은 Repository 내부에서만 수행되며, Service 계층은 비즈니스 로직만 담당한다.
/// 【참조하는 곳】 AuthService(인증), LocalProgressService(진행도), LocalRewardService(보상),
///                LoginController, SignupController — 인증 화면,
///                ProblemStepBase (SaveAttempt/SaveReward), StepFlowController (문제 완료 저장),
///                CommonRewardStep, InventoryDropTargetStepBase — 스텝 내 DB 저장,
///                ProblemSceneController, ThemePanelsController, AdminUserBrowserController,
///                AdminService(관리자 기능)
/// 【참조되는 곳】 DBGateway(LiteDB 접근), 각 Repository/Service 구현체
/// 【흐름】 Awake() → DBGateway 생성 → Repository 조립(6개) → Service 조립(6개)
///         이후 DataService.Instance.Auth.Login() / DataService.Instance.Progress.FetchProgress() 등으로 사용
/// </summary>
public class DataService : MonoBehaviour
{
    /// <summary>전역 싱글톤 인스턴스. DontDestroyOnLoad로 씬 전환에도 유지됨</summary>
    public static DataService Instance { get; private set; }

    /// <summary>원격 서버 사용 여부 (현재 미구현, 항상 로컬 LiteDB 사용)</summary>
    [SerializeField] bool useRemote = false;
    /// <summary>원격 서버 URL (현재 미구현, 향후 서버 연동 시 사용 예정)</summary>
    [SerializeField] string baseUrl = "https://api.example.com";

    /// <summary>LiteDB 접근을 위한 게이트웨이. 직접 사용보다는 Repository를 통한 접근 권장</summary>
    public DBGateway Db { get; private set; }

    // ===== Repositories (데이터 접근 계층) =====
    /// <summary>인벤토리(보상 아이템) 데이터 접근</summary>
    public IInventoryRepository InventoryRepository { get; private set; }
    /// <summary>사용자 데이터 접근 (가입, 조회, 권한 관리)</summary>
    public IUserRepository UserRepository { get; private set; }
    /// <summary>사용자 진행도/시도 기록 데이터 접근</summary>
    public IProgressRepository ProgressRepository { get; private set; }
    /// <summary>문제(Problem) 데이터 접근</summary>
    public IProblemRepository ProblemRepository { get; private set; }
    /// <summary>문제 풀이 결과(ResultDoc) 데이터 접근</summary>
    public IResultRepository ResultRepository { get; private set; }
    /// <summary>관리자 피드백 데이터 접근</summary>
    public IFeedbackRepository FeedbackRepository { get; private set; }

    // ===== Services (비즈니스 로직 계층) =====
    /// <summary>인증 서비스 (회원가입, 로그인, 이메일 중복 확인)</summary>
    public IAuthService Auth { get; private set; }
    /// <summary>진행도 서비스 (Attempt 저장, 문제 클리어 기록)</summary>
    public IProgressService Progress { get; private set; }
    /// <summary>보상 서비스 (인벤토리 아이템 지급, 보상 저장)</summary>
    public IRewardService Reward { get; private set; }
    /// <summary>문제 조회 서비스 (ID로 문제 검색)</summary>
    public IProblemQueryService Problems { get; private set; }
    /// <summary>결과 조회 서비스 (ID로 결과 검색)</summary>
    public IResultQueryService Results { get; private set; }
    /// <summary>관리자 데이터 서비스 (사용자 검색, 결과 조회, 피드백)</summary>
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
        Db = new DBGateway();

        // ----- Repository 조립 -----
        var dbCore = (IDBGateway)Db;

        InventoryRepository = new InventoryRepository(dbCore);
        UserRepository = new UserRepository(dbCore);
        ProgressRepository = new ProgressRepository(dbCore);
        ProblemRepository = new ProblemRepository(dbCore);
        ResultRepository = new ResultRepository(dbCore);
        FeedbackRepository = new FeedbackRepository(dbCore);

        // ----- Service 조립 -----
        Auth = new AuthService(UserRepository);

        Progress = new LocalProgressService(
            ProgressRepository,
            UserRepository,
            ResultRepository
        );

        Reward = new LocalRewardService(
            InventoryRepository,
            UserRepository,
            Progress
        );

        Problems = new LocalProblemQueryService(
            ProblemRepository
        );

        Results = new LocalResultQueryService(
            ResultRepository
        );

        Admin = new LocalAdminDataService(
            UserRepository,
            ResultRepository,
            FeedbackRepository
        );
    }
}
