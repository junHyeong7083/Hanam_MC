using System;
using UnityEngine;

/// <summary>
/// IResultQueryService - 결과(ResultDoc) 조회 서비스 인터페이스
///
/// 【역할】 결과 ID로 개별 ResultDoc을 조회하는 기능을 정의한다.
/// 【참조하는 곳】 DataService.Instance.Results로 접근, ResultScene 컨트롤러
/// </summary>
public interface IResultQueryService
{
    /// <summary>결과 ID 또는 세션 ID로 ResultDoc을 조회한다</summary>
    Result<ResultDoc> FetchResult(string resultIdOrSessionId);
}

/// <summary>
/// LocalResultQueryService - IResultQueryService의 로컬(LiteDB) 구현체
///
/// 【역할】 ResultRepository를 통해 LiteDB에서 결과 데이터를 조회한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성
/// 【참조되는 곳】 IResultRepository (실제 DB 접근)
/// </summary>
public class LocalResultQueryService : IResultQueryService
{
    /// <summary>결과 데이터 접근용 Repository</summary>
    private readonly IResultRepository _resultRepository;

    public LocalResultQueryService(IResultRepository resultRepository)
    {
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
    }

    /// <summary>
    /// 결과 ID로 ResultDoc을 조회한다. 존재하지 않으면 NotFoundOrInactive 에러를 반환한다.
    /// </summary>
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
