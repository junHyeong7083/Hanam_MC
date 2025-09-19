using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using LiteDB;
using BCrypt.Net;

/// <summary>
/// 사용자 회원가입, 로그인, SUPERADMIN 최초 생성
/// 모든 인증 로직 담당
/// DB연결은 DBHelper로 처리
/// </summary>
public class AuthService : MonoBehaviour
{
    static ILiteCollection<User> Col(LiteDatabase db) => db.GetCollection<User>("users");
    // 최초 1회: SUPERADMIN 생성(설치 마법사에서 호출)

    /// <summary>
    /// 사용자 회원가입 메서드
    /// 기본권한은 USER이지만 첫 생성시 SUPERADMIN
    /// 같은 Email(로그인시 사용되는 아이디)가 존재시 false반환
    /// </summary>
    public static bool SignUp(string name, string email, string password)
    {
        email = email.Trim().ToLower();
        return DBHelper.With(db =>
        {
            var users = Col(db);
            // 이메일 중복 체크
            users.EnsureIndex(x => x.Email, true);
            if (users.Exists(u => u.Email == email)) return false;

            bool hasSuperAdmin = users.Exists(u => u.Role == UserRole.SUPERADMIN);

            var u = new User
            {
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = hasSuperAdmin ? UserRole.USER : UserRole.SUPERADMIN
            };
            users.Insert(u);
            return true;
        });
    }

    /// <summary>
    /// 로그인 기능 메서드
    /// 
    /// email, password 입력받고
    /// 비번 검증(BCrypt) 후 USER 객체 반환
    /// 실패시 null
    /// </summary>
    public static User Login(string email, string password)
    {
        email = email.Trim().ToLower();
        return DBHelper.With(db =>
        {
            var users = Col(db);
            var u = users.FindOne(x => x.Email == email && x.IsActive);
            if (u == null) return null;
            return BCrypt.Net.BCrypt.Verify(password, u.PasswordHash) ? u : null;
        });
    }
}
