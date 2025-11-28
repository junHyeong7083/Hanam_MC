# Hanam_MC Unity 프로젝트 기술 보고서

**작성일**: 2025-11-28
**프로젝트명**: Hanam_MC
**분석 대상**: Assets/01. Script 폴더 내 92개 C# 스크립트
**기술 스택**: Unity 2022+, LiteDB, BCrypt.Net, TextMeshPro

---

## 목차

1. [프로젝트 개요](#1-프로젝트-개요)
2. [시스템 아키텍처](#2-시스템-아키텍처)
3. [폴더 구조 분석](#3-폴더-구조-분석)
4. [DB 레이어 상세 분석](#4-db-레이어-상세-분석)
5. [핵심 모듈별 상세 분석](#5-핵심-모듈별-상세-분석)
6. [데이터 모델 및 스키마](#6-데이터-모델-및-스키마)
7. [씬 및 UI 플로우](#7-씬-및-ui-플로우)
8. [적용된 디자인 패턴](#8-적용된-디자인-패턴)
9. [보안 및 검증](#9-보안-및-검증)
10. [서버 연동 가이드](#10-서버-연동-가이드)
11. [결론 및 개선 제안](#11-결론-및-개선-제안)

---

## 1. 프로젝트 개요

### 1.1 시스템 목적

Hanam_MC는 Unity 기반의 **교육용 게임 애플리케이션**입니다. 사용자가 다양한 테마(Director, Gardener 등)의 문제를 풀면서 학습하고, 보상 아이템을 수집하는 시스템입니다.

### 1.2 주요 기능

| 기능 | 설명 |
|------|------|
| **회원 관리** | 회원가입, 로그인, 역할 기반 접근 제어 (USER/ADMIN/SUPERADMIN) |
| **문제 풀이** | 테마별 10개 문제, 단계별(Step) 진행, 다양한 인터랙션 |
| **진행도 추적** | 사용자별 풀이 기록, Attempt 로그, 완료 상태 관리 |
| **보상 시스템** | 문제 완료 시 인벤토리 아이템 지급 |
| **관리자 기능** | 사용자 검색, 진행 상황 모니터링, 피드백 제공 |

### 1.3 기술 스택

| 분류 | 기술 |
|------|------|
| **게임 엔진** | Unity 2022+ |
| **프로그래밍 언어** | C# |
| **데이터베이스** | LiteDB (임베디드 NoSQL) |
| **비밀번호 암호화** | BCrypt.Net (WorkFactor=10) |
| **UI 텍스트** | TextMeshPro |

---

## 2. 시스템 아키텍처

### 2.1 전체 레이어 구조

```
┌─────────────────────────────────────────────────────────────────┐
│                    Presentation Layer                           │
│         (UI Components, Controllers, Scene Managers)            │
│   LoginFormUI, SignupFormUI, HomeSceneUI, ProblemStepBase 등    │
├─────────────────────────────────────────────────────────────────┤
│                    Application Layer                            │
│              (Business Logic Services)                          │
│   AuthService, ProgressService, RewardService, AdminService     │
├─────────────────────────────────────────────────────────────────┤
│                    Data Access Layer                            │
│                    (Repositories)                               │
│   UserRepository, ProgressRepository, InventoryRepository 등    │
├─────────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                           │
│               (Database Gateway)                                │
│              DBGateway, DBHelper                                │
├─────────────────────────────────────────────────────────────────┤
│                    Database Layer                               │
│                  LiteDB (mc.db)                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 핵심 설계 원칙

1. **계층 분리 (Layered Architecture)**: UI → Service → Repository → DB의 명확한 책임 분리
2. **의존성 역전 원칙 (DIP)**: 상위 레이어가 하위 레이어의 인터페이스에만 의존
3. **단일 책임 원칙 (SRP)**: 각 클래스가 하나의 책임만 담당
4. **인터페이스 분리 원칙 (ISP)**: 역할별로 세분화된 인터페이스 정의

---

## 3. 폴더 구조 분석

### 3.1 전체 폴더 구조

```
Assets/01. Script/
├── Bootstrap.cs                 # 앱 진입점
├── SessionManager.cs            # 세션 상태 관리
├── SceneNavigator.cs            # 씬 전환 관리
│
├── Data/                        # 데이터 레이어 (18개 파일)
│   ├── Model.cs                 # 데이터 모델 정의
│   ├── DataService.cs           # 서비스 싱글톤 허브
│   ├── DBHelper.cs              # DB 연결 헬퍼
│   ├── DBGateway/               # DB 접근 게이트웨이
│   │   ├── DBGateway.cs
│   │   └── DBGateway.Core.cs
│   ├── Repository/              # 데이터 접근 레이어
│   │   ├── UserRepository.cs
│   │   ├── ProgressRepository.cs
│   │   ├── InventoryRepository.cs
│   │   ├── ProblemRepository.cs
│   │   ├── ResultRepository.cs
│   │   └── FeedbackRepository.cs
│   ├── LocalProgressService.cs  # 진행도 서비스
│   ├── LocalRewardService.cs    # 보상 서비스
│   ├── LocalAdminDataService.cs # 관리자 서비스
│   ├── LocalProblemQueryService.cs
│   ├── LocalResultQueryService.cs
│   └── UserSearchUtility.cs     # 한글 초성 검색
│
├── Service/                     # 인증 서비스 (4개 파일)
│   ├── AuthService.cs           # 로그인/회원가입
│   ├── AuthValidator.cs         # 입력 검증
│   ├── AuthUIText.cs            # UI 텍스트 설정
│   └── AdminService.cs          # 관리자 기능
│
├── RegisterScene/               # 로그인/회원가입 씬 (6개 파일)
│   ├── LoginFormUI.cs           # 로그인 UI
│   ├── LoginController.cs       # 로그인 로직
│   ├── SignupFormUI.cs          # 회원가입 UI
│   ├── SignupController.cs      # 회원가입 로직
│   ├── RegisterTabsController.cs # 탭 전환
│   └── Result.cs                # Result 모노이드
│
├── HomeScene/                   # 메인 화면 (4개 파일)
│   ├── HomeSceneUI.cs           # 메인 UI
│   ├── ThemePanelsController.cs # 테마 패널 관리
│   ├── ThemePanelUI.cs          # 개별 테마 UI
│   └── ProblemSession.cs        # 문제 선택 세션
│
├── ProblemScene/                # 문제 풀이 (50+ 파일)
│   ├── ProblemSceneController.cs
│   ├── ProblemContext.cs
│   ├── StepFlowController.cs
│   ├── StepCompletionGate.cs
│   ├── StepKeyConfig.cs
│   ├── StepErrorPanel.cs
│   ├── StepBases/
│   │   ├── ProblemStepBase.cs
│   │   ├── MultipleChoiceStepBase.cs
│   │   ├── RandomCardSequenceStepBase.cs
│   │   ├── InventoryDropTargetStepBase.cs
│   │   └── CommonRewardStep.cs
│   ├── StepComponent/
│   │   ├── UIDropBoxArea.cs
│   │   ├── UICardFlip.cs
│   │   └── UILineConnector.cs
│   └── Director/
│       ├── Problem1/ ~ Problem6/
│
├── ResultScene/                 # 관리자 화면 (4개 파일)
│   ├── AdminUserBrowserController.cs
│   ├── AdminUserBrowserUI.cs
│   ├── AdminUserItemUI.cs
│   └── AdminUserCommentPanel.cs
│
├── Inventory/                   # 인벤토리 (3개 파일)
│   ├── RewardInventoryPanel.cs
│   ├── StepInventoryPanel.cs
│   └── StepInventoryItem.cs
│
└── Effect/                      # 애니메이션 효과 (7개 파일)
    └── HomeScene/
        └── LevelSelectPanelAnimator.cs
```

### 3.2 파일 통계

| 폴더 | 파일 수 | 역할 |
|------|--------|------|
| 루트 | 3개 | 앱 초기화, 세션, 씬 전환 |
| Data | 18개 | 데이터 모델, Repository, Service |
| Service | 4개 | 인증, 검증, 관리자 |
| RegisterScene | 6개 | 로그인/회원가입 UI 및 로직 |
| HomeScene | 4개 | 메인 화면, 테마/문제 선택 |
| ProblemScene | 50+개 | 문제 풀이 로직, Step 구현 |
| ResultScene | 4개 | 관리자 사용자 브라우저 |
| Inventory | 3개 | 인벤토리 UI 및 아이템 |
| Effect | 7개 | 애니메이션 효과 |
| **총계** | **92개** | |

---

## 4. DB 레이어 상세 분석

### 4.1 전체 DB 아키텍처

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              UI / Controller Layer                               │
│  (LoginController, SignupController, ThemePanelsController, ProblemStepBase)    │
└───────────────────────────────────────┬─────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        DataService (Singleton Hub)                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  Services (비즈니스 로직)                                                 │    │
│  │  ├─ IAuthService      → AuthService                                     │    │
│  │  ├─ IProgressService  → LocalProgressService                            │    │
│  │  ├─ IRewardService    → LocalRewardService                              │    │
│  │  ├─ IProblemQueryService → LocalProblemQueryService                     │    │
│  │  ├─ IResultQueryService  → LocalResultQueryService                      │    │
│  │  └─ IAdminDataService    → LocalAdminDataService                        │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                        │                                         │
│  ┌─────────────────────────────────────▼───────────────────────────────────┐    │
│  │  Repositories (데이터 접근)                                               │    │
│  │  ├─ IUserRepository      → UserRepository                               │    │
│  │  ├─ IProgressRepository  → ProgressRepository                           │    │
│  │  ├─ IInventoryRepository → InventoryRepository                          │    │
│  │  ├─ IProblemRepository   → ProblemRepository                            │    │
│  │  ├─ IResultRepository    → ResultRepository                             │    │
│  │  └─ IFeedbackRepository  → FeedbackRepository                           │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└───────────────────────────────────────┬─────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           IDBGateway Interface                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  DBGateway (partial class)                                              │    │
│  │  ├─ WithDb<T>(Func<LiteDatabase, T>)  → 값 반환 쿼리                     │    │
│  │  └─ WithDb(Action<LiteDatabase>)      → void 쿼리                        │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└───────────────────────────────────────┬─────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              DBHelper (Static)                                   │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  Connection: "Filename={persistentDataPath}/mc.db;Connection=shared;"   │    │
│  │  With<T>(Func<LiteDatabase, T>) → using var db = new LiteDatabase(...)  │    │
│  │  With(Action<LiteDatabase>)     → using var db = new LiteDatabase(...)  │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
└───────────────────────────────────────┬─────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              LiteDB (mc.db)                                      │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │  Collections:                                                             │  │
│  │  ├─ users      (User)           - 사용자 정보                             │  │
│  │  ├─ results    (ResultDoc)      - 문제 완료 결과                          │  │
│  │  ├─ attempts   (Attempt)        - 단계별 시도 로그                        │  │
│  │  ├─ inventory  (InventoryItem)  - 보상 아이템                             │  │
│  │  ├─ sessions   (SessionRecord)  - 세션 기록                               │  │
│  │  ├─ problems   (Problem)        - 문제 정보                               │  │
│  │  └─ feedback   (Feedback)       - 관리자 피드백                           │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 의존성 주입 (Dependency Injection) 흐름

#### DataService.Awake()에서 조립

```csharp
void Awake()
{
    // 1. 싱글톤 설정
    if (Instance != null) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    // 2. DBGateway 생성 (최하위 레이어)
    Db = new DBGateway();
    var dbCore = (IDBGateway)Db;

    // 3. Repository 생성 (DBGateway 주입)
    InventoryRepository = new InventoryRepository(dbCore);
    UserRepository      = new UserRepository(dbCore);
    ProgressRepository  = new ProgressRepository(dbCore);
    ProblemRepository   = new ProblemRepository(dbCore);
    ResultRepository    = new ResultRepository(dbCore);
    FeedbackRepository  = new FeedbackRepository(dbCore);

    // 4. Service 생성 (Repository들 주입)
    Auth = new AuthService(UserRepository);

    Progress = new LocalProgressService(
        ProgressRepository,
        UserRepository,
        ResultRepository
    );

    Reward = new LocalRewardService(
        InventoryRepository,
        UserRepository,
        Progress          // Service가 다른 Service 의존
    );

    Admin = new LocalAdminDataService(
        UserRepository,
        ResultRepository,
        FeedbackRepository
    );
}
```

#### 의존성 주입 다이어그램

```
                    DataService.Awake()
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
         ▼                 ▼                 ▼
    ┌─────────┐      ┌─────────┐      ┌─────────┐
    │DBGateway│      │DBGateway│      │DBGateway│
    └────┬────┘      └────┬────┘      └────┬────┘
         │                │                │
         ▼                ▼                ▼
┌────────────────┐ ┌─────────────────┐ ┌──────────────────┐
│UserRepository  │ │ProgressRepository│ │InventoryRepository│
└───────┬────────┘ └────────┬────────┘ └─────────┬────────┘
        │                   │                    │
        │    ┌──────────────┼────────────────────┤
        │    │              │                    │
        ▼    ▼              ▼                    ▼
┌─────────────┐    ┌───────────────────┐   ┌─────────────────┐
│ AuthService │    │LocalProgressService│   │LocalRewardService│
│             │    │                   │   │                 │
│ 의존:       │    │ 의존:              │   │ 의존:           │
│ -UserRepo   │    │ -ProgressRepo     │   │ -InventoryRepo  │
└─────────────┘    │ -UserRepo         │   │ -UserRepo       │
                   │ -ResultRepo       │   │ -ProgressService│◄─┐
                   └───────────────────┘   └─────────────────┘  │
                              │                    │            │
                              └────────────────────┴────────────┘
                                    (Service → Service 의존)
```

### 4.3 각 레이어별 코드 분석

#### Layer 1: DBHelper (최하위 - 인프라)

```csharp
public static class DBHelper
{
    // DB 파일 경로: {Application.persistentDataPath}/mc.db
    static string DBPath => Path.Combine(Application.persistentDataPath, "mc.db");

    // 연결 문자열 (shared 모드로 동시 접근 허용)
    static string LitDB_Connection => $"Filename={DBPath};Connection=shared;";

    // 값 반환 쿼리
    public static T With<T>(Func<LiteDatabase, T> f)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        using var db = new LiteDatabase(LitDB_Connection);
        return f(db);
    }

    // void 쿼리
    public static void With(Action<LiteDatabase> f)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        using var db = new LiteDatabase(LitDB_Connection);
        f(db);
    }
}
```

**역할**:
- LiteDB 연결 생성/해제를 `using` 패턴으로 자동 관리
- 연결 문자열 중앙 관리
- 디렉토리 자동 생성

---

#### Layer 2: IDBGateway / DBGateway

```csharp
public interface IDBGateway
{
    T WithDb<T>(Func<LiteDatabase, T> func);
    void WithDb(Action<LiteDatabase> action);
}

public partial class DBGateway : IDBGateway
{
    public T WithDb<T>(Func<LiteDatabase, T> func) => DBHelper.With(func);
    public void WithDb(Action<LiteDatabase> action) => DBHelper.With(action);
}
```

**역할**:
- DBHelper를 인터페이스로 추상화
- Repository가 구체 구현이 아닌 인터페이스에 의존하도록 함
- 테스트 시 Mock 교체 가능

---

#### Layer 3: Repository (데이터 접근)

```csharp
public interface IUserRepository
{
    bool ExistsEmail(string email);
    User FindActiveUserByEmail(string email);
    void InsertUser(User user);
    void UpdateUser(User user);
    UserSummary[] SearchUsersFriendly(string query);
}

public class UserRepository : IUserRepository
{
    private readonly IDBGateway _db;  // ◄── 의존성 주입
    private const string CUsers = "users";

    public UserRepository(IDBGateway db)  // ◄── 생성자 주입
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public bool ExistsEmail(string email)
    {
        return _db.WithDb(db =>  // ◄── DBGateway 사용
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email, true);  // 유니크 인덱스
            return col.Exists(u => u.Email == email);
        });
    }

    public User FindActiveUserByEmail(string email)
    {
        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email, true);
            return col.FindOne(u => u.Email == email && u.IsActive);
        });
    }

    public void InsertUser(User u)
    {
        _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Email, true);
            col.Insert(u);
        });
    }
}
```

**역할**:
- 단일 Collection에 대한 CRUD 담당
- 인덱스 관리 (EnsureIndex)
- LINQ 쿼리 작성
- DB 스키마 변경 시 에러 핸들링

---

#### Layer 4: Service (비즈니스 로직)

```csharp
public interface IAuthService
{
    Result<bool> Exists(string email);
    Result SignUp(string name, string email, string password);
    Result<User> Login(string email, string password);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;  // ◄── Repository 의존
    private const int BcryptWorkFactor = 10;

    public AuthService(IUserRepository users)  // ◄── 생성자 주입
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        EnsureSuperAdmin();  // 기본 관리자 생성
    }

    public Result<User> Login(string email, string password)
    {
        try
        {
            // 1. 유효성 검사
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<User>.Fail(AuthError.EmailInvalid);

            // 2. Repository 호출
            var u = _users.FindActiveUserByEmail(e);
            if (u == null)
                return Result<User>.Fail(AuthError.NotFoundOrInactive);

            // 3. 비즈니스 로직 (BCrypt 검증)
            bool ok = BCrypt.Net.BCrypt.Verify(password, u.PasswordHash);
            if (!ok)
                return Result<User>.Fail(AuthError.PasswordMismatch);

            return Result<User>.Success(u);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] Login error: {ex}");
            return Result<User>.Fail(AuthError.Internal);
        }
    }
}
```

**역할**:
- 비즈니스 로직 처리
- 여러 Repository 조합
- 유효성 검사
- Result 모노이드로 명시적 에러 처리

### 4.4 실제 데이터 흐름 예시

#### 예시 1: 로그인 흐름

```
[LoginController]
    │
    │ DataService.Instance.Auth.Login(email, password)
    ▼
