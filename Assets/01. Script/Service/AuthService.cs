using BCrypt.Net;

public class AuthService
{
    private readonly UserRepository _repo;

    public AuthService(UserRepository repo = null)
    {
        _repo = repo ?? new UserRepository();
    }



    public bool SignUp(string name, string email, string password)
    {
        name = (name ?? "").Trim();
        email = AuthValidator.NormalizeEmail(email);

        // 서버측 정책 강제
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (!AuthValidator.IsValidEmail(email)) return false;
        if (!AuthValidator.IsStrongPassword(password)) return false;
        if (_repo.ExistsEmail(email)) return false;

        bool hasSuperAdmin = _repo.HasSuperAdmin();

        var u = new User
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = hasSuperAdmin ? UserRole.USER : UserRole.SUPERADMIN,
            IsActive = true
        };

        _repo.Insert(u);
        return true;
    }
    public bool Exists(string email)
    {
        return _repo.ExistsEmail(AuthValidator.NormalizeEmail(email));
    }

    public User Login(string email, string password)
    {
        email = AuthValidator.NormalizeEmail(email);
        var u = _repo.FindActiveByEmail(email);
        if (u == null) return null;
        return BCrypt.Net.BCrypt.Verify(password, u.PasswordHash) ? u : null;
    }
}
