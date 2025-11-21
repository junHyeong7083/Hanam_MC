using System;
using UnityEngine;
using System.Linq;
/// <summary>
/// 사용자 문제 풀이 / 진행도용 로컬 데이터 서비스.
/// 실제 DB 접근은 DbGateway를 통해서만 한다.
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
    readonly DBGateway _db;

    public LocalUserDataService(DBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Result<UserProgress> FetchProgress(string userEmail)
    {
        try
        {
            var progress = _db.GetUserProgress(userEmail);
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
            var p = _db.GetProblemById(problemId);
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
            if (string.IsNullOrEmpty(attempt.Id))
                attempt.Id = Guid.NewGuid().ToString();
            if (attempt.CreatedAt == default)
                attempt.CreatedAt = DateTime.UtcNow;

            _db.InsertAttempt(attempt);
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
            // 1) Id로 직접 조회
            var r = _db.GetResultById(resultIdOrSessionId);
            if (r != null)
                return Result<ResultDoc>.Success(r);

            // 2) 나중에 필요하면 MetaJson 등에 세션ID를 저장해서 추가 검색 로직 확장 가능
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

            var indexes = _db.GetSolvedProblemIndexes(userEmail, themeKey)
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
        if (SessionManager.Instance == null ||
            SessionManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[LocalUserData] 세션/유저 없음 - Attempt 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        try
        {
            string userEmail = SessionManager.Instance.CurrentUser.Email;
            string sessionId = SessionManager.Instance.SessionId;

            string contentJson = JsonUtility.ToJson(payload);

            var attempt = new Attempt
            {
                SessionId = sessionId,
                UserEmail = userEmail,
                Content = contentJson,
                ProblemId = string.IsNullOrEmpty(problemId) ? null : problemId,
                Theme = theme,
                ProblemIndex = problemIndex
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
        if (SessionManager.Instance == null ||
            SessionManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[LocalUserData] 세션/유저 없음 - 보상 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        string userEmail = SessionManager.Instance.CurrentUser.Email;

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
                UserEmail = userEmail,
                ItemId = itemId,
                ItemName = itemName,
                Theme = theme,
                AcquiredAt = DateTime.UtcNow
            };

            return GrantInventoryItem(userEmail, invItem);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalUserData] SaveRewardForCurrentUser error: {ex}");
            return Result.Fail(AuthError.InventoryError, "인벤토리 저장 중 오류가 발생했습니다.");
        }
    }

    // =========================
    // 인벤토리 관련 메서드
    // =========================
    public Result GrantInventoryItem(string userEmail, InventoryItem item)
    {
        if (item == null)
            return Result.Fail(AuthError.Internal, "Inventory item is null");

        try
        {
            // 이미 있으면 조용히 성공 처리
            if (_db.HasInventoryItem(userEmail, item.ItemId))
                return Result.Success();

            _db.AddInventoryItem(item);
            return Result.Success();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalUserData] GrantInventoryItem error: {ex}");
            return Result.Fail(AuthError.InventoryError, "인벤토리 저장 중 오류가 발생했습니다.");
        }
    }

    public Result<InventoryItem[]> GetInventory(string userEmail)
    {
        try
        {
            var list = _db.GetInventoryByUser(userEmail);
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

            // 1) 현재 로그인한 유저 찾기
            var user = _db.FindActiveUserByEmail(userEmail);
            if (user == null)
            {
                Debug.LogWarning("[LocalUserData] MarkProblemSolvedForCurrentUser: user not found or inactive");
                return Result.Fail(AuthError.NotFoundOrInactive);
            }

            // 2) 이미 같은 Theme + Stage 결과가 있는지 체크 (중복 방지)
            var existing = _db
                .GetResultsByUser(userEmail)
                ?.FirstOrDefault(r => r.Stage == problemIndex && r.Theme == themeKey);

            // 이미 기록되어 있으면 그냥 성공 처리
            if (existing != null)
            {
                return Result.Success();
            }

            // 3) ResultDoc 한 줄 생성해서 '이 문제를 풀었다' 기록
            var result = new ResultDoc
            {
                UserId = user.Id,
                Theme = themeKey,
                Stage = problemIndex,
                CreatedAt = DateTime.UtcNow
            };

            _db.InsertResult(result);

            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalUserData] MarkProblemSolvedForCurrentUser: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }



}