[AuthService.Login()]
    │
    │ _users.FindActiveUserByEmail(email)
    ▼
[UserRepository.FindActiveUserByEmail()]
    │
    │ _db.WithDb(db => {
    │     var col = db.GetCollection<User>("users");
    │     return col.FindOne(u => u.Email == email && u.IsActive);
    │ })
    ▼
[DBGateway.WithDb()]
    │
    │ DBHelper.With(func)
    ▼
[DBHelper.With()]
    │
    │ using var db = new LiteDatabase(connection);
    │ return func(db);
    ▼
[LiteDB]
    │
    │ SELECT * FROM users WHERE Email = ? AND IsActive = true
    ▼
[User 객체 반환]
    │
    ▲ 역순으로 반환
    │
[LoginController]
    │
    │ SessionManager.Instance.SignIn(user)
    ▼
[화면 전환]
```

#### 예시 2: 보상 저장 흐름 (다중 Repository)

```
[CommonRewardStep.SaveReward()]
    │
    │ DataService.Instance.Reward.SaveRewardForCurrentUser(...)
    ▼
[LocalRewardService.SaveRewardForCurrentUser()]
    │
    ├──────────────────────────────────────────────┐
    │                                              │
    │ 1) Attempt 로그 저장                          │
    │ _progressService.SaveStepAttemptForCurrentUser()
    │                                              │
    ▼                                              │
