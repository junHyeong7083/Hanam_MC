using System;
using UnityEngine;

/// <summary>
/// IProblemQueryService - 문제(Problem) 조회 서비스 인터페이스
///
/// 【역할】 ID로 문제를 조회하는 기능을 정의한다.
/// 【참조하는 곳】 DataService.Instance.Problems로 접근
/// </summary>
public interface IProblemQueryService
{
    /// <summary>문제 ID로 Problem 엔티티를 조회한다</summary>
    Result<Problem> FetchProblem(string problemId);
}

/// <summary>
/// LocalProblemQueryService - IProblemQueryService의 로컬(LiteDB) 구현체
///
/// 【역할】 ProblemRepository를 통해 LiteDB에서 문제 데이터를 조회한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성
/// 【참조되는 곳】 IProblemRepository (실제 DB 접근)
/// </summary>
public class LocalProblemQueryService : IProblemQueryService
{
    /// <summary>문제 데이터 접근용 Repository</summary>
    private readonly IProblemRepository _problemRepository;

    public LocalProblemQueryService(IProblemRepository problemRepository)
    {
        _problemRepository = problemRepository ?? throw new ArgumentNullException(nameof(problemRepository));
    }

    /// <summary>
    /// 문제 ID로 Problem을 조회한다. 존재하지 않으면 NotFoundOrInactive 에러를 반환한다.
    /// </summary>
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
            Debug.LogError($"[ProblemQueryService] FetchProblem: {e}");
            return Result<Problem>.Fail(AuthError.Internal);
        }
    }
}
