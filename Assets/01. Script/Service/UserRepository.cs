using LiteDB;
public interface IUserRepository
{
    bool ExistsEmail(string email);
    bool HasSuperAdmin();
    User FindActiveByEmail(string email);
    void Insert(User u);
}
public class UserRepository : IUserRepository
{
    static ILiteCollection<User> Col(LiteDatabase db) => db.GetCollection<User>("users");

    public bool ExistsEmail(string email)
    {
        email = AuthValidator.NormalizeEmail(email);
        return DBHelper.With(db =>
        {
            var users = Col(db);
            users.EnsureIndex(x => x.Email, true);
            return users.Exists(u => u.Email == email);
        });
    }

    public bool HasSuperAdmin()
    {
        return DBHelper.With(db =>
        {
            var users = Col(db);
            users.EnsureIndex(x => x.Email, true);
            return users.Exists(u => u.Role == UserRole.SUPERADMIN);
        });
    }

    public User FindActiveByEmail(string email)
    {
        email = AuthValidator.NormalizeEmail(email);
        return DBHelper.With(db =>
        {
            var users = Col(db);
            return users.FindOne(x => x.Email == email && x.IsActive);
        });
    }

    public void Insert(User u)
    {
        DBHelper.With(db =>
        {
            var users = Col(db);
            users.EnsureIndex(x => x.Email, true);
            users.Insert(u);
            return true;
        });
    }
}