[LocalProgressService]                             │
    │                                              │
    │ _progressRepository.InsertAttempt(attempt)   │
    ▼                                              │
[ProgressRepository.InsertAttempt()]               │
    │                                              │
    │ _db.WithDb(db => col.Insert(attempt))        │
    ▼                                              │
[LiteDB: attempts INSERT]                          │
    │                                              │
    ◄──────────────────────────────────────────────┘
    │
    │ 2) 인벤토리 아이템 지급
    │ GrantInventoryItem(userEmail, invItem)
    ▼
[LocalRewardService.GrantInventoryItem()]
    │
    │ _userRepository.FindActiveUserByEmail()
    │ _inventoryRepository.Add(item)
    ▼
[InventoryRepository.Add()]
    │
    │ _db.WithDb(db => col.Insert(item))
    ▼
[LiteDB: inventory INSERT]
```

### 4.5 인터페이스 분리 현황

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Interface Segregation                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  IUserRepository                IProgressRepository                 │
│  ├─ ExistsEmail()               ├─ GetUserProgress()               │
│  ├─ FindActiveUserByEmail()     ├─ InsertAttempt()                 │
│  ├─ FindUserById()              └─ GetSolvedProblemIndexes()       │
│  ├─ InsertUser()                                                   │
│  ├─ UpdateUser()                IInventoryRepository               │
│  ├─ SearchUsersFriendly()       ├─ Add()                           │
│  ├─ ListAllUsers()              ├─ HasItem()                       │
│  ├─ TrySetUserRole()            └─ GetByUser()                     │
│  ├─ TrySetUserActive()                                             │
│  └─ SearchUsersRaw()            IResultRepository                  │
│                                 ├─ InsertResult()                  │
│  IAuthService                   └─ GetResultsByUser()              │
│  ├─ Exists()                                                       │
│  ├─ SignUp()                    IFeedbackRepository                │
│  └─ Login()                     ├─ InsertFeedback()                │
│                                 └─ GetByResult()                   │
│  IProgressService                                                  │
│  ├─ FetchProgress()             IRewardService                     │
│  ├─ FetchSolvedProblemIndexes() ├─ SaveRewardForCurrentUser()      │
│  ├─ SaveAttempt()               ├─ GrantInventoryItem()            │
│  ├─ SaveStepAttemptForCurrentUser()                                │
│  └─ MarkProblemSolvedForCurrentUser()                              │
│                                 └─ GetInventory()                  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 5. 핵심 모듈별 상세 분석

### 5.1 인증 시스템 (RegisterScene + Service)

#### 구성 요소

| 클래스 | 역할 | 패턴 |
|--------|------|------|
| `LoginFormUI` | 로그인 입력 필드, 버튼 이벤트 발행 | View |
| `LoginController` | AuthService 호출, 세션 설정, 씬 전환 | Controller |
| `SignupFormUI` | 회원가입 UI, 실시간 유효성 힌트 | View |
| `SignupController` | 이메일 중복 체크, 비밀번호 강도 표시 | Controller |
| `AuthService` | BCrypt 해싱, 로그인/가입 로직 | Service |
| `AuthValidator` | 이메일 정규식, 비밀번호 검증 | Utility |

#### 인증 플로우

```
┌──────────────────────────────────────────────────────────────┐
│                      회원가입 플로우                          │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  SignupFormUI                                                │
│       │                                                      │
│       │ OnSignupRequested(name, email, password)            │
│       ▼                                                      │
│  SignupController                                            │
│       │                                                      │
│       │ DataService.Instance.Auth.SignUp(...)               │
│       ▼                                                      │
│  AuthService.SignUp()                                        │
│       │                                                      │
│       ├─ AuthValidator.IsValidEmail()                       │
│       ├─ AuthValidator.IsStrongPassword()                   │
│       ├─ _users.ExistsEmail()                               │
│       ├─ BCrypt.HashPassword(password, 10)                  │
│       └─ _users.InsertUser(user)                            │
│                                                              │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                      로그인 플로우                            │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  LoginFormUI                                                 │
│       │                                                      │
│       │ OnLoginRequested(email, password)                   │
│       ▼                                                      │
│  LoginController                                             │
│       │                                                      │
│       │ DataService.Instance.Auth.Login(...)                │
│       ▼                                                      │
│  AuthService.Login()                                         │
│       │                                                      │
│       ├─ _users.FindActiveUserByEmail(email)                │
│       └─ BCrypt.Verify(password, user.PasswordHash)         │
│              │                                               │
│              ▼                                               │
│       Result<User>.Success(user)                            │
│              │                                               │
│              ▼                                               │
│  LoginController                                             │
│       │                                                      │
│       ├─ SessionManager.Instance.SignIn(user)               │
│       └─ 역할 기반 라우팅:                                   │
│          ├─ ADMIN/SUPERADMIN → ResultScene                  │
│          └─ USER → HomeScene                                │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 5.2 문제 풀이 시스템 (ProblemScene)

