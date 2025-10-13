using System;
using System.Linq;
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
}



public class UserRepository : IUserRepository
{
    static ILiteCollection<User> Col(LiteDatabase db) => db.GetCollection<User>("users");

    static void EnsureIndexes(ILiteCollection<User> users)
    {
        users.EnsureIndex(x => x.Email, unique: true);
        users.EnsureIndex(x => x.LowerName);
        users.EnsureIndex(x => x.NameChosung);
    }

    public bool ExistsEmail(string email)
    {
        email = AuthValidator.NormalizeEmail(email);
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.Exists(u => u.Email == email);
        });
    }

    public bool HasSuperAdmin()
    {
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.Exists(u => u.Role == UserRole.SUPERADMIN);
        });
    }

    public User FindActiveByEmail(string email)
    {
        email = AuthValidator.NormalizeEmail(email);
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.FindOne(x => x.Email == email && x.IsActive);
        });
    }

    public User FindById(string id)
    {
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.FindById(id);
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

            // 이름 변경 대응
            SetSearchFields(u);

            users.Update(u);
            return true;
        });
    }

    // UserRepository.cs
    public UserSummary[] SearchUsersFriendly(string query)
    {
        var qRaw = (query ?? "").Trim();
        if (qRaw.Length == 0) return ListAllUsers(); // 공백이면 전체. 1글자부터 검색

        var qLower = qRaw.ToLowerInvariant();
        var norm = UserSearchUtility.NormalizeName(qRaw);

        // 초성-only / 완성형 분기
        string cho = UserSearchUtility.IsChoseongOnly(qRaw)
            ? UserSearchUtility.NormalizeChoseongInput(qRaw)   // ex) "ㅅㅅㅎ"
            : UserSearchUtility.ToChosung(qRaw);               // ex) "손승현" -> "ㅅㅅㅎ"

        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);

            // 전체를 읽어와서 (작은 컬렉션 전제) 메모리에서 안전하게 필터
            var all = users.FindAll().ToList();

            //  누락된 보조필드 백필(예전에 넣은 데이터 방지)
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
            if (dirty) all = users.FindAll().ToList(); // 갱신 반영

            //  필터 (빈 문자열 조건은 제외)
            bool Match(User u)
            {
                if (!string.IsNullOrEmpty(norm) && (u.LowerName ?? "").Contains(norm)) return true;
                if (!string.IsNullOrEmpty(cho) && (u.NameChosung ?? "").Contains(cho)) return true;
                if (!string.IsNullOrEmpty(qLower) &&
                    !string.IsNullOrEmpty(u.Email) &&
                    u.Email.IndexOf(qLower, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                return false;
            }

            return all.Where(Match)
                      .Select(u => new UserSummary { Email = u.Email, Name = u.Name, Role = u.Role, IsActive = u.IsActive })
                      .ToArray();
        });
    }


    static void SetSearchFields(User u)
    {
        u.LowerName = UserSearchUtility.NormalizeName(u.Name);
        u.NameChosung = UserSearchUtility.ToChosung(u.Name);
    }
    public UserSummary[] ListAllUsers(int limit = 1000, int skip = 0)
    {
        return DBHelper.With(db =>
        {
            var users = Col(db);
            EnsureIndexes(users);
            return users.FindAll()
                        .Skip(skip)
                        .Take(limit)
                        .Select(u => new UserSummary
                        {
                            Email = u.Email,
                            Name = u.Name,
                            Role = u.Role,
                            IsActive = u.IsActive
                        })
                        .ToArray();
        });
    }

}
