using System.Text.RegularExpressions;

public static class AuthValidator
{
    static readonly Regex EmailRx =
        new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$");

    public static bool IsValidEmail(string email)
    {
        email = (email ?? "").Trim();
        return EmailRx.IsMatch(email);
    }

    // 8자 이상 + 문자/숫자 각각 1개 이상
    public static bool IsStrongPassword(string pw)
    {
        if (string.IsNullOrEmpty(pw) || pw.Length < 8) return false;
        bool hasLetter = false, hasDigit = false;
        foreach (var c in pw)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
            if (hasLetter && hasDigit) return true;
        }
        return false;
    }

    public static string NormalizeEmail(string email) =>
        (email ?? "").Trim().ToLower();
}