#### Step Base 클래스 계층

```
ProblemStepBase (abstract)
    │
    │  공통 기능:
    │  - OnStepEnter() / OnStepExit()
    │  - SaveAttempt(body)
    │  - SaveReward(body, itemId, itemName)
    │  - StepKey 생성 (StepKeyConfig 연동)
    │
    ├─► MultipleChoiceStepBase<TQuestion>
    │       │  객관식 문제 처리
    │       └─► Director_Problem3_Step1, Step3
    │
    ├─► RandomCardSequenceStepBase
    │       │  랜덤 카드 순서 처리
    │       └─► Director_Problem1_Step2
    │
    ├─► InventoryDropTargetStepBase
    │       │  드래그 앤 드롭
    │       └─► Director_Problem4_Step2, Problem5_Step2
    │
    └─► CommonRewardStep
            │  공통 보상 연출
            └─► 모든 문제의 Reward Step
```

#### 문제 풀이 전체 흐름

```
HomeScene에서 문제 선택
    │
    │ ProblemSession.CurrentTheme = Director
    │ ProblemSession.CurrentProblemIndex = 1
    │ SceneNavigator.GoTo(PROBLEM)
    ▼
ProblemSceneController.Start()
    │
    ├─ SetupThemeRoot()       // Director/Gardener 활성화
    └─ ActivateSingleProblem() // 해당 Problem만 활성화
           │
           ▼
StepFlowController.OnEnable()
    │
    │ GoToStep(0)  // Intro부터 시작
    ▼
┌──────────────────────────────────────────────────────────┐
│  Step 0: Intro Panel                                     │
│       │ NextStep() 또는 SkipFlow()                       │
│       ▼                                                  │
│  Step 1: 문제 설명/인터랙션                               │
│       ├─ OnStepEnter()                                   │
│       ├─ 사용자 인터랙션                                  │
│       ├─ SaveAttempt(body)  ──► DB attempts 저장         │
│       ├─ StepCompletionGate.MarkOneDone()               │
│       │ NextStep()                                       │
│       ▼                                                  │
│  Step 2, 3: 추가 문제/인터랙션                            │
│       │ (동일 패턴 반복)                                  │
│       ▼                                                  │
│  Reward: 보상 연출                                        │
│       ├─ CommonRewardStep.OnStepEnter()                  │
│       ├─ SaveReward()  ──► DB attempts + inventory 저장  │
│       ├─ RewardInventoryPanel.ShowInventory()            │
│       │ ProblemEnd()                                     │
│       ▼                                                  │
│  StepFlowController.ProblemEnd()                         │
│       ├─ ProgressService.MarkProblemSolvedForCurrentUser()
│       │       └─► DB results INSERT                      │
│       └─ SceneNavigator.GoTo(HOME)                       │
└──────────────────────────────────────────────────────────┘
```

