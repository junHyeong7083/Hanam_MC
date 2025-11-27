using System;
using UnityEngine;

public interface IResultQueryService
{
    Result<ResultDoc> FetchResult(string resultIdOrSessionId);
}

public class LocalResultQueryService : IResultQueryService
{
    private readonly IResultRepository _resultRepository;

    public LocalResultQueryService(IResultRepository resultRepository)
    {
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
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
            Debug.LogError($"[ResultQueryService] FetchResult: {e}");
            return Result<ResultDoc>.Fail(AuthError.Internal);
        }
    }
}
