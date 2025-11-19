using UnityEngine;

/// <summary>
/// 회원가입 / 로그인 / 이메일 중복 확인을 담당하는 인증 서비스.
/// 실제 User 저장/조회는 IUserRepository를 통해 수행된다.
/// </summary>
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
        EnsureSuperAdmin();
    }

    /// <summary>
    /// SUPERADMIN 계정이 없으면 기본 SuperAdmin을 하나 만들어준다.
    /// </summary>
    private void EnsureSuperAdmin()
    {
        try
        {
            if (_repo.HasSuperAdmin()) return;

            var user = new User
            {
                Name = "Super Admin",
                Email = "admin@local",
                Role = UserRole.SUPERADMIN,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin1234", BcryptWorkFactor),
            };

            _repo.Insert(user);
            Debug.Log("[AuthService] Default SUPERADMIN created: admin@local / admin1234");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AuthService] EnsureSuperAdmin error: {ex}");
        }
    }

    public Result<bool> Exists(string email)
    {
        try
        {
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<bool>.Fail(AuthError.EmailInvalid);

            bool exists = _repo.ExistsEmail(e);
            return Result<bool>.Success(exists);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AuthService] Exists error: {ex}");
            return Result<bool>.Fail(AuthError.Internal);
        }
    }

    public Result SignUp(string name, string email, string password)
    {
        try
        {
            name = (name ?? string.Empty).Trim();
            email = AuthValidator.NormalizeEmail(email);

            if (string.IsNullOrEmpty(name))
                return Result.Fail(AuthError.NameEmpty, "이름을 입력해주세요.");

            if (!AuthValidator.IsValidEmail(email))
                return Result.Fail(AuthError.EmailInvalid, "이메일 형식이 올바르지 않습니다.");

            if (!AuthValidator.IsStrongPassword(password))
                return Result.Fail(AuthError.PasswordWeak, "비밀번호는 8자 이상, 영문+숫자를 포함해야 합니다.");

            if (_repo.ExistsEmail(email))
                return Result.Fail(AuthError.EmailDuplicate, "이미 가입된 이메일입니다.");

            var user = new User
            {
                Name = name,
                Email = email,
                Role = UserRole.USER,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor),
            };

            _repo.Insert(user);
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
        try
        {
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<User>.Fail(AuthError.EmailInvalid);

            if (string.IsNullOrEmpty(password))
                return Result<User>.Fail(AuthError.PasswordWeak);

            var u = _repo.FindActiveByEmail(e);
            if (u == null)
                return Result<User>.Fail(AuthError.NotFoundOrInactive);

            bool ok = BCrypt.Net.BCrypt.Verify(password, u.PasswordHash);
            if (!ok)
                return Result<User>.Fail(AuthError.PasswordMismatch);

            return Result<User>.Success(u);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AuthService] Login error: {ex}");
            return Result<User>.Fail(AuthError.Internal);
        }
    }
}

