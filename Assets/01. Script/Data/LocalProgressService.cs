using System;
using System.Linq;
using UnityEngine;

public interface IProgressService
{
    // 나의 진행 요약
    Result<UserProgress> FetchProgress(string userEmail);

    // 사용자가 푼 문제 번호 목록
    Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme);

    // Attempt 단건 저장
    Result SaveAttempt(Attempt attempt);

    // 현재 로그인 사용자 기준 Attempt 저장 (문제 풀이용 헬퍼)
    Result SaveStepAttemptForCurrentUser(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        object payload
    );

    // 현재 로그인 사용자 기준 "이 문제 풀었다" 기록
    Result MarkProblemSolvedForCurrentUser(ProblemTheme theme, int problemIndex);
}

public class LocalProgressService : IProgressService
{
    private readonly IProgressRepository _progressRepository;
    private readonly IUserRepository _userRepository;
    private readonly IResultRepository _resultRepository;

    public LocalProgressService(
        IProgressRepository progressRepository,
        IUserRepository userRepository,
        IResultRepository resultRepository)
    {
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
            Debug.LogError($"[ProgressService] FetchProgress: {e}");
            return Result<UserProgress>.Fail(AuthError.Internal);
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
            Debug.LogError($"[ProgressService] FetchSolvedProblemIndexes: {e}");
            return Result<int[]>.Fail(AuthError.Internal);
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
            Debug.LogError($"[ProgressService] SaveAttempt: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }

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
            Debug.LogWarning("[ProgressService] 세션/유저 없음 - Attempt 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        try
        {
            string json = payload != null
                ? JsonUtility.ToJson(payload)
                : null;

            var attempt = new Attempt
            {
                UserId = currentUser.Id,
                UserEmail = currentUser.Email,
                SessionId = null,
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
            Debug.LogError($"[ProgressService] SaveStepAttemptForCurrentUser: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }

    public Result MarkProblemSolvedForCurrentUser(ProblemTheme theme, int problemIndex)
    {
        var sess = SessionManager.Instance;
        if (sess == null || sess.CurrentUser == null)
        {
            Debug.LogWarning("[ProgressService] 세션/유저 없음 - 문제 클리어 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
        }

        try
        {
            string userEmail = sess.CurrentUser.Email;
            string themeKey = theme.ToString();

            var user = _userRepository.FindActiveUserByEmail(userEmail);
            if (user == null)
            {
                Debug.LogWarning("[ProgressService] MarkProblemSolvedForCurrentUser: user not found or inactive");
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
            Debug.LogError($"[ProgressService] MarkProblemSolvedForCurrentUser: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }
}