### 5.3 인벤토리 시스템

```
┌─────────────────────────────────────────────────────────────────┐
│                     Inventory System                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  RewardInventoryPanel (보상 화면)                                │
│       ├─ SyncSlotsFromDb()                                      │
│       │     └─ DataService.Instance.Reward.GetInventory()       │
│       ├─ ApplyLockStatesFromRuntime()                           │
│       │     └─ 슬롯별 잠금/해제 시각화                           │
│       └─ SlotsSequence(unlockedItemId)                          │
│             └─ 순차 등장 애니메이션 + 뱃지 팝                     │
│                                                                 │
│  StepInventoryPanel (Step 내부)                                  │
│       ├─ OnEnable 시 DB 조회                                    │
│       └─ draggableThisStep 설정                                 │
│                                                                 │
│  StepInventoryItem (개별 슬롯)                                   │
│       ├─ Hover: 스케일 확대                                     │
│       ├─ Wiggle: Sin 함수 진동 (드래그 가능 강조)                │
│       ├─ Drag: 반투명 고스트 생성                               │
│       └─ Drop: IStepInventoryDragHandler 이벤트 전달            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. 데이터 모델 및 스키마

### 6.1 Collection 스키마

#### users Collection

| 필드 | 타입 | 설명 | 인덱스 |
|------|------|------|--------|
| Id | string (GUID) | 고유 식별자 | Unique |
| Email | string | 로그인 ID | Unique |
| PasswordHash | string | BCrypt 해시 | - |
| Name | string | 사용자 이름 | - |
| LowerName | string | 소문자 이름 (검색용) | Index |
| NameChosung | string | 초성 (ㄱㅊㅅ) | Index |
| Role | UserRole | USER/ADMIN/SUPERADMIN | Index |
| IsActive | bool | 활성 상태 | - |
| CreatedAt | DateTime | 생성일 | - |

#### results Collection

| 필드 | 타입 | 설명 | 인덱스 |
|------|------|------|--------|
| Id | string (GUID) | 고유 식별자 | - |
| UserId | string | users.Id FK | Index |
| Theme | string | "Director"/"Gardener" | Index |
| ProblemIndex | int | 1~10 | Index |
| Score | int | 점수 | - |
| CorrectRate | decimal? | 정답률 (0~1) | - |
| DurationSec | int? | 풀이 시간(초) | - |
| MetaJson | string | 추가 메타데이터 | - |
| CreatedAt | DateTime | 생성일 | - |

#### attempts Collection

| 필드 | 타입 | 설명 | 인덱스 |
|------|------|------|--------|
| Id | string (GUID) | 고유 식별자 | Unique |
| UserId | string | 사용자 ID | Index |
| UserEmail | string | 사용자 이메일 | Index |
| SessionId | string | 세션 ID | - |
| ProblemId | string | 문제 ID | Index |
| Theme | ProblemTheme | Director/Gardener | Index |
| ProblemIndex | int? | 1~10 | Index |
| Content | string | JSON payload | - |
| CreatedAt | DateTime | 생성일 | - |

#### inventory Collection

| 필드 | 타입 | 설명 | 인덱스 |
|------|------|------|--------|
| Id | string (GUID) | 고유 식별자 | - |
| UserId | string | 사용자 ID | - |
| UserEmail | string | 사용자 이메일 | Index |
| ItemId | string | "mind_lens" 등 | - |
| ItemName | string | "마음 렌즈" 등 | - |
| Theme | ProblemTheme | 테마 | - |
| ProblemIndex | int | 획득 문제 번호 | - |
| AcquiredAt | DateTime | 획득일 | - |

### 6.2 데이터 모델 클래스

```csharp
// 사용자 역할
public enum UserRole { USER = 0, ADMIN = 1, SUPERADMIN = 2 }

