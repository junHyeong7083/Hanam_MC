using System;
using System.Linq;
using UnityEngine;


/// <summary>
/// IUserRepository - 사용자(User) 데이터 접근 인터페이스
///
/// 【역할】 User 엔티티의 CRUD, 검색, 권한/활성 상태 관리 기능을 정의한다.
/// 【참조하는 곳】 AuthService (가입/로그인), LocalProgressService (사용자 확인),
///                LocalRewardService (사용자 확인), LocalAdminDataService (사용자 검색),
///                AdminService (권한/활성 관리)
/// </summary>
public interface IUserRepository
{
    // ===== 기본 사용자 조작 =====
    /// <summary>이메일 중복 확인</summary>
    bool ExistsEmail(string email);
    /// <summary>SUPERADMIN 계정 존재 여부 확인</summary>
    bool HasSuperAdmin();
    /// <summary>이메일로 활성(IsActive=true) 사용자 조회. 비활성이면 null 반환</summary>
    User FindActiveUserByEmail(string email);
    /// <summary>ID로 사용자 조회 (활성/비활성 무관)</summary>
    User FindUserById(string id);
    /// <summary>새 사용자를 DB에 저장</summary>
    void InsertUser(User user);
    /// <summary>기존 사용자 정보를 수정</summary>
    void UpdateUser(User user);

    // ===== 목록 / 검색 =====
    /// <summary>이메일/이름/초성으로 사용자를 검색하여 UserSummary로 반환</summary>
    UserSummary[] SearchUsersFriendly(string query);
    /// <summary>전체 사용자 목록을 UserSummary로 반환 (limit: 최대 개수, 0=무제한)</summary>
    UserSummary[] ListAllUsers(int limit = 0);

    // ===== 관리자용 권한/활성 관리 =====
    /// <summary>대상 사용자의 권한을 변경한다 (관리자 권한 필요)</summary>
    bool TrySetUserRole(string actingUserId, string targetUserId, UserRole role);
    /// <summary>대상 사용자의 활성 상태를 변경한다 (관리자 권한 필요)</summary>
    bool TrySetUserActive(string actingUserId, string targetUserId, bool active);
    /// <summary>관리자가 사용자를 검색한다 (원본 User 배열 반환)</summary>
    User[] SearchUsersRaw(string actingUserId, string contains = "");
}

/// <summary>
/// UserRepository - IUserRepository의 LiteDB 구현체
///
/// 【역할】 LiteDB "users" 컬렉션에 대한 모든 사용자 관련 CRUD 작업을 수행한다.
///          이메일 중복 확인, 활성 사용자 조회, 관리자 검색, 권한/활성 상태 변경을 포함한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, AuthService/각종 Service에서 사용
/// 【참조되는 곳】 IDBGateway (DB 커넥션)
/// 【컬렉션명】 "users"
/// 【보안】 TrySetUserRole/TrySetUserActive는 actingUser의 권한을 검증한 후 수행
/// </summary>
public class UserRepository : IUserRepository
{
    /// <summary>DB 접근 게이트웨이</summary>
    private readonly IDBGateway _db;
    /// <summary>LiteDB 컬렉션명</summary>
    private const string CUsers = "users";

    public UserRepository(IDBGateway db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // ===== 기본 사용자 조작 =====

    /// <summary>해당 이메일이 이미 등록되어 있는지 확인한다</summary>
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

    /// <summary>SUPERADMIN 권한의 사용자가 존재하는지 확인한다. AuthService 초기화 시 사용</summary>
    public bool HasSuperAdmin()
    {
        return _db.WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Role);
            return col.Exists(u => u.Role == UserRole.SUPERADMIN);
        });
    }

    /// <summary>이메일로 활성(IsActive=true) 사용자를 조회한다. 비활성이거나 없으면 null</summary>
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

    /// <summary>ID(GUID)로 사용자를 조회한다. 활성/비활성 무관. 없으면 null</summary>
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

    /// <summary>새 사용자를 DB에 저장한다. Id와 Email에 유니크 인덱스를 생성한다</summary>
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

    /// <summary>기존 사용자 정보를 수정한다. Id 인덱스를 기준으로 업데이트</summary>
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

    // ===== 목록 / 검색 =====

    /// <summary>
    /// 이메일, 이름, 소문자이름, 초성으로 사용자를 검색하여 UserSummary 배열로 반환한다.
    /// query가 빈 문자열이면 전체 사용자를 반환한다.
    /// </summary>
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

    /// <summary>전체 사용자 목록을 UserSummary로 반환한다. limit > 0이면 최대 개수 제한</summary>
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

    /// <summary>User 엔티티를 UserSummary DTO로 변환한다 (민감 정보 제외)</summary>
    private static UserSummary ToSummary(User u) => new UserSummary
    {
        Email = u.Email,
        Name = u.Name,
        Role = u.Role,
        IsActive = u.IsActive
    };

    // ===== 관리자용 권한/활성 관리 =====

    /// <summary>
    /// 대상 사용자의 권한을 변경한다. actingUser가 ADMIN 이상이어야 하며,
    /// SUPERADMIN 권한은 변경할 수 없다. SUPERADMIN에게 권한 부여/해제도 불가.
    /// USER↔ADMIN 간 전환만 허용된다.
    /// </summary>
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

    /// <summary>
    /// 대상 사용자의 활성 상태를 변경한다. actingUser가 ADMIN 이상이어야 한다.
    /// 자기 자신 비활성화, SUPERADMIN 비활성화, 마지막 관리자 비활성화는 불가.
    /// </summary>
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

    /// <summary>
    /// 관리자가 사용자를 검색한다 (원본 User 배열 반환).
    /// actingUser가 ADMIN 이상이어야 하며, 이메일/이름에 contains 문자열이 포함된 사용자를 검색.
    /// 최신 가입순(CreatedAt 내림차순)으로 정렬하여 반환한다.
    /// </summary>
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
