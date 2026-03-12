using System.Linq;

/// <summary>
/// UserSearchUtility - 사용자 이름 정규화 유틸리티 (현재 미사용)
///
/// 【역할】 사용자 이름에서 공백을 제거하고 소문자로 변환하는 정규화 함수 제공.
///          관리자 검색에서 이름 비교 시 사용하려고 만들었으나, 현재는 UserRepository에서
///          LowerName/NameChosung 필드를 직접 사용하므로 미사용 상태.
/// 【참조하는 곳】 없음 (미사용)
/// </summary>
public static class UserSearchUtility
{
    /// <summary>
    /// 이름을 정규화한다: 공백 제거 + 소문자 변환.
    /// 예: "홍 길동" → "홍길동" (소문자 변환은 한글에는 영향 없음)
    /// </summary>
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        return new string(name.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    }
}