// 문제 테마
public enum ProblemTheme { Director = 0, Gardener = 1 }

// Step 식별자
public enum StepId { Step1, Step2, Step3, Step4, Reward, Extra1, Extra2 }
```

---

## 7. 씬 및 UI 플로우

### 7.1 전체 씬 전환 다이어그램

```
                    ┌──────────────┐
                    │  Bootstrap   │
                    │  (진입점)     │
                    └──────┬───────┘
                           │
                    세션 복원 시도
                    SessionManager.TryRestore()
                           │
              ┌────────────┴────────────┐
              │                         │
         세션 있음                   세션 없음
              │                         │
              ▼                         ▼
    ┌─────────────────┐       ┌─────────────────┐
    │   HOME Scene    │       │ REGISTER Scene  │
    │   (문제 선택)    │       │ (로그인/가입)    │
    └────────┬────────┘       └────────┬────────┘
             │                         │
             │                    로그인 성공
             │                         │
             │         ┌───────────────┴───────────────┐
             │         │                               │
             │    USER 역할                    ADMIN/SUPERADMIN
             │         │                               │
             │         ▼                               ▼
             │   ┌─────────────┐              ┌─────────────────┐
             │   │ HOME Scene  │              │  RESULT Scene   │
             │   └─────────────┘              │ (관리자 화면)    │
             │                                └─────────────────┘
             │
        문제 클릭
             │
             ▼
    ┌─────────────────┐
    │ PROBLEM Scene   │
    │  (문제 풀이)     │
    └────────┬────────┘
             │
        ProblemEnd()
             │
             ▼
    ┌─────────────────┐
    │   HOME Scene    │
    │  (다음 문제)     │
    └─────────────────┘
