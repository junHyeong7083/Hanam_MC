using System;
using System.Linq;
using System.Collections.Generic;
using LiteDB;

// 모델(User, UserSummary, UserRole)은 Model.cs에 있다고 가정
// DBHelper.With(...) 유틸은 DBHelper.cs에 있다고 가정
// UserSearchUtility는 UserSearchUtility.cs에 있다고 가정

public interface IUserRepository
{
    bool ExistsEmail(string email);
    bool HasSuperAdmin();
    User FindActiveByEmail(string email);
    User FindById(string id);
    void Insert(User u);
    void Update(User u);

    // 검색
    UserSummary[] SearchUsersFriendly(string query);

    // 전체 조회 (limit == 0 이면 제한 없음)
    UserSummary[] ListAllUsers(int limit = 0);
}

public class UserRepository : IUserRepository
{
    // 컬렉션 핸들
    private ILiteCollection<User> Col(ILiteDatabase db) => db.GetCollection<User>("users");

    // 인덱스 보장
    private void EnsureIndexes(ILiteCollection<User> users)
    {
        users.EnsureIndex(u => u.Email, true);
        users.EnsureIndex(u => u.IsActive);
        users.EnsureIndex(u => u.LowerName);
        users.EnsureIndex(u => u.NameChosung);
    }

    // 검색 보조필드 세팅
    private void SetSearchFields(User u)
    {
        u.LowerName = UserSearchUtility.NormalizeName(u.Name ?? "");
        u.NameChosung = UserSearchUtility.ToChosung(u.Name ?? "");
    }

    // ===== CRUD =====

    public bool ExistsEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var e = email.Trim();

        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.Exists(x => x.Email == e);
        });
    }

    public bool HasSuperAdmin()
    {
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.Exists(x => x.Role == UserRole.SUPERADMIN);
        });
    }

    public User FindActiveByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var e = email.Trim();

        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.FindOne(u => u.Email == e && u.IsActive);
        });
    }

    public User FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var i = id.Trim();

        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.FindById(i);
        });
    }

    public void Insert(User u)
    {
        DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            SetSearchFields(u);
            users.Insert(u);
            return true;
        });
    }

    public void Update(User u)
    {
        DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            SetSearchFields(u);
            users.Update(u);
            return true;
        });
    }

    // ===== 조회 유틸 =====

    public UserSummary[] ListAllUsers(int limit = 0)
    {
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);

            IEnumerable<User> query = users.FindAll().OrderBy(u => u.Name);
            if (limit > 0) query = query.Take(limit);

            return query.Select(ToSummary).ToArray();
        });
    }

    /// <summary>
    /// 친화적 검색:
    /// 1) 정확 일치(이름/초성/이메일)가 있으면 그것만 반환
    /// 2) 없으면 접두(StartsWith) 매칭 반환
    /// * 초성 검색은 “입력이 초성만일 때”만 동작 (완성형 입력은 초성 비교 안 함)
    /// * 이름 비교는 공백 제거 + 소문자 정규화(NormalizeName) 기준
    /// </summary>
    public UserSummary[] SearchUsersFriendly(string query)
    {
        var qRaw = (query ?? string.Empty).Trim();
        if (qRaw.Length == 0) return ListAllUsers(); // 전체

        var qLower = qRaw.ToLowerInvariant();
        var norm = UserSearchUtility.NormalizeName(qRaw); // 공백 제거 + 소문자
        var isCho = UserSearchUtility.IsChoseongOnly(qRaw);
        var choInput = isCho ? UserSearchUtility.NormalizeChoseongInput(qRaw) : ""; // 초성만 입력일 때만 세팅

        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);

            // 메모리에서 우선 처리 (필드 백필/정렬 우선순위 적용)
            var all = users.FindAll().ToList();

            // 누락된 검색 보조필드 백필
            bool dirty = false;
            foreach (var u in all)
            {
                if (string.IsNullOrEmpty(u.LowerName) || string.IsNullOrEmpty(u.NameChosung))
                {
                    SetSearchFields(u);
                    users.Update(u);
                    dirty = true;
                }
            }
            if (dirty) all = users.FindAll().ToList();

            bool IsExact(User u)
            {
                if (!string.IsNullOrEmpty(norm) && (u.LowerName ?? "") == norm) return true; // 이름 정확 일치
                if (isCho && !string.IsNullOrEmpty(choInput) && (u.NameChosung ?? "") == choInput) return true; // 초성 정확 일치 (초성만 입력했을 때만)
                if (!string.IsNullOrEmpty(qLower) && !string.IsNullOrEmpty(u.Email) &&
                    u.Email.Equals(qLower, StringComparison.OrdinalIgnoreCase)) return true; // 이메일 정확 일치
                return false;
            }

            bool IsPrefix(User u)
            {
                if (!string.IsNullOrEmpty(norm) && (u.LowerName ?? "").StartsWith(norm)) return true; // 이름 접두
                if (isCho && !string.IsNullOrEmpty(choInput) && (u.NameChosung ?? "").StartsWith(choInput)) return true; // 초성 접두 (초성만 입력했을 때만)
                if (!string.IsNullOrEmpty(qLower) && !string.IsNullOrEmpty(u.Email) &&
                    u.Email.StartsWith(qLower, StringComparison.OrdinalIgnoreCase)) return true; // 이메일 접두
                return false;
            }

            // 1) 정확 일치 우선
            var exact = all.Where(IsExact)
                           .OrderBy(u => u.Name)
                           .Select(ToSummary)
                           .ToArray();
            if (exact.Length > 0) return exact;

            // 2) 접두 매칭
            var prefix = all.Where(IsPrefix)
                            .OrderBy(u => u.Name)
                            .Select(ToSummary)
                            .ToArray();
            return prefix;
        });
    }

    // DTO 변환
    private static UserSummary ToSummary(User u) => new UserSummary
    {
        Email = u.Email,
        Name = u.Name,
        Role = u.Role,
        IsActive = u.IsActive
    };
}
