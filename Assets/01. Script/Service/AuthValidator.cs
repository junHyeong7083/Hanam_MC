using System.Text.RegularExpressions;

/// <summary>
/// AuthValidator - 인증 관련 입력값 유효성 검증 정적 유틸리티
///
/// 【역할】 이메일 형식 검증, 비밀번호 강도 검증, 이메일 정규화 기능을 제공한다.
///          AuthService의 SignUp/Login/Exists에서 사용된다.
/// 【참조하는 곳】 AuthService (회원가입/로그인 시 입력값 검증)
/// 【비밀번호 정책】 최소 6자 이상, 영문 1자 이상 + 숫자 1자 이상 포함
/// 【이메일 정규화】 앞뒤 공백 제거 + 소문자 변환
/// </summary>
public static class AuthValidator
{
    /// <summary>이메일 형식 정규식. RFC 5322 간소화 버전 (user@domain.tld)</summary>
    static readonly Regex EmailRx =
        new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$");

    /// <summary>
    /// 이메일 형식이 올바른지 검증한다.
    /// null이나 빈 문자열은 false를 반환한다.
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        email = (email ?? "").Trim();
        return EmailRx.IsMatch(email);
    }

    /// <summary>
    /// 비밀번호 강도를 검증한다.
    /// 조건: 6자 이상 + 영문(Letter) 1자 이상 + 숫자(Digit) 1자 이상.
    /// 조건을 만족하면 true, 아니면 false.
    /// </summary>
    public static bool IsStrongPassword(string pw)
    {
        if (string.IsNullOrEmpty(pw) || pw.Length < 6) return false;
        bool hasLetter = false, hasDigit = false;
        foreach (var c in pw)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
            if (hasLetter && hasDigit) return true;
        }
        return false;
    }

    /// <summary>
    /// 이메일을 정규화한다: 앞뒤 공백 제거 + 소문자 변환.
    /// DB에 저장하기 전과 조회할 때 모두 이 함수를 거쳐야 일관성이 유지된다.
    /// </summary>
    public static string NormalizeEmail(string email) =>
        (email ?? "").Trim().ToLower();
}
