using System;
using UnityEngine;
using System.Linq;

/// <summary>
/// IUserDataService - 사용자 문제 풀이/진행도용 통합 데이터 서비스 인터페이스
///
/// 【역할】 진행도 조회, 문제 조회, 시도(Attempt) 저장, 결과 조회, 인벤토리 관리 등
///          사용자 관련 모든 데이터 기능을 하나의 인터페이스로 통합한다.
///          (참고: 현재는 DataService에서 개별 서비스(Progress, Reward, Problems, Results)로
///          분리되어 사용되므로, 이 통합 인터페이스는 레거시 코드에 가깝다.)
/// 【참조하는 곳】 일부 구버전 컨트롤러에서 사용 가능
/// 【참조되는 곳】 Repository들 (Inventory, User, Progress, Problem, Result)
/// </summary>
public interface IUserDataService
{
    // 나의 진행 요약
    Result<UserProgress> FetchProgress(string userEmail);

    // 문제 데이터 조회 (ID 기준)
    Result<Problem> FetchProblem(string problemId);

    // 제출 저장(시도/답안 등)
    Result SaveAttempt(Attempt attempt);

    // 결과 조회(세션 또는 ResultId 기준)
    Result<ResultDoc> FetchResult(string resultIdOrSessionId);

    // 사용자가 푼 문제 번호 목록
    Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme);

    // 현재 로그인 사용자 기준 Attempt 저장 (문제 풀이용 헬퍼)
    Result SaveStepAttemptForCurrentUser(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        object payload
    );

    // 현재 로그인 사용자 기준 보상 Attempt + 인벤토리 저장 헬퍼
    Result SaveRewardForCurrentUser(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        object payload,
        string itemId,
        string itemName
    );

    // ===== 인벤토리 =====
    Result GrantInventoryItem(string userEmail, InventoryItem item);
    Result<InventoryItem[]> GetInventory(string userEmail);

    Result MarkProblemSolvedForCurrentUser(ProblemTheme theme, int problemIndex);

}

/// <summary>
/// LocalUserDataService - IUserDataService의 로컬(LiteDB) 구현체
///
/// 【역할】 사용자 문제 풀이에 필요한 모든 데이터 기능을 통합 제공한다.
///          진행도 조회, 문제 조회, Attempt 저장, 결과 조회, 보상/인벤토리 관리가 포함된다.
///          (참고: DataService에서는 이 클래스 대신 개별 서비스(LocalProgressService, LocalRewardService 등)를
///          사용하도록 리팩토링되었다. 이 클래스는 동일 기능의 레거시 통합 버전이다.)
/// 【참조하는 곳】 직접 생성하여 사용하는 곳은 현재 없음 (DataService가 개별 서비스를 조립)
/// 【참조되는 곳】 IInventoryRepository, IUserRepository, IProgressRepository,
///                IProblemRepository, IResultRepository
/// </summary>
public class LocalUserDataService : IUserDataService
{
    /// <summary>인벤토리 아이템 저장/조회용 Repository</summary>
    private readonly IInventoryRepository _inventoryRepository;
    /// <summary>사용자 조회용 Repository</summary>
    private readonly IUserRepository _userRepository;
    /// <summary>진행도/시도 기록 접근용 Repository</summary>
    private readonly IProgressRepository _progressRepository;
    /// <summary>문제 데이터 조회용 Repository</summary>
    private readonly IProblemRepository _problemRepository;
    /// <summary>결과 데이터 저장/조회용 Repository</summary>
    private readonly IResultRepository _resultRepository;

