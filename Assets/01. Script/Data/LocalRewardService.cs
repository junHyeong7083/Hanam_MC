using System;
using System.Linq;
using UnityEngine;

public interface IRewardService
{
    // 현재 로그인 사용자 기준 보상 Attempt + 인벤토리 저장
    Result SaveRewardForCurrentUser(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        object payload,
        string itemId,
        string itemName
    );

    // 인벤토리 직접 지급
    Result GrantInventoryItem(string userEmail, InventoryItem item);

    // 인벤토리 조회
    Result<InventoryItem[]> GetInventory(string userEmail);
}

public class LocalRewardService : IRewardService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProgressService _progressService;

    public LocalRewardService(
        IInventoryRepository inventoryRepository,
        IUserRepository userRepository,
        IProgressService progressService)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
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
            Debug.LogWarning("[RewardService] 세션/유저 없음 - 보상 저장 스킵");
            return Result.Fail(AuthError.Internal, "세션 정보가 없습니다.");
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

    public Result GrantInventoryItem(string userEmail, InventoryItem item)
    {
        if (item == null)
            return Result.Fail(AuthError.Internal, "InventoryItem is null");

        try
        {
            var user = _userRepository.FindActiveUserByEmail(userEmail);
            if (user == null)
                return Result.Fail(AuthError.NotFoundOrInactive);

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
