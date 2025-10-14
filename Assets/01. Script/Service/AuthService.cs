using UnityEngine;

public interface IAuthService
{
    Result<bool> Exists(string email);
    Result SignUp(string name, string email, string password);
    Result<User> Login(string email, string password);
}
public class AuthService : IAuthService
{


    private readonly IUserRepository _repo;
    private const int BcryptWorkFactor = 10;

    public AuthService(IUserRepository repo = null)
    {
        _repo = repo ?? new UserRepository();
    }

    public Result<bool> Exists(string email)
    {
        try
        {
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<bool>.Fail(AuthError.EmailInvalid);

            return Result<bool>.Success(_repo.ExistsEmail(e));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AuthService] Exists error: {ex}");
            return Result<bool>.Fail(AuthError.Internal);
        }
    }

    public Result SignUp(string name, string email, string password)
    {
        var n = (name ?? "").Trim();
        var e = AuthValidator.NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(n)) return Result.Fail(AuthError.NameEmpty);
        if (!AuthValidator.IsValidEmail(e)) return Result.Fail(AuthError.EmailInvalid);
        if (!AuthValidator.IsStrongPassword(password)) return Result.Fail(AuthError.PasswordWeak);

        try
        {
            if (_repo.ExistsEmail(e)) return Result.Fail(AuthError.EmailDuplicate);

            bool hasSuperAdmin = _repo.HasSuperAdmin();

            var u = new User
            {
                Name = n,
                Email = e,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor),
                Role = hasSuperAdmin ? UserRole.USER : UserRole.SUPERADMIN,
                IsActive = true
            };

            _repo.Insert(u);
            return Result.Success();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AuthService] SignUp error: {ex}");
            return Result.Fail(AuthError.Internal);
        }
    }

    public Result<User> Login(string email, string password)
    {
        var e = AuthValidator.NormalizeEmail(email);
        try
        {
            var u = _repo.FindActiveByEmail(e);
            if (u == null) return Result<User>.Fail(AuthError.NotFoundOrInactive);

            return BCrypt.Net.BCrypt.Verify(password, u.PasswordHash)
                ? Result<User>.Success(u)
                : Result<User>.Fail(AuthError.PasswordMismatch);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AuthService] Login error: {ex}");
            return Result<User>.Fail(AuthError.Internal);
        }
    }
}
