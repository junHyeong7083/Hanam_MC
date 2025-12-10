using UnityEngine;

/// <summary>
/// 인증 관련 설정값
/// 실제 배포 시 Inspector에서 값을 변경하거나,
/// 환경 변수 / 설정 파일로 관리하세요.
/// </summary>
[CreateAssetMenu(fileName = "AuthConfig", menuName = "Config/AuthConfig")]
public class AuthConfig : ScriptableObject
{
    private static AuthConfig _instance;

    public static AuthConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<AuthConfig>("AuthConfig");
                if (_instance == null)
                {
                    // 기본값 사용 (개발용)
                    _instance = CreateInstance<AuthConfig>();
                }
            }
            return _instance;
        }
    }

    [Header("기본 관리자 계정 (최초 실행 시 생성)")]
    [SerializeField] private string defaultAdminEmail = "admin@local";
    [SerializeField] private string defaultAdminPassword = "admin1234";
    [SerializeField] private string defaultAdminName = "Super Admin";

    public string DefaultAdminEmail => defaultAdminEmail;
    public string DefaultAdminPassword => defaultAdminPassword;
    public string DefaultAdminName => defaultAdminName;
}