```

### 7.2 잠금 해제 로직

```csharp
void RefreshSinglePanel(ThemePanelUI panel, ProblemTheme theme)
{
    // 1. DB에서 해결한 문제 목록 조회
    var res = DataService.Instance.Progress
        .FetchSolvedProblemIndexes(userEmail, theme);

    var solvedSet = new HashSet<int>(res.Value);  // 예: {1, 2, 3}
    bool allSolved = solvedSet.Count >= 10;

    // 2. 잠금 상태 결정
    bool[] unlocked = new bool[11];

    if (allSolved)
    {
        // 10개 다 풀었으면 전체 해제 (자유 선택)
        for (int i = 1; i <= 10; i++)
            unlocked[i] = true;
    }
    else
    {
        // 다음 문제만 해제 (순차 진행)
        int nextIndex = FindFirstNotSolved(solvedSet);
        for (int i = 1; i <= 10; i++)
            unlocked[i] = (i == nextIndex);
    }

    // 3. UI 반영
    panel.SetLockStates(unlocked, solvedSet);
}
```

---

## 8. 적용된 디자인 패턴

### 8.1 패턴 목록

| 패턴 | 적용 위치 | 설명 |
|------|----------|------|
| **Singleton** | DataService, SessionManager, Bootstrap | 앱 전역 상태 관리 |
| **Repository** | 모든 Repository 클래스 | 데이터 접근 추상화 |
| **Service Layer** | AuthService, ProgressService 등 | 비즈니스 로직 캡슐화 |
| **MVC** | RegisterScene (FormUI + Controller) | UI와 로직 분리 |
| **Template Method** | ProblemStepBase | 공통 흐름 정의, 세부 구현 위임 |
| **Strategy** | MultipleChoiceStepBase<T> | 제네릭으로 알고리즘 교체 |
| **Observer** | 이벤트 기반 UI 통신 | 느슨한 결합 |
| **Facade** | DataService | 복잡한 하위 시스템 단순화 |
| **Result Monad** | Result<T> | 명시적 에러 처리 |

### 8.2 주요 패턴 상세

#### Singleton 패턴

```csharp
public class DataService : MonoBehaviour
{
    public static DataService Instance { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

#### Result Monad 패턴

```csharp
public readonly struct Result<T>
{
    public bool Ok { get; }
    public AuthError Error { get; }
    public T Value { get; }

    public static Result<T> Success(T value) => new(true, default, value);
    public static Result<T> Fail(AuthError error) => new(false, error, default);
}

// 사용 예시
var result = AuthService.Login(email, password);
if (!result.Ok)
{
    ShowError(result.Error);
    return;
}
var user = result.Value;
```

---

## 9. 보안 및 검증

### 9.1 비밀번호 보안

```csharp
// BCrypt 해싱 (WorkFactor = 10)
string hash = BCrypt.Net.BCrypt.HashPassword(password, 10);

// 검증
bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
```

**특징**:
- 평문 비밀번호 저장 없음
- Salt 자동 생성 및 포함
- WorkFactor=10 (2^10 = 1024 라운드)

### 9.2 입력 검증

```csharp
public static class AuthValidator
{
    // 이메일 정규식
    private static readonly Regex EmailRx = new Regex(
        @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$");

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRx.IsMatch(email);
    }

    // 비밀번호 강도 검사 (8자 이상, 영문+숫자)
    public static bool IsStrongPassword(string pw)
    {
        if (string.IsNullOrEmpty(pw) || pw.Length < 8) return false;

        bool hasLetter = false, hasDigit = false;
        foreach (var c in pw)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
        }
        return hasLetter && hasDigit;
    }
}
```

### 9.3 권한 관리

```csharp
// 역할 기반 라우팅
if (user.Role >= UserRole.ADMIN)
{
    SceneNavigator.GoTo(ScreenId.RESULT);  // 관리자 화면
}
else
{
    SceneNavigator.GoTo(ScreenId.HOME);    // 일반 사용자 화면
}
```

---

## 10. 서버 연동 가이드

### 10.1 현재 로컬 구조 vs 서버 연동 구조

```
┌─────────────────────────────────────────────────────────────────┐
│                      현재 구조 (로컬)                            │
├─────────────────────────────────────────────────────────────────┤
│  DataService                                                    │
│       ├─ Auth = new AuthService(UserRepository)                │
│       ├─ Progress = new LocalProgressService(...)              │
│       └─ Reward = new LocalRewardService(...)                  │
│                    │                                            │
│                    ▼                                            │
│              Repositories → DBGateway → LiteDB                 │
└─────────────────────────────────────────────────────────────────┘

                           ▼▼▼

┌─────────────────────────────────────────────────────────────────┐
│                    서버 연동 구조 (목표)                         │
├─────────────────────────────────────────────────────────────────┤
│  DataService                                                    │
│       │ useRemote = true 일 때:                                 │
│       ├─ Auth = new RemoteAuthService(httpClient)              │
│       ├─ Progress = new RemoteProgressService(httpClient)      │
│       └─ Reward = new RemoteRewardService(httpClient)          │
│                    │                                            │
│                    ▼                                            │
│              HTTP Client → REST API 서버 → PostgreSQL          │
└─────────────────────────────────────────────────────────────────┘
```

### 10.2 수정이 필요한 파일들

#### 1단계: 원격 Service 구현 생성

```
Assets/01. Script/Data/
├── Remote/                          # 새로 생성
│   ├── RemoteAuthService.cs
│   ├── RemoteProgressService.cs
│   ├── RemoteRewardService.cs
│   ├── RemoteAdminDataService.cs
│   └── HttpClientWrapper.cs         # HTTP 통신 래퍼
```

#### 2단계: DataService 수정

```csharp
public class DataService : MonoBehaviour
{
    [SerializeField] bool useRemote = false;           // ◄── 토글
    [SerializeField] string baseUrl = "https://api.example.com";

    void Awake()
    {
        // 기존 싱글톤 코드...

        if (useRemote)
        {
            InitializeRemoteServices();
        }
        else
        {
            InitializeLocalServices();
        }
    }

    private void InitializeLocalServices()
    {
        // 기존 코드 그대로
        Db = new DBGateway();
        var dbCore = (IDBGateway)Db;
        UserRepository = new UserRepository(dbCore);
        Auth = new AuthService(UserRepository);
        // ...
    }

    private void InitializeRemoteServices()
    {
        // 새로운 원격 서비스 초기화
        var httpClient = new HttpClientWrapper(baseUrl);
        Auth = new RemoteAuthService(httpClient);
        Progress = new RemoteProgressService(httpClient);
        Reward = new RemoteRewardService(httpClient);
    }
}
```

### 10.3 Remote Service 구현 예시

#### RemoteAuthService.cs

```csharp
public class RemoteAuthService : IAuthService
{
    private readonly HttpClientWrapper _http;

    public RemoteAuthService(HttpClientWrapper http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public Result<User> Login(string email, string password)
    {
        try
        {
            var payload = new LoginRequest { email = email, password = password };
            var response = _http.Post("/api/auth/login", payload);

            if (response.IsSuccess)
            {
                var data = JsonUtility.FromJson<LoginResponse>(response.Body);
                TokenManager.SaveToken(data.token);

                var user = new User
                {
                    Id = data.user.id,
                    Name = data.user.name,
                    Email = data.user.email,
                    Role = (UserRole)data.user.role,
                    IsActive = true
                };
                return Result<User>.Success(user);
            }

            var error = JsonUtility.FromJson<ErrorResponse>(response.Body);
            return Result<User>.Fail(ParseAuthError(error.code));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteAuthService] Login error: {ex}");
            return Result<User>.Fail(AuthError.Internal);
        }
    }

    // SignUp, Exists 등 동일 패턴으로 구현...
}
```

#### HttpClientWrapper.cs

```csharp
public class HttpClientWrapper
{
    private readonly string _baseUrl;

    public HttpClientWrapper(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public HttpResponse Post(string endpoint, object payload)
    {
        var url = _baseUrl + endpoint;
        var json = JsonUtility.ToJson(payload);
        var bodyRaw = Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        AddAuthHeader(request);

        var operation = request.SendWebRequest();
        while (!operation.isDone) { }

        return new HttpResponse
        {
            StatusCode = (int)request.responseCode,
            Body = request.downloadHandler.text,
            IsSuccess = request.result == UnityWebRequest.Result.Success
        };
    }

    private void AddAuthHeader(UnityWebRequest request)
    {
        var token = TokenManager.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", $"Bearer {token}");
        }
    }
}
```

### 10.4 서버 API 설계 예시

| Method | Endpoint | 설명 | Request Body | Response |
|--------|----------|------|--------------|----------|
| GET | `/api/auth/exists?email={email}` | 이메일 중복 확인 | - | `{ exists: bool }` |
| POST | `/api/auth/signup` | 회원가입 | `{ name, email, password }` | `{ success: bool }` |
| POST | `/api/auth/login` | 로그인 | `{ email, password }` | `{ token, user }` |
| GET | `/api/progress/{userEmail}` | 진행도 조회 | - | `{ progress }` |
| GET | `/api/progress/{userEmail}/solved` | 해결한 문제 | - | `{ indexes: int[] }` |
| POST | `/api/progress/attempt` | Attempt 저장 | `{ attempt }` | `{ success: bool }` |
| POST | `/api/progress/solve` | 문제 완료 | `{ theme, problemIndex }` | `{ success: bool }` |
| GET | `/api/inventory/{userEmail}` | 인벤토리 조회 | - | `{ items: [] }` |
| POST | `/api/inventory/grant` | 아이템 지급 | `{ item }` | `{ success: bool }` |

### 10.5 수정 체크리스트

서버 연동 시 수정이 필요한 항목:

- [ ] `DataService.cs` - useRemote 분기 처리 추가
- [ ] `Remote/HttpClientWrapper.cs` - HTTP 통신 래퍼 생성
- [ ] `Remote/TokenManager.cs` - JWT 토큰 관리
- [ ] `Remote/RemoteAuthService.cs` - 원격 인증 서비스
- [ ] `Remote/RemoteProgressService.cs` - 원격 진행도 서비스
- [ ] `Remote/RemoteRewardService.cs` - 원격 보상 서비스
- [ ] `SessionManager.cs` - 토큰 기반 세션 관리 추가
- [ ] 서버 측 REST API 구현
- [ ] 서버 측 데이터베이스 스키마 설계

---

## 11. 결론 및 개선 제안

### 11.1 프로젝트 강점

1. **명확한 계층 분리**: UI → Controller → Service → Repository → DB
2. **의존성 역전 원칙 준수**: 인터페이스 기반 설계로 교체 용이
3. **확장 가능한 설계**: 새로운 문제 타입, 테마 추가 용이
4. **일관된 네이밍**: 클래스/메서드명이 역할을 명확히 표현
5. **재사용 가능한 컴포넌트**: StepCompletionGate, Base 클래스들

### 11.2 개선 제안

| 현재 | 제안 | 이유 |
|------|------|------|
| 일부 동기 DB 호출 | 비동기 처리 도입 | 메인 스레드 블로킹 방지 |
| 정적 ProblemSession | ScriptableObject 또는 DI | 테스트 용이성 |
| Exception + Result 혼용 | Result 패턴 통일 | 일관된 에러 처리 |
| 스키마 변경 시 Drop | 마이그레이션 전략 | 데이터 보존 |

### 11.3 기술적 성과 요약

- **총 92개 C# 스크립트**를 체계적으로 구조화
- **LiteDB + BCrypt**를 활용한 안전한 로컬 데이터 관리
- **인터페이스 기반 설계**로 로컬 ↔ 서버 전환 용이
- **제네릭 Base 클래스**로 코드 중복 최소화
- **이벤트 기반 아키텍처**로 낮은 결합도 유지

---

**문서 작성**: Claude (Anthropic)
**분석 대상**: Hanam_MC Unity 프로젝트
**총 스크립트 수**: 92개
**주요 기술**: Unity, C#, LiteDB, BCrypt.Net, TextMeshPro
