using System;
using System.Linq;

// Admin 권한/활성 상태 변경 및 관리자용 사용자 검색
public partial class DBGateway
{
    public bool TrySetUserRole(string actingUserId, string targetUserId, UserRole role)
    {
        return WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);

            var acting = users.FindById(actingUserId);
            if (acting == null) return false;

            if (acting.Role != UserRole.SUPERADMIN && acting.Role != UserRole.ADMIN)
                return false;

            var target = users.FindById(targetUserId);
            if (target == null) return false;

            if (target.Role == UserRole.SUPERADMIN) return false;
            if (role == UserRole.SUPERADMIN) return false;

            bool hasAnyAdmin = users.Exists(u => u.Role == UserRole.ADMIN);

            if (!hasAnyAdmin && role == UserRole.ADMIN && acting.Role != UserRole.SUPERADMIN)
                return false;

            if (target.Role == UserRole.USER && role == UserRole.ADMIN)
            {
                target.Role = UserRole.ADMIN;
                return users.Update(target);
            }

            if (target.Role == UserRole.ADMIN && role == UserRole.USER)
            {
                target.Role = UserRole.USER;
                return users.Update(target);
            }

            return false;
        });
    }

    public bool TrySetUserActive(string actingUserId, string targetUserId, bool active)
    {
        return WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);

            var acting = users.FindById(actingUserId);
            if (acting == null || (acting.Role != UserRole.SUPERADMIN && acting.Role != UserRole.ADMIN))
                return false;

            var target = users.FindById(targetUserId);
            if (target == null) return false;

            if (!active && target.Id == actingUserId)
                return false;

            if (!active && target.Role == UserRole.SUPERADMIN)
                return false;

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

    public User[] SearchUsersRaw(string actingUserId, string contains = "")
    {
        return WithDb(db =>
        {
            var users = db.GetCollection<User>(CUsers);
            var act = users.FindById(actingUserId);
            if (act == null || (int)act.Role < (int)UserRole.ADMIN)
                return Array.Empty<User>();

            string q = (contains ?? string.Empty).Trim().ToLower();

            return users.Find(u =>
                        string.IsNullOrEmpty(q) ||
                        (u.Email ?? string.Empty).ToLower().Contains(q) ||
                        (u.Name ?? string.Empty).ToLower().Contains(q))
                    .OrderByDescending(u => u.CreatedAt)
                    .ToArray();
        });
    }
}
