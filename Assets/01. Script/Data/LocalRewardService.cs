using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// IRewardService - 보상(인벤토리 아이템) 관리 서비스 인터페이스
///
/// 【역할】 문제 풀이 보상 아이템 지급, 인벤토리 조회 기능을 정의한다.
/// 【참조하는 곳】 CommonRewardStep (보상 스텝에서 아이템 지급),
///                StepInventory (인벤토리 UI에서 아이템 목록 조회)
/// </summary>
public interface IRewardService
{
    /// <summary>현재 로그인 사용자에게 보상을 지급한다 (Attempt 로그 + 인벤토리 아이템 저장)</summary>
    Result SaveRewardForCurrentUser(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        object payload,
        string itemId,
        string itemName
    );

    /// <summary>특정 사용자에게 인벤토리 아이템을 직접 지급한다</summary>
    Result GrantInventoryItem(string userEmail, InventoryItem item);

    /// <summary>특정 사용자의 인벤토리 전체를 조회한다</summary>
    Result<InventoryItem[]> GetInventory(string userEmail);
}

/// <summary>
/// LocalRewardService - IRewardService의 로컬(LiteDB) 구현체
///
/// 【역할】 문제 풀이 보상을 처리한다. Attempt 기록 저장은 ProgressService에 위임하고,
///          인벤토리 아이템 지급은 InventoryRepository를 통해 DB에 저장한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, DataService.Instance.Reward로 접근
/// 【참조되는 곳】 IInventoryRepository (아이템 저장/조회),
///                IUserRepository (사용자 존재 확인),
///                IProgressService (Attempt 저장 위임)
/// 【흐름】 SaveRewardForCurrentUser() → ProgressService.SaveStepAttemptForCurrentUser()
///         → GrantInventoryItem() → InventoryRepository.Add()
/// </summary>
public class LocalRewardService : IRewardService
{
    /// <summary>인벤토리 아이템 저장/조회용 Repository</summary>
    private readonly IInventoryRepository _inventoryRepository;
    /// <summary>사용자 존재 확인용 Repository</summary>
    private readonly IUserRepository _userRepository;
    /// <summary>Attempt 저장을 위임할 ProgressService</summary>
    private readonly IProgressService _progressService;

    /// <summary>
    /// 생성자. DataService.Awake()에서 Repository와 ProgressService를 주입받아 생성된다.
    /// </summary>
    public LocalRewardService(
        IInventoryRepository inventoryRepository,
        IUserRepository userRepository,
        IProgressService progressService)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
    }

    /// <summary>
    /// 현재 로그인 사용자에게 보상을 지급한다.
    /// 1) ProgressService를 통해 Attempt 로그를 저장하고,
    /// 2) InventoryRepository를 통해 아이템을 인벤토리에 추가한다.
    /// 미로그인 시에는 저장을 스킵하고 실패를 반환한다.
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
            Debug.LogWarning("[RewardService] 세션/유저 없음 - 보상 저장 스킵");
            return Result.Fail(AuthError.Internal, "로그인 상태가 아닙니다.");
        }

        string userEmail = currentUser.Email;

        // 1) Attempt 로그 저장 (ProgressService에 위임)
        var attemptResult = _progressService.SaveStepAttemptForCurrentUser(
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
            Debug.LogError($"[RewardService] SaveRewardForCurrentUser error: {ex}");
            return Result.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 특정 사용자에게 인벤토리 아이템을 직접 지급한다.
    /// 사용자 존재/활성 여부를 확인한 후, UserId/Email을 보정하여 DB에 저장한다.
    /// </summary>
    public Result GrantInventoryItem(string userEmail, InventoryItem item)
    {
        if (item == null)
            return Result.Fail(AuthError.Internal, "InventoryItem is null");

        try
        {
            // 사용자 존재/활성 여부 확인
            var user = _userRepository.FindActiveUserByEmail(userEmail);
            if (user == null)
                return Result.Fail(AuthError.NotFoundOrInactive);

            // UserId/Email을 DB의 실제 값으로 보정
            item.UserId = user.Id;
            item.UserEmail = user.Email;

            if (item.AcquiredAt == default)
                item.AcquiredAt = DateTime.UtcNow;

            _inventoryRepository.Add(item);
            return Result.Success();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RewardService] GrantInventoryItem: {e}");
            return Result.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 특정 사용자의 인벤토리 전체를 조회한다.
    /// StepInventory UI에서 획득한 아이템 목록을 표시할 때 호출된다.
    /// </summary>
    public Result<InventoryItem[]> GetInventory(string userEmail)
    {
        try
        {
            var list = _inventoryRepository.GetByUser(userEmail);
            var arr = (list != null) ? list.ToArray() : Array.Empty<InventoryItem>();
            Debug.Log("[RewardService] Inventory List " + list?.Count);
            return Result<InventoryItem[]>.Success(arr);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RewardService] GetInventory error: {ex}");
            return Result<InventoryItem[]>.Fail(AuthError.InventoryError);
        }
    }
}
