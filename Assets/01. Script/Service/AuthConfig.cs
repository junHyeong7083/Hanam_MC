using UnityEngine;

/// <summary>
/// AuthConfig - 인증 관련 설정값을 담는 ScriptableObject
///
/// 【역할】 기본 관리자(SUPERADMIN) 계정의 이메일, 비밀번호, 이름을 설정한다.
///          AuthService 초기화 시 SUPERADMIN이 없으면 이 설정값으로 자동 생성한다.
///          Resources/AuthConfig에 배치하거나, 없으면 기본값으로 생성된다.
/// 【참조하는 곳】 AuthService.EnsureSuperAdmin() (SUPERADMIN 자동 생성 시)
/// 【생성 방법】 Unity 메뉴: Create > Config > AuthConfig
/// 【배포 주의】 실제 배포 시 기본 비밀번호를 반드시 변경할 것
/// </summary>
[CreateAssetMenu(fileName = "AuthConfig", menuName = "Config/AuthConfig")]
public class AuthConfig : ScriptableObject
{
    /// <summary>싱글톤 인스턴스 캐시. Resources.Load로 한 번만 로드</summary>
    private static AuthConfig _instance;

    /// <summary>
    /// 싱글톤 접근자. Resources/AuthConfig를 로드하고, 없으면 기본값으로 인스턴스를 생성한다.
    /// </summary>
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

    /// <summary>기본 SUPERADMIN 이메일 (최초 실행 시 자동 생성)</summary>
    [Header("기본 관리자 계정 (최초 실행 시 생성)")]
    [SerializeField] private string defaultAdminEmail = "admin@local";
    /// <summary>기본 SUPERADMIN 비밀번호 (BCrypt로 해시되어 저장됨. 배포 전 변경 필수)</summary>
    [SerializeField] private string defaultAdminPassword = "admin1234";
    /// <summary>기본 SUPERADMIN 표시 이름</summary>
    [SerializeField] private string defaultAdminName = "Super Admin";

    /// <summary>기본 SUPERADMIN 이메일 읽기 전용 접근자</summary>
    public string DefaultAdminEmail => defaultAdminEmail;
    /// <summary>기본 SUPERADMIN 비밀번호 읽기 전용 접근자</summary>
    public string DefaultAdminPassword => defaultAdminPassword;
    /// <summary>기본 SUPERADMIN 이름 읽기 전용 접근자</summary>
    public string DefaultAdminName => defaultAdminName;
}