    /// <summary>생성자. 5개의 Repository를 모두 주입받아야 한다.</summary>
    public LocalUserDataService(
     IInventoryRepository inventoryRepository,
     IUserRepository userRepository,
     IProgressRepository progressRepository,
     IProblemRepository problemRepository,
     IResultRepository resultRepository)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _problemRepository = problemRepository ?? throw new ArgumentNullException(nameof(problemRepository));
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
    }


    /// <summary>사용자 진행도 요약을 조회한다 (총 세션 수, 풀이 수, 마지막 세션 시각)</summary>
    public Result<UserProgress> FetchProgress(string userEmail)
    {
        try
        {
            var progress = _progressRepository.GetUserProgress(userEmail);
            return Result<UserProgress>.Success(progress);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProgress: {e}");
            return Result<UserProgress>.Fail(AuthError.Internal);
        }
    }


    /// <summary>문제 ID로 Problem을 조회한다. 존재하지 않으면 NotFoundOrInactive</summary>
    public Result<Problem> FetchProblem(string problemId)
    {
        try
        {
            var p = _problemRepository.GetProblemById(problemId);
            if (p == null)
                return Result<Problem>.Fail(AuthError.NotFoundOrInactive);

            return Result<Problem>.Success(p);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchProblem: {e}");
            return Result<Problem>.Fail(AuthError.Internal);
        }
    }


    /// <summary>Attempt(시도) 기록을 DB에 저장한다. UserId/Email 미설정 시 현재 사용자로 자동 채움</summary>
    public Result SaveAttempt(Attempt attempt)
    {
        if (attempt == null)
            return Result.Fail(AuthError.Internal, "Attempt is null");

        try
        {
            var sess = SessionManager.Instance;
            var currentUser = sess?.CurrentUser;

            if (currentUser != null)
            {
                if (string.IsNullOrEmpty(attempt.UserId))
                    attempt.UserId = currentUser.Id;
                if (string.IsNullOrEmpty(attempt.UserEmail))
                    attempt.UserEmail = currentUser.Email;
            }

            if (string.IsNullOrEmpty(attempt.Id))
                attempt.Id = Guid.NewGuid().ToString();
            if (attempt.CreatedAt == default)
                attempt.CreatedAt = DateTime.UtcNow;

            _progressRepository.InsertAttempt(attempt);
            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] SaveAttempt: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }



    /// <summary>결과 ID로 ResultDoc을 조회한다. 존재하지 않으면 NotFoundOrInactive</summary>
    public Result<ResultDoc> FetchResult(string resultIdOrSessionId)
    {
        if (string.IsNullOrWhiteSpace(resultIdOrSessionId))
            return Result<ResultDoc>.Fail(AuthError.NotFoundOrInactive);

        try
        {
            var r = _resultRepository.GetResultById(resultIdOrSessionId);
            if (r != null)
                return Result<ResultDoc>.Success(r);

            return Result<ResultDoc>.Fail(AuthError.NotFoundOrInactive);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchResult: {e}");
            return Result<ResultDoc>.Fail(AuthError.Internal);
        }
    }


    /// <summary>사용자가 특정 테마에서 풀이 완료한 문제 번호 목록을 조회한다</summary>
    public Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme)
    {
        try
        {
            string themeKey = theme.ToString();
            var indexes = _progressRepository.GetSolvedProblemIndexes(userEmail, themeKey)
                         ?? Array.Empty<int>();

            return Result<int[]>.Success(indexes);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] FetchSolvedProblemIndexes: {e}");
            return Result<int[]>.Fail(AuthError.Internal);
        }
    }


    // =========================
    // 편의 메서드: 현재 로그인 사용자 기준 Attempt / Reward 저장
    // =========================

    /// <summary>
    /// 현재 로그인 사용자의 스텝 시도를 기록한다.
    /// payload를 JSON으로 직렬화하여 Attempt.Content에 저장한다.
    /// </summary>
    public Result SaveStepAttemptForCurrentUser(
      ProblemTheme theme,
      int problemIndex,
      string problemId,
      object payload
  )
    {
        var sess = SessionManager.Instance;
        var currentUser = sess?.CurrentUser;

        if (sess == null || currentUser == null)
        {
            Debug.LogWarning("[LocalUserData] 세션/유저 없음 - Attempt 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        try
        {
            // body는 JSON 직렬화를 위해 문자열로 저장
            string json = payload != null
                ? UnityEngine.JsonUtility.ToJson(payload)
                : null;

            var attempt = new Attempt
            {
                UserId = currentUser.Id,
                UserEmail = currentUser.Email,
                SessionId = null,               // 나중에 SessionRecord 도입 시 채워도 됨
                ProblemId = problemId,
                Theme = theme,
                ProblemIndex = problemIndex,
                Content = json,
                CreatedAt = DateTime.UtcNow
            };

            return SaveAttempt(attempt);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] SaveStepAttemptForCurrentUser: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }


    /// <summary>
    /// 현재 로그인 사용자에게 보상을 지급한다.
    /// Attempt 저장 + 인벤토리 아이템 추가를 한 번에 수행한다.
    /// </summary>
    public Result SaveRewardForCurrentUser(
     ProblemTheme theme,
     int problemIndex,
     string problemId,
     object payload,
     string itemId,
     string itemName
 )
    {
        var sess = SessionManager.Instance;
        var currentUser = sess?.CurrentUser;

        if (sess == null || currentUser == null)
        {
            Debug.LogWarning("[LocalUserData] 세션/유저 없음 - 보상 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        string userEmail = currentUser.Email;

        // 1) Attempt 로그 저장
        var attemptResult = SaveStepAttemptForCurrentUser(
            theme,
            problemIndex,
            problemId,
            payload
        );

        if (!attemptResult.Ok)
            return attemptResult;

        // 2) 인벤토리 아이템 지급
        try
        {
            var invItem = new InventoryItem
            {
                UserId = currentUser.Id,
                UserEmail = userEmail,
                ItemId = itemId,
                ItemName = itemName,
                Theme = theme,
                ProblemIndex = problemIndex,
                AcquiredAt = DateTime.UtcNow
            };

            return GrantInventoryItem(userEmail, invItem);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalUserData] SaveRewardForCurrentUser error: {ex}");
            return Result.Fail(AuthError.Internal);
        }
    }


    // =========================
    // 인벤토리 관련 메서드
    // =========================

    /// <summary>특정 사용자에게 인벤토리 아이템을 지급한다. 사용자 존재/활성 여부를 검증</summary>
    public Result GrantInventoryItem(string userEmail, InventoryItem item)
    {
        if (item == null)
            return Result.Fail(AuthError.Internal, "InventoryItem is null");

        try
        {
            var user = _userRepository.FindActiveUserByEmail(userEmail);
            if (user == null)
                return Result.Fail(AuthError.NotFoundOrInactive);

            // UserId / Email 보정
            item.UserId = user.Id;
            item.UserEmail = user.Email;

            if (item.AcquiredAt == default)
                item.AcquiredAt = DateTime.UtcNow;

            _inventoryRepository.Add(item);
            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] GrantInventoryItem: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }


    /// <summary>특정 사용자의 인벤토리 전체를 조회한다</summary>
    public Result<InventoryItem[]> GetInventory(string userEmail)
    {
        try
        {
            var list = _inventoryRepository.GetByUser(userEmail);
            var arr = (list != null) ? list.ToArray() : Array.Empty<InventoryItem>();
            Debug.Log("DB List" + list);
            return Result<InventoryItem[]>.Success(arr);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalUserData] GetInventory error: {ex}");
            return Result<InventoryItem[]>.Fail(AuthError.InventoryError);
        }
    }

    /// <summary>
    /// 현재 로그인 사용자의 문제 풀이 완료를 기록한다.
    /// 이미 같은 테마+문제번호의 ResultDoc이 있으면 중복 생성하지 않는다.
    /// </summary>
    public Result MarkProblemSolvedForCurrentUser(ProblemTheme theme, int problemIndex)
    {
        var sess = SessionManager.Instance;
        if (sess == null || sess.CurrentUser == null)
        {
            Debug.LogWarning("[LocalUserData] 세션/유저 없음 - 문제 클리어 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        try
        {
            string userEmail = sess.CurrentUser.Email;
            string themeKey = theme.ToString();

            var user = _userRepository.FindActiveUserByEmail(userEmail);
            if (user == null)
            {
                Debug.LogWarning("[LocalUserData] MarkProblemSolvedForCurrentUser: user not found or inactive");
                return Result.Fail(AuthError.NotFoundOrInactive);
            }

            var existing = _resultRepository
                .GetResultsByUser(userEmail)
                ?.FirstOrDefault(r => r.ProblemIndex == problemIndex &&
                                      r.Theme == themeKey);

            if (existing != null)
            {
                return Result.Success();
            }

            var result = new ResultDoc
            {
                UserId = user.Id,
                Theme = themeKey,
                ProblemIndex = problemIndex,
                Score = 0,
                CorrectRate = null,
                DurationSec = null,
                MetaJson = null,
                CreatedAt = DateTime.UtcNow
            };

            _resultRepository.InsertResult(result);

            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] MarkProblemSolvedForCurrentUser: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }





}
