using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 관리자 권한 관리 서비스
/// 
/// 1. 사용자 권한 변경
/// 2. 사용자 활성/비활성 전환
/// 
/// 
/// 아직 해당 클래스를 이용한 기능은 미구현
/// </summary>
public static class AdminService
{
    public static bool SetRole(string actingUserId, string targetUserId, UserRole role)
        => DataService.Instance.Db.TrySetUserRole(actingUserId, targetUserId, role);

    public static bool SetActive(string actingUserId, string targetUserId, bool active)
        => DataService.Instance.Db.TrySetUserActive(actingUserId, targetUserId, active);

    public static List<User> SearchUsers(string actingUserId, string contains = "")
        => DataService.Instance.Db.SearchUsersRaw(actingUserId, contains).ToList();
}
