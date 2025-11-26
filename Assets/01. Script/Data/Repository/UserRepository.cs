using System;
using System.Linq;
using UnityEngine;


public interface IUserRepository
{
    // ===== 기본 유저 정보 =====
    bool ExistsEmail(string email);
    bool HasSuperAdmin();
    User FindActiveUserByEmail(string email);
    User FindUserById(string id);
    void InsertUser(User user);
    void UpdateUser(User user);

    // ===== 요약 / 검색 =====
    UserSummary[] SearchUsersFriendly(string query);
    UserSummary[] ListAllUsers(int limit = 0);

    // ===== 관리자용 권한/활성 변경 =====
    bool TrySetUserRole(string actingUserId, string targetUserId, UserRole role);
    bool TrySetUserActive(string actingUserId, string targetUserId, bool active);
    User[] SearchUsersRaw(string actingUserId, string contains = "");
}
public class UserRepository : IUserRepository
{
    private readonly IDBGateway _db;
    private const string CUsers = "users";

    public UserRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // ===== 기본 유저 정보 =====

    public bool ExistsEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email, true);
            return col.Exists(u => u.Email == email);
        });
    }

    public bool HasSuperAdmin()
    {
        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Role);
            return col.Exists(u => u.Role == UserRole.SUPERADMIN);
        });
    }

    public User FindActiveUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email, true);
            return col.FindOne(u => u.Email == email && u.IsActive);
        });
    }

    public User FindUserById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);
            return col.FindById(id);
        });
    }

    public void InsertUser(User u)
    {
        if (u == null) throw new ArgumentNullException(nameof(u));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);
            col.EnsureIndex(x => x.Email, true);
            col.Insert(u);
        });
    }

    public void UpdateUser(User u)
    {
        if (u == null) throw new ArgumentNullException(nameof(u));

        _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);
            col.Update(u);
        });
    }

    // ===== 요약 / 검색 =====

    public UserSummary[] SearchUsersFriendly(string query)
    {
        query = (query ?? string.Empty).Trim();

        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email);
            col.EnsureIndex(x => x.Name);
            col.EnsureIndex(x => x.LowerName);
            col.EnsureIndex(x => x.NameChosung);

            if (string.IsNullOrEmpty(query))
            {
                return col.FindAll()
                          .Select(ToSummary)
                          .ToArray();
            }

            string lower = query.ToLowerInvariant();

            var q1 = col.Find(u =>
                (u.Email != null && u.Email.ToLower().Contains(lower)) ||
                (u.Name != null && u.Name.Contains(query)) ||
                (u.LowerName != null && u.LowerName.Contains(lower)) ||
                (u.NameChosung != null && u.NameChosung.Contains(query))
            );

            return q1.Select(ToSummary).ToArray();
        });
    }

    public UserSummary[] ListAllUsers(int limit = 0)
    {
        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            var q = col.FindAll();

            if (limit > 0)
                q = q.Take(limit);

            return q.Select(ToSummary).ToArray();
        });
    }

    private static UserSummary ToSummary(User u) => new UserSummary
    {
        Email = u.Email,
        Name = u.Name,
        Role = u.Role,
        IsActive = u.IsActive
    };

    // ===== 관리자용 권한/활성 변경 =====

    public bool TrySetUserRole(string actingUserId, string targetUserId, UserRole role)
    {
        return _db.WithDb(db =>
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
        return _db.WithDb(db =>
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
        return _db.WithDb(db =>
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
