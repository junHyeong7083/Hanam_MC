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
    /// <summary>
    /// SUPERADMIN 또는 ADMIN용도
    /// 
    ///  USER -> ADMIN(승격)
    ///  ADMIN -> USER(강등)
    ///  
    /// 첫번째 ADMIN을 만드는 시점에는 SUPERADMIN 사용
    /// SUPERADMIN의 권한은 변경 및 부여불가
    /// </summary>
    public static bool SetRole(string actingUserId, string targetUserId, UserRole role)
    {
        return DBHelper.With(db =>
        {
            var users = db.GetCollection<User>("users");

            var acting = users.FindById(actingUserId);
            if (acting == null) return false;

            // 실행 주체는 SUPERADMIN 또는 ADMIN만 가능
            if (acting.Role != UserRole.SUPERADMIN && acting.Role != UserRole.ADMIN)
                return false;

            var target = users.FindById(targetUserId);
            if (target == null) return false;

            // SUPERADMIN의 권한은 절대 변경 불가
            if (target.Role == UserRole.SUPERADMIN) return false;

            // SUPERADMIN 권한 부여 금지
            if (role == UserRole.SUPERADMIN) return false;

            // 현재 시스템에 ADMIN이 1명이라도 있는가?
            bool hasAnyAdmin = users.Exists(u => u.Role == UserRole.ADMIN);

            // 첫 번째 ADMIN을 만드는 순간에는 SUPERADMIN만 허용
            if (!hasAnyAdmin && role == UserRole.ADMIN && acting.Role != UserRole.SUPERADMIN)
                return false;

            // 허용되는 변경: USER<->ADMIN
            // 1) 승격: USER -> ADMIN
            if (target.Role == UserRole.USER && role == UserRole.ADMIN)
            {
                target.Role = UserRole.ADMIN;
                return users.Update(target);
            }

            // 2) 강등: ADMIN -> USER  (ADMIN도 가능)
            if (target.Role == UserRole.ADMIN && role == UserRole.USER)
            {
                target.Role = UserRole.USER;
                return users.Update(target);
            }

            // 그 외 조합은 모두 금지
            return false;
        });
    }

    // 관리자(ADMIN, SUPERADMIN)만: 활성/비활성
    public static bool SetActive(string actingUserId, string targetUserId, bool active)
    {
        return DBHelper.With(db =>
        {
            var users = db.GetCollection<User>("users");

            var acting = users.FindById(actingUserId);
            // acting이 없거나, ADMIN/SUPERADMIN 둘 다 아니면 거부
            if (acting == null || (acting.Role != UserRole.SUPERADMIN && acting.Role != UserRole.ADMIN))
                return false;

            var target = users.FindById(targetUserId);
            if (target == null) return false;

            // 자기 자신 비활성화 방지(실수 보호)
            if (!active && target.Id == actingUserId)
                return false;

            // SUPERADMIN 비활성화 금지
            if (!active && target.Role == UserRole.SUPERADMIN)
                return false;

            // 마지막 활성 관리자 보호(ADMIN/SUPERADMIN이 최소 1명은 남아야 함)
            if (!active && (target.Role == UserRole.ADMIN || target.Role == UserRole.SUPERADMIN))
            {
                bool stillHasAdmin = users.Exists(u =>
                    u.Id != target.Id &&
                    u.IsActive &&
                    (u.Role == UserRole.ADMIN || u.Role == UserRole.SUPERADMIN));
                if (!stillHasAdmin) return false;
            }

            target.IsActive = active;
            return users.Update(target);
        });
    }


    // ADMIN 이상: 사용자 검색
    public static List<User> SearchUsers(string actingUserId, string contains = "")
    {
        return DBHelper.With(db =>
        {
            var users = db.GetCollection<User>("users");
            var act = users.FindById(actingUserId);
            if (act == null || (int)act.Role < (int)UserRole.ADMIN)
                return new List<User>();

            string q = (contains ?? string.Empty).Trim().ToLower();

            return users.Find(u =>
                        string.IsNullOrEmpty(q) ||
                        (u.Email ?? string.Empty).ToLower().Contains(q) ||
                        (u.Name ?? string.Empty).ToLower().Contains(q))
                    .OrderByDescending(u => u.CreatedAt)
                    .ToList();
        });
    }

}