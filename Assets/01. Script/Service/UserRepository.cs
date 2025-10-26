using System;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

// Model(User 등)은 Model.cs에 존재 (LowerName, NameChosung 필드 사용)  :contentReference[oaicite:1]{index=1}
public interface IUserRepository
{
    bool ExistsEmail(string email);
    bool HasSuperAdmin();
    User FindActiveByEmail(string email);
    User FindById(string id);
    void Insert(User u);
    void Update(User u);

    // 검색 (기존 시그니처 유지)
    UserSummary[] SearchUsersFriendly(string query);

    // 전체
    UserSummary[] ListAllUsers(int limit = 0);
}

public class UserRepository : IUserRepository
{
    // ===== 내부 구성요소 (SRP 분리; 파일 추가 없이 내부 클래스로 캡슐화) =====
    static class Bootstrapper
    {
        static bool _done;

        public static void Ensure(ILiteDatabase db)
        {
            if (_done) return;
            var users = db.GetCollection<User>("users");

            // 인덱스 보장
            users.EnsureIndex(u => u.Email, true);
            users.EnsureIndex(u => u.IsActive);
            users.EnsureIndex(u => u.LowerName);

            // 레거시 백필 (LowerName 채움)
            bool dirty = false;
            foreach (var u in users.FindAll())
            {
                if (string.IsNullOrEmpty(u.LowerName))
                {
                    u.LowerName = UserSearchUtility.NormalizeName(u.Name ?? "");
                    u.NameChosung = null; // 초성 검색 폐기 => 추후 개발예정
                    users.Update(u);
                    dirty = true;
                }
            }
            if (dirty) { /* no-op, 저장 완료 */ }

            _done = true;
        }
    }

    // 검색 규칙 클래스
    static class SearchPolicy
    {
        public static UserSummary[] Search(ILiteCollection<User> users, string query)
        {
            var qRaw = (query ?? string.Empty).Trim();
            if (qRaw.Length == 0)
                return users.FindAll().OrderBy(u => u.Name).Select(ToSummary).ToArray();

            var qLower = qRaw.ToLowerInvariant();
            var norm = UserSearchUtility.NormalizeName(qRaw); // 공백제거+소문자

            // 전체 한번만 로드 (LiteDB 내장 필터로 바꿔도 되지만, 정확/접두 혼합 규칙을 단일 패스에서 처리)
            var all = users.FindAll().ToList();

            bool IsExact(User u)
            {
                if (!string.IsNullOrEmpty(u.Name) && string.Equals(u.Name, qRaw, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(u.LowerName) && string.Equals(u.LowerName, norm, StringComparison.Ordinal)) return true;
                if (!string.IsNullOrEmpty(u.Email) && u.Email.Equals(qLower, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            bool IsPrefix(User u)
            {
                if (!string.IsNullOrEmpty(u.Name) && u.Name.StartsWith(qRaw, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(u.LowerName) && u.LowerName.StartsWith(norm)) return true;
                if (!string.IsNullOrEmpty(u.Email) && u.Email.StartsWith(qLower, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            var exact = all.Where(IsExact).OrderBy(u => u.Name).Select(ToSummary).ToArray();
            if (exact.Length > 0) return exact;

            var prefix = all.Where(IsPrefix).OrderBy(u => u.Name).Select(ToSummary).ToArray();
            return prefix;
        }
    }

    // ===== Repository 본체 =====

    private ILiteCollection<User> Col(ILiteDatabase db) => db.GetCollection<User>("users");

    public bool ExistsEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var e = email.Trim();
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
        if (string.IsNullOrWhiteSpace(email)) return null;
        var e = email.Trim();
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
            u.LowerName = UserSearchUtility.NormalizeName(u.Name ?? "");
            u.NameChosung = null; // 레거시 무효화
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
            u.LowerName = UserSearchUtility.NormalizeName(u.Name ?? "");
            u.NameChosung = null;
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
            return SearchPolicy.Search(Col(db), query);
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
