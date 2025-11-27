using System;
using UnityEngine;

public interface IProblemQueryService
{
    Result<Problem> FetchProblem(string problemId);
}

public class LocalProblemQueryService : IProblemQueryService
{
    private readonly IProblemRepository _problemRepository;

    public LocalProblemQueryService(IProblemRepository problemRepository)
    {
        _problemRepository = problemRepository ?? throw new ArgumentNullException(nameof(problemRepository));
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
            Debug.LogError($"[ProblemQueryService] FetchProblem: {e}");
            return Result<Problem>.Fail(AuthError.Internal);
        }
    }
}
