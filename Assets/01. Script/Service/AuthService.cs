using System;
using UnityEngine;

/// <summary>
/// IAuthService - 인증(회원가입/로그인/이메일 중복 확인) 서비스 인터페이스
///
/// 【역할】 사용자 인증 관련 비즈니스 로직 인터페이스를 정의한다.
/// 【참조하는 곳】 LoginController, RegisterController, DataService.Instance.Auth
/// </summary>
public interface IAuthService
{
    /// <summary>이메일 존재 여부를 확인한다. 이메일 형식 검증도 수행</summary>
    Result<bool> Exists(string email);
    /// <summary>회원가입 처리. 이름/이메일 검증 → BCrypt 해시 → DB 저장</summary>
    Result SignUp(string name, string email, string password);
    /// <summary>로그인 처리. 이메일로 사용자 조회 → BCrypt 비밀번호 검증 → User 반환</summary>
    Result<User> Login(string email, string password);
}

/// <summary>
/// AuthService - IAuthService의 구현체 (BCrypt 기반 로컬 인증)
///
/// 【역할】 회원가입, 로그인, 이메일 중복 확인을 담당한다.
///          비밀번호는 BCrypt(WorkFactor=10)로 해시하여 저장하며,
///          로그인 시 BCrypt.Verify()로 검증한다.
///          생성자에서 SUPERADMIN이 없으면 AuthConfig 설정값으로 자동 생성한다.
/// 【참조하는 곳】 DataService.Awake()에서 생성, LoginController/RegisterController에서 사용
/// 【참조되는 곳】 IUserRepository (사용자 CRUD), AuthValidator (이메일/비밀번호 검증),
///                AuthConfig (기본 SUPERADMIN 설정), BCrypt.Net (비밀번호 해싱)
/// 【흐름】
///   회원가입: RegisterController → AuthService.SignUp() → AuthValidator 검증 → BCrypt 해시 → UserRepository.InsertUser()
///   로그인: LoginController → AuthService.Login() → UserRepository.FindActiveUserByEmail() → BCrypt.Verify()
///           → SessionManager.SignIn(user) → SceneNavigator.GoTo(HOME)
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>사용자 데이터 접근용 Repository</summary>
    private readonly IUserRepository _users;
    /// <summary>BCrypt 해싱 강도. 값이 클수록 느리지만 안전 (기본 10)</summary>
    private const int BcryptWorkFactor = 10;

    /// <summary>
    /// 생성자. UserRepository를 주입받고, SUPERADMIN 계정이 없으면 자동 생성한다.
    /// </summary>
    public AuthService(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        // 최초 실행 시 SUPERADMIN 자동 생성
        EnsureSuperAdmin();
    }

    /// <summary>
    /// SUPERADMIN 계정이 없으면 기본 SuperAdmin을 하나 만들어준다.
    /// </summary>
    private void EnsureSuperAdmin()
    {
        try
        {
            if (_users.HasSuperAdmin()) return;

            var config = AuthConfig.Instance;
            var user = new User
            {
                Name = config.DefaultAdminName,
                Email = config.DefaultAdminEmail,
                Role = UserRole.SUPERADMIN,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(config.DefaultAdminPassword, BcryptWorkFactor),
            };

            _users.InsertUser(user);
            Debug.Log($"[AuthService] Default SUPERADMIN created: {config.DefaultAdminEmail}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] EnsureSuperAdmin error: {ex}");
        }
    }

    /// <summary>
    /// 이메일 존재 여부를 확인한다.
    /// 이메일 형식을 먼저 검증하고, DB에서 중복 여부를 조회한다.
    /// 회원가입 폼에서 이메일 입력 시 실시간 중복 체크에 사용된다.
    /// </summary>
    public Result<bool> Exists(string email)
    {
        try
        {
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<bool>.Fail(AuthError.EmailInvalid);

            bool exists = _users.ExistsEmail(e);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] Exists error: {ex}");
            return Result<bool>.Fail(AuthError.Internal);
        }
    }

    /// <summary>
    /// 회원가입을 처리한다.
    /// 1) 이름/이메일/비밀번호 유효성 검증 (AuthValidator 사용)
    /// 2) 이메일 중복 확인
    /// 3) BCrypt로 비밀번호 해시 생성
    /// 4) User 엔티티를 DB에 저장
    /// </summary>
    /// <param name="name">사용자 이름 (공백 제거됨)</param>
    /// <param name="email">사용자 이메일 (소문자 정규화됨)</param>
    /// <param name="password">평문 비밀번호 (6자 이상, 영문+숫자 필수)</param>
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
                return Result.Fail(AuthError.PasswordWeak, "비밀번호는 6자 이상, 영문+숫자를 포함해야 합니다.");

            if (_users.ExistsEmail(email))
                return Result.Fail(AuthError.EmailDuplicate, "이미 가입된 이메일입니다.");

            var user = new User
            {
                Name = name,
                Email = email,
                Role = UserRole.USER,
                IsActive = true,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor),
            };

            _users.InsertUser(user);
            return Result.Success();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] SignUp error: {ex}");
            return Result.Fail(AuthError.Internal);
        }
    }


    /// <summary>
    /// 로그인을 처리한다.
    /// 1) 이메일 형식 검증
    /// 2) 활성(IsActive=true) 사용자 조회
    /// 3) BCrypt.Verify()로 비밀번호 검증
    /// 4) 성공 시 User 객체 반환 (호출자가 SessionManager.SignIn()을 수행)
    /// </summary>
    /// <param name="email">사용자 이메일</param>
    /// <param name="password">평문 비밀번호 (BCrypt로 검증)</param>
    public Result<User> Login(string email, string password)
    {
        try
        {
            var e = AuthValidator.NormalizeEmail(email);
            if (!AuthValidator.IsValidEmail(e))
                return Result<User>.Fail(AuthError.EmailInvalid);

            if (string.IsNullOrEmpty(password))
                return Result<User>.Fail(AuthError.PasswordWeak);

            var u = _users.FindActiveUserByEmail(e);
            if (u == null)
                return Result<User>.Fail(AuthError.NotFoundOrInactive);

            bool ok = BCrypt.Net.BCrypt.Verify(password, u.PasswordHash);
            if (!ok)
                return Result<User>.Fail(AuthError.PasswordMismatch);

            return Result<User>.Success(u);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthService] Login error: {ex}");
            return Result<User>.Fail(AuthError.Internal);
        }
    }
}
