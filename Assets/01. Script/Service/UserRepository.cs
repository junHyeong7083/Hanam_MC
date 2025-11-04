using System;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

public interface IUserRepository
{
    bool ExistsEmail(string email);
    bool HasSuperAdmin();
    User FindActiveByEmail(string email);
    User FindById(string id);
    void Insert(User u);
    void Update(User u);

    UserSummary[] SearchUsersFriendly(string query);
    UserSummary[] ListAllUsers(int limit = 0);
}

public class UserRepository : IUserRepository
{
    private const string COL = "users";

    static class Bootstrapper
    {
        static bool _done;

        public static void Ensure(ILiteDatabase db)
        {
            if (_done) return;
            var users = db.GetCollection<User>(COL);

            users.EnsureIndex(u => u.Email, unique: true);
            users.EnsureIndex(u => u.IsActive, unique: false);
            users.EnsureIndex(u => u.CreatedAt, unique: false);

            // --- 기존 레코드 Email을 소문자/트림으로 정규화 (1회) ---
            bool dirty = false;
            foreach (var u in users.FindAll())
            {
                var norm = (u.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.Equals(u.Email, norm, StringComparison.Ordinal))
                {
                    // (주의) 서로 다른 문서가 대소문자만 달라 같은 이메일이면 Unique 인덱스 충돌 가능
                    // 운영 중이면 사전 중복 정리 필요. 로컬/개발이라면 그대로 normalize.  => 우선 개발 후 추후 이메일 중복 방지 함수 추가하기
                    u.Email = norm;
                    users.Update(u);
                    dirty = true;
                }
            }
            if (dirty) db.Checkpoint();

            _done = true;
        }

    }

    private ILiteCollection<User> Col(ILiteDatabase db) => db.GetCollection<User>(COL);

    private static string NormalizeEmail(string email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    public bool ExistsEmail(string email)
    {
        var e = NormalizeEmail(email);
        if (string.IsNullOrEmpty(e)) return false;

        return DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            return Col(db).Exists(x => x.Email == e);
        });
    }

    public bool HasSuperAdmin()
    {
        return DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            return Col(db).Exists(x => x.Role == UserRole.SUPERADMIN);
        });
    }

    public User FindActiveByEmail(string email)
    {
        var e = NormalizeEmail(email);
        if (string.IsNullOrEmpty(e)) return null;

        return DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            return Col(db).FindOne(u => u.Email == e && u.IsActive);
        });
    }

    public User FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var i = id.Trim();
        return DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            return Col(db).FindById(i);
        });
    }

    public void Insert(User u)
    {
        DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            var users = Col(db);

            // 정규화: Email만 강제, 이름 관련 레거시는 건드리지 않음
            u.Email = NormalizeEmail(u.Email);
            users.Insert(u);
            return true;
        });
    }

    public void Update(User u)
    {
        DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            var users = Col(db);

            // 정규화: Email만 강제
            u.Email = NormalizeEmail(u.Email);
            users.Update(u);
            return true;
        });
    }


    public UserSummary[] ListAllUsers(int limit = 0)
    {
        return DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            IEnumerable<User> q = Col(db).FindAll().OrderBy(u => u.Name);
            if (limit > 0) q = q.Take(limit);
            return q.Select(ToSummary).ToArray();
        });
    }


    public UserSummary[] SearchUsersFriendly(string query)
    {
        return DBHelper.With(db =>
        {
            Bootstrapper.Ensure(db);
            var users = Col(db);

            var qRaw = (query ?? string.Empty).Trim();
            var qLower = qRaw.ToLowerInvariant();

            // 0) 빈 쿼리 → 기본 목록(최근/이름순)
            if (qRaw.Length == 0)
            {
                return users.Query()
                            .OrderBy(u => u.Name ?? string.Empty)
                            .Limit(200)
                            .ToList()
                            .Select(ToSummary)
                            .ToArray();
            }

            // 1) 정확 일치 (Email == qLower) — 인덱스 타는 경로
            var exactByEmail = users.FindOne(x => x.Email == qLower);
            if (exactByEmail != null)
                return new[] { ToSummary(exactByEmail) };

            // 2) Email 접두(prefix) — 인덱스 기반
            var emailPrefix = users.Find(Query.StartsWith(nameof(User.Email), qLower))
                                   .Take(200)
                                   .ToList();

            // 3) 이름 보조검색(소규모): 최근 N명만 메모리에서 case-insensitive 부분일치
            //    - 전량 스캔 금지; 운영 규모 고려해 N 조절 (여기선 2000)
            const int NAME_SCAN_LIMIT = 2000;
            var recentChunk = users.Query()
                                   .OrderByDescending(u => u.CreatedAt)
                                   .Limit(NAME_SCAN_LIMIT)
                                   .ToList();

            var nameHits = recentChunk
                .Where(u => !string.IsNullOrEmpty(u.Name) &&
                            u.Name.IndexOf(qRaw, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            var merged = emailPrefix
                .Concat(nameHits)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .OrderBy(u => u.Name ?? string.Empty)
                .Take(200)
                .Select(ToSummary)
                .ToArray();

            return merged;
        });
    }

    static UserSummary ToSummary(User u) => new UserSummary
    {
        Email = u.Email,
        Name = u.Name,
        Role = u.Role,
        IsActive = u.IsActive
    };
}
