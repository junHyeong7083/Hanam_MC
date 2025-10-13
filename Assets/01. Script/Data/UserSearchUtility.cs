using System.Linq;

public static class UserSearchUtility
{
    // 초성만으로 이뤄졌는지
    public static bool IsChoseongOnly(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        foreach (var ch in s.Trim())
        {
            bool compat = (ch >= 0x3131 && ch <= 0x314E); // ㄱ~ㅎ
            bool choseong = (ch >= 0x1100 && ch <= 0x1112); // ᄀ~ᄒ
            if (!(compat || choseong || char.IsWhiteSpace(ch))) return false;
        }
        return true;
    }

    // ᄀ→ㄱ 로 통일 + 공백 제거
    public static string NormalizeChoseongInput(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        const string CHO_COMP = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        int[] CHO_CODE = { 0x1100, 0x1101, 0x1102, 0x1103, 0x1104, 0x1105, 0x1106, 0x1107, 0x1108, 0x1109, 0x110A, 0x110B, 0x110C, 0x110D, 0x110E, 0x110F, 0x1110, 0x1111, 0x1112 };

        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch)) continue; // ✅ 공백 제거
            int idx = System.Array.IndexOf(CHO_CODE, ch);
            sb.Append(idx >= 0 ? CHO_COMP[idx] : ch);
        }
        return sb.ToString();
    }

    // 완성형 한글 → 초성만(비한글/공백 제거)
    public static string ToChosung(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder();
        const string CHO = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

        foreach (var ch in s)
        {
            if (ch >= 0xAC00 && ch <= 0xD7A3)
            {
                int code = ch - 0xAC00;
                int choIndex = code / (21 * 28);
                sb.Append(CHO[choIndex]);
            }
        }
        return sb.ToString();
    }

    // 이름 정규화(공백 제거 + 소문자)
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        return new string(name.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    }
}
