using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// IProgressService - 사용자 진행도 관리 서비스 인터페이스
///
/// 【역할】 사용자의 문제 풀이 진행도를 조회하고, 시도(Attempt) 기록 저장,
///          문제 클리어 처리를 담당하는 서비스 인터페이스.
/// 【참조하는 곳】 ProblemStepBase(문제 풀이 중 Attempt 저장),
///                LocalRewardService(보상 저장 시 Attempt 저장 위임),
///                HomeScene 컨트롤러(진행도 표시)
/// </summary>
public interface IProgressService
{
    /// <summary>사용자 진행도 요약 조회 (총 세션 수, 총 풀이 수, 마지막 세션 시각)</summary>
    Result<UserProgress> FetchProgress(string userEmail);

    /// <summary>사용자가 풀이 완료한 문제 번호 목록 조회 (테마별)</summary>
    Result<int[]> FetchSolvedProblemIndexes(string userEmail, ProblemTheme theme);

    /// <summary>Attempt(시도) 기록을 DB에 저장</summary>
    Result SaveAttempt(Attempt attempt);

    /// <summary>현재 로그인 사용자 기준으로 Attempt 저장 (문제 풀이 중 자동 호출)</summary>
    Result SaveStepAttemptForCurrentUser(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        object payload
    );

    /// <summary>현재 로그인 사용자 기준으로 "이 문제 풀이 완료" 기록 (ResultDoc 생성)</summary>
    Result MarkProblemSolvedForCurrentUser(ProblemTheme theme, int problemIndex);
}

/// <summary>
/// LocalProgressService - IProgressService의 로컬(LiteDB) 구현체
///
/// 【역할】 사용자의 문제 풀이 진행도를 관리한다.
///          Attempt 기록 저장, 문제 클리어(ResultDoc 생성), 진행도 조회를 수행한다.
///          현재 로그인 사용자 정보는 SessionManager에서 가져온다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, DataService.Instance.Progress로 접근
/// 【참조되는 곳】 IProgressRepository (Attempt 저장, 진행도 조회),
///                IUserRepository (사용자 확인), IResultRepository (결과 저장/조회)
/// 【흐름】 ProblemStepBase → Progress.SaveStepAttemptForCurrentUser() → Attempt DB 저장
///         CommonRewardStep → Progress.MarkProblemSolvedForCurrentUser() → ResultDoc DB 저장
/// </summary>
public class LocalProgressService : IProgressService
{
    /// <summary>진행도/시도 기록 데이터 접근용 Repository</summary>
    private readonly IProgressRepository _progressRepository;
    /// <summary>사용자 조회용 Repository (MarkProblemSolved에서 사용자 존재 확인)</summary>
    private readonly IUserRepository _userRepository;
    /// <summary>결과 저장/조회용 Repository (문제 클리어 기록)</summary>
    private readonly IResultRepository _resultRepository;

    /// <summary>
    /// 생성자. DataService.Awake()에서 Repository들을 주입받아 생성된다.
    /// </summary>
    public LocalProgressService(
        IProgressRepository progressRepository,
        IUserRepository userRepository,
        IResultRepository resultRepository)
    {
        _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
    }

    /// <summary>
    /// 사용자 진행도 요약을 조회한다. ProgressRepository에서 sessions/results를 집계하여 반환한다.
    /// </summary>
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

    /// <summary>
    /// 사용자가 특정 테마에서 풀이 완료한 문제 번호 목록을 조회한다.
    /// HomeScene의 문제 선택 UI에서 이미 푼 문제를 표시할 때 사용된다.
    /// </summary>
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

    /// <summary>
    /// Attempt(시도) 기록을 DB에 저장한다.
    /// UserId/UserEmail이 비어있으면 현재 로그인 사용자 정보로 자동 채운다.
    /// Id와 CreatedAt도 비어있으면 자동 생성한다.
    /// </summary>
    public Result SaveAttempt(Attempt attempt)
    {
        if (attempt == null)
            return Result.Fail(AuthError.Internal, "Attempt is null");

        try
        {
            var sess = SessionManager.Instance;
            var currentUser = sess?.CurrentUser;

            // UserId/UserEmail이 비어있으면 현재 로그인 사용자로 자동 채움
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

    /// <summary>
    /// 현재 로그인 사용자의 스텝 시도를 기록한다.
    /// payload를 JsonUtility.ToJson()으로 직렬화하여 Attempt.Content에 저장한다.
    /// 미로그인 시에는 저장을 스킵하고 실패를 반환한다.
    /// </summary>
    /// <param name="theme">테마 (Director/Gardener)</param>
    /// <param name="problemIndex">문제 번호 (1~10)</param>
    /// <param name="problemId">Problem.Id (DB 참조용)</param>
    /// <param name="payload">사용자 응답 데이터 (JSON 직렬화됨)</param>
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
            Debug.LogWarning("[ProgressService] ����/���� ���� - Attempt ���� ��ŵ");
            return Result.Fail(AuthError.Internal, "���� ������ �����ϴ�.");
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

    /// <summary>
    /// 현재 로그인 사용자의 문제 풀이 완료를 기록한다.
    /// 같은 테마+문제번호의 ResultDoc이 이미 존재하면 중복 생성하지 않고 성공을 반환한다.
    /// 새로운 ResultDoc을 생성하여 results 컬렉션에 저장한다.
    /// </summary>
    /// <param name="theme">풀이 완료한 테마</param>
    /// <param name="problemIndex">풀이 완료한 문제 번호 (1~10)</param>
    public Result MarkProblemSolvedForCurrentUser(ProblemTheme theme, int problemIndex)
    {
        var sess = SessionManager.Instance;
        if (sess == null || sess.CurrentUser == null)
        {
            Debug.LogWarning("[ProgressService] ����/���� ���� - ���� Ŭ���� ���� ��ŵ");
            return Result.Fail(AuthError.Internal, "���� ������ �����ϴ�.");
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
