using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AdminService - 관리자 권한 관리 정적 서비스 (현재 미구현 기능)
///
/// 【역할】 사용자 권한 변경, 활성/비활성 전환, 관리자 검색 기능을 정적 메서드로 제공한다.
///          DataService.Instance.UserRepository에 직접 위임하는 파사드(Facade) 역할.
/// 【참조하는 곳】 관리자 UI (현재 미구현)
/// 【참조되는 곳】 DataService.Instance.UserRepository (실제 DB 작업 위임)
/// 【상태】 아직 해당 클래스를 이용한 기능은 미구현
/// </summary>
public static class AdminService
{
    /// <summary>DataService에서 UserRepository를 가져오는 편의 프로퍼티</summary>
    private static IUserRepository Users => DataService.Instance.UserRepository;

    /// <summary>
    /// 대상 사용자의 권한을 변경한다. actingUser가 ADMIN 이상이어야 한다.
    /// UserRepository.TrySetUserRole()에 위임한다.
    /// </summary>
    public static bool SetRole(string actingUserId, string targetUserId, UserRole role)
        => Users.TrySetUserRole(actingUserId, targetUserId, role);

    /// <summary>
    /// 대상 사용자의 활성 상태를 변경한다. actingUser가 ADMIN 이상이어야 한다.
    /// UserRepository.TrySetUserActive()에 위임한다.
    /// </summary>
    public static bool SetActive(string actingUserId, string targetUserId, bool active)
        => Users.TrySetUserActive(actingUserId, targetUserId, active);

    /// <summary>
    /// 관리자가 사용자를 검색한다. actingUser가 ADMIN 이상이어야 한다.
    /// contains 문자열로 이메일/이름을 부분 검색한다.
    /// </summary>
    public static List<User> SearchUsers(string actingUserId, string contains = "")
        => Users.SearchUsersRaw(actingUserId, contains).ToList();
}
