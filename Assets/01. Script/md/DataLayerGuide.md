# Data Layer 가이드

## 개요
데이터 레이어는 Repository Pattern과 Service Pattern을 사용하여 데이터 접근을 추상화합니다.

---

## 1. 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                        DataService                          │
│                    (Singleton Hub)                          │
├─────────────────────────────────────────────────────────────┤
│  Repositories              │  Services                      │
│  ├── IUserRepository       │  ├── IAuthService              │
│  ├── IProgressRepository   │  ├── IProgressService          │
│  ├── IInventoryRepository  │  ├── IRewardService            │
│  ├── IProblemRepository    │  ├── IProblemQueryService      │
│  ├── IResultRepository     │  └── IAdminDataService         │
│  └── IFeedbackRepository   │                                │
├─────────────────────────────────────────────────────────────┤
│                       IDBGateway                            │
│                      (LiteDB 래퍼)                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Model 클래스

### 2.1 기본 구조
```csharp
public class User
{
    // 고유 식별자 (항상 Guid 문자열)
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // 기본 정보
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }

    // 상태/권한
    public UserRole Role { get; set; } = UserRole.USER;
    public bool IsActive { get; set; } = true;

    // 타임스탬프
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 검색용 보조 필드 (선택)
    public string LowerName { get; set; }
    public string NameChosung { get; set; }
}
```

### 2.2 Model 정의 규칙
| 항목 | 규칙 |
|------|------|
| Id | `Guid.NewGuid().ToString()` 기본값 |
| 타임스탬프 | `DateTime.UtcNow` 기본값 |
| 외래키 | `{Entity}Id` 형식 (예: UserId) |
| Enum | Model.cs에 함께 정의 |
| 기본값 | 속성 선언 시 설정 |

### 2.3 주요 Model들
```csharp
// 사용자
public class User { ... }
public class UserSummary { ... }  // 조회용 DTO

// 문제/진행
public class Problem { ... }
public class SessionRecord { ... }
public class Attempt { ... }

// 결과/피드백
public class ResultDoc { ... }
public class Feedback { ... }

// 보상
public class InventoryItem { ... }

// 집계
public class UserProgress { ... }
public class ProblemFlowSummary { ... }
```

---

## 3. Repository 패턴

### 3.1 Interface 정의
```csharp
public interface IUserRepository
{
    // 기본 CRUD
    bool ExistsEmail(string email);
    User FindActiveUserByEmail(string email);
    User FindUserById(string id);
    void InsertUser(User user);
    void UpdateUser(User user);

    // 검색/목록
    UserSummary[] SearchUsersFriendly(string query);
    UserSummary[] ListAllUsers(int limit = 0);

    // 관리자 기능
    bool TrySetUserRole(string actingUserId, string targetUserId, UserRole role);
    bool TrySetUserActive(string actingUserId, string targetUserId, bool active);
}
```

