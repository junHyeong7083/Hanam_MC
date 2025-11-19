using System;
using System.Linq;

// User / UserSummary 관련 DB 접근
public partial class DBGateway
{
    public bool ExistsEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        return WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email, true);
            return col.Exists(u => u.Email == email);
        });
    }

    public bool HasSuperAdmin()
    {
        return WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Role);
            return col.Exists(u => u.Role == UserRole.SUPERADMIN);
        });
    }

    public User FindActiveUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        return WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Email, true);
            return col.FindOne(u => u.Email == email && u.IsActive);
        });
    }

    public User FindUserById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);
            return col.FindById(id);
        });
    }

    public void InsertUser(User u)
    {
        if (u == null) throw new ArgumentNullException(nameof(u));

        WithDb(db =>
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

        WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            col.EnsureIndex(x => x.Id, true);
            col.Update(u);
        });
    }

    public UserSummary[] SearchUsersFriendly(string query)
    {
        query = (query ?? string.Empty).Trim();

        return WithDb(db =>
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
        return WithDb(db =>
        {
            var col = db.GetCollection<User>(CUsers);
            var q = col.FindAll();

            if (limit > 0)
                q = q.Take(limit);

            return q.Select(ToSummary).ToArray();
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
