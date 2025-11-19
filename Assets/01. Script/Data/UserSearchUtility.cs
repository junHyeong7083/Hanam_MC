using System.Linq;

// 미사용
public static class UserSearchUtility
{
    // 이름 정규화(공백 제거 + 소문자)
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        return new string(name.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    }
}