### 3.2 Implementation 구조
```csharp
public class UserRepository : IUserRepository
{
    private readonly IDBGateway _db;
    private const string CUsers = "users";  // 컬렉션 이름

    public UserRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public User FindUserById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);  // 인덱스 보장
            return col.FindById(id);
        });
    }

    public void InsertUser(User u)
    {
        if (u == null) throw new ArgumentNullException(nameof(u));

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

### 3.3 Repository 구현 규칙
1. **생성자에서 null 체크**
   ```csharp
   _db = db ?? throw new ArgumentNullException(nameof(db));
   ```

2. **메서드 시작 시 파라미터 검증**
   ```csharp
   if (string.IsNullOrWhiteSpace(id)) return null;
   if (u == null) throw new ArgumentNullException(nameof(u));
   ```

3. **EnsureIndex 호출**
   ```csharp
   col.EnsureIndex(x => x.Id, true);  // unique
   col.EnsureIndex(x => x.Email);     // non-unique
   ```

4. **컬렉션 이름 상수화**
   ```csharp
   private const string CUsers = "users";
   ```

---

## 4. Service 패턴

### 4.1 Interface 정의
```csharp
public interface IAuthService
{
    Result<bool> Exists(string email);
    Result SignUp(string name, string email, string password);
    Result<User> Login(string email, string password);
}
```

### 4.2 Implementation 구조
```csharp
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private const int BcryptWorkFactor = 10;

    public AuthService(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        EnsureSuperAdmin();  // 초기화 시 관리자 계정 보장
    }

    public Result<User> Login(string email, string password)
    {
        try
        {
            // 1. 입력 검증
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<User>.Fail(AuthError.EmailInvalid);

            if (string.IsNullOrEmpty(password))
                return Result<User>.Fail(AuthError.PasswordWeak);

            // 2. 사용자 조회
            var u = _users.FindActiveUserByEmail(e);
            if (u == null)
                return Result<User>.Fail(AuthError.NotFoundOrInactive);

            // 3. 비밀번호 검증
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

### 4.3 Service 구현 규칙
1. **Repository 주입**
   ```csharp
   public AuthService(IUserRepository users)
   {
       _users = users ?? throw new ArgumentNullException(nameof(users));
   }
   ```

2. **Result 패턴 사용**
   ```csharp
   return Result<User>.Success(user);
   return Result<User>.Fail(AuthError.EmailInvalid);
   ```

3. **try-catch로 예외 처리**
   ```csharp
   try { ... }
   catch (Exception ex)
   {
       Debug.LogError($"[{ServiceName}] {Method} error: {ex}");
       return Result<T>.Fail(ErrorCode.Internal);
   }
   ```

4. **Validator 분리**
   ```csharp
   var e = AuthValidator.NormalizeEmail(email);
   if (!AuthValidator.IsValidEmail(e)) ...
   ```

---

## 5. Result 패턴

### 5.1 기본 Result
```csharp
public class Result
{
    public bool Ok { get; private set; }
    public string Error { get; private set; }

    private Result(bool ok, string error)
    {
        Ok = ok;
        Error = error;
    }

    public static Result Success() => new Result(true, null);
    public static Result Fail(string error) => new Result(false, error);
    public static Result Fail(Enum errorCode) => new Result(false, errorCode.ToString());
    public static Result Fail(Enum errorCode, string message)
        => new Result(false, $"{errorCode}: {message}");
}
```

### 5.2 제네릭 Result
```csharp
public class Result<T>
{
    public bool Ok { get; private set; }
    public T Data { get; private set; }
    public string Error { get; private set; }

    private Result(bool ok, T data, string error)
    {
        Ok = ok;
        Data = data;
        Error = error;
    }

    public static Result<T> Success(T data) => new Result<T>(true, data, null);
    public static Result<T> Fail(string error) => new Result<T>(false, default, error);
    public static Result<T> Fail(Enum errorCode) => new Result<T>(false, default, errorCode.ToString());
}
```

### 5.3 사용 예시
```csharp
// 서비스에서 반환
public Result<User> Login(string email, string password)
{
    // 성공
    return Result<User>.Success(user);

    // 실패
    return Result<User>.Fail(AuthError.PasswordMismatch);
}

// 호출 측에서 처리
var result = Auth.Login(email, password);
if (!result.Ok)
{
    Debug.LogWarning($"Login failed: {result.Error}");
    return;
}
var user = result.Data;
```

---

## 6. DataService 사용법

### 6.1 접근 방법
```csharp
var ds = DataService.Instance;
if (ds == null)
{
    Debug.LogWarning("DataService not initialized");
    return;
}

// Repository 직접 접근
var user = ds.UserRepository.FindUserById(id);

// Service 통한 접근 (권장)
var result = ds.Auth.Login(email, password);
```

### 6.2 ProblemStepBase에서 사용
```csharp
protected void SaveAttempt(object body)
{
    if (!useDBSave || context == null) return;

    var ds = DataService.Instance;
    if (ds == null || ds.Progress == null)
    {
        Debug.LogWarning("[ProblemStepBase] DataService.Progress 없음");
        return;
    }

    string stepKey = BuildStepKey();
    var payload = new { stepKey, theme = context.Theme.ToString(), body };

    var result = ds.Progress.SaveStepAttemptForCurrentUser(
        context.Theme,
        context.ProblemIndex,
        context.ProblemId,
        payload
    );

    if (!result.Ok)
        Debug.LogWarning($"SaveAttempt 실패: {result.Error}");
}
```

---

## 7. 에러 코드 정의

### 7.1 AuthError
```csharp
public enum AuthError
{
    EmailInvalid,
    EmailDuplicate,
    PasswordWeak,
    PasswordMismatch,
    NotFoundOrInactive,
    NameEmpty,
    Internal
}
```

### 7.2 에러 코드 사용
```csharp
// 실패 시 에러 코드 반환
return Result.Fail(AuthError.EmailInvalid, "이메일 형식이 올바르지 않습니다.");

// 호출 측에서 처리
if (result.Error.Contains(AuthError.EmailInvalid.ToString()))
{
    ShowEmailError();
}
```

---

## 8. 테스트 및 디버그

### 8.1 Repository 테스트
```csharp
// 에디터 스크립트나 테스트에서
[MenuItem("Debug/Test UserRepository")]
static void TestUserRepository()
{
    var db = new DBGateway();
    var repo = new UserRepository(db);

    // 테스트 사용자 생성
    var testUser = new User
    {
        Name = "Test",
        Email = "test@test.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("test1234")
    };

    repo.InsertUser(testUser);
    Debug.Log($"Inserted: {testUser.Id}");

    var found = repo.FindUserById(testUser.Id);
    Debug.Log($"Found: {found?.Name}");
}
```

### 8.2 로그 위치
- Repository: `[{RepositoryName}]`
- Service: `[{ServiceName}]`
- DataService: `[DataService]`

---

## 9. 확장 가이드

### 9.1 새 Repository 추가
1. Interface 정의
   ```csharp
   public interface INewRepository
   {
       NewEntity FindById(string id);
       void Insert(NewEntity entity);
   }
   ```

2. Implementation 작성
   ```csharp
   public class NewRepository : INewRepository
   {
       private readonly IDBGateway _db;
       private const string CCollection = "newentities";
       // ...
   }
   ```

3. DataService에 등록
   ```csharp
   // DataService.Awake()
   NewRepository = new NewRepository(dbCore);
   ```

### 9.2 새 Service 추가
1. Interface 정의
   ```csharp
   public interface INewService
   {
       Result<SomeData> DoSomething(string param);
   }
   ```

2. Implementation 작성
   ```csharp
   public class NewService : INewService
   {
       private readonly INewRepository _repo;

       public NewService(INewRepository repo)
       {
           _repo = repo ?? throw new ArgumentNullException(nameof(repo));
       }
       // ...
   }
   ```

3. DataService에 등록
   ```csharp
   // DataService.Awake()
   NewService = new NewService(NewRepository);
   ```
