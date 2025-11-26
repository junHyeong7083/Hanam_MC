using System;
using UnityEngine;
using System.Linq;
/// <summary>
/// 사용자 문제 풀이 / 진행도용 로컬 데이터 서비스.
/// 실제 DB 접근은 Repository들(Progress/Problem/Result/Inventory)을 통해서만 한다.
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

public class LocalUserDataService : IUserDataService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProgressRepository _progressRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IResultRepository _resultRepository;
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
