using System;
using UnityEngine;

/// <summary>
/// SessionManager - 앱 전체의 로그인 상태/세션 정보를 관리하는 싱글톤 매니저
///
/// 【역할】 현재 로그인된 사용자 정보를 런타임에서 유지하고,
///          PlayerPrefs를 통해 세션을 디스크에 저장/복원하여 앱 재시작 시 자동 로그인을 지원한다.
/// 【참조하는 곳】 AuthService(Login 후 SignIn 호출), GameManager(Logout 시 SignOut),
///                Bootstrap(초기 세션 상태 확인), SceneNavigator(인증 검증),
///                LocalProgressService/LocalRewardService(현재 사용자 조회),
///                LoginController (로그인 상태 확인), StepFlowController (사용자 조회),
///                CommonRewardStep, InventoryDropTargetStepBase (보상/인벤토리 사용자 조회),
///                ThemePanelsController, AdminPanel, AdminUserBrowserUI/Controller (관리자 화면)
/// 【참조되는 곳】 User 모델 (사용자 정보)
/// 【흐름】 AuthService.Login() 성공 → SessionManager.SignIn(user) → IsSignedIn=true
///         GameManager.Logout() → SessionManager.SignOut() → IsSignedIn=false → PlayerPrefs 클리어
/// </summary>
public class SessionManager : MonoBehaviour
{
    /// <summary>전역 싱글톤 인스턴스. DontDestroyOnLoad로 씬 전환에도 유지됨</summary>
    public static SessionManager Instance { get; private set; }

    /// <summary>
    /// PlayerPrefs에 JSON으로 저장하기 위한 최소 사용자 정보 스냅샷.
    /// User 클래스의 PasswordHash 등 민감 정보는 제외된다.
    /// </summary>
    [Serializable]
    public class UserSnapshot
    {
        public string Name;
        public string Email;
        public int Role;      // UserRole enum을 int로 저장 (직렬화 호환성)
        public bool IsActive;
    }

    /// <summary>현재 로그인 상태. _currentUser가 null이 아니면 true</summary>
    public bool IsSignedIn => _currentUser != null;
    /// <summary>현재 로그인된 사용자 정보. 미로그인 시 null</summary>
    public User CurrentUser => _currentUser;
    /// <summary>현재 세션의 고유 ID. 세션 추적/분석에 사용 가능 (GUID 기반)</summary>
    public string SessionId { get; private set; }

    /// <summary>로그인/로그아웃 시 발생하는 이벤트. UI 갱신 등에 구독하여 사용</summary>
    public event Action OnChanged;

    /// <summary>현재 로그인된 사용자 객체. SignIn()에서 설정, SignOut()에서 null로 초기화</summary>
    User _currentUser;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>로그인 성공 시 세션 정보 저장</summary>
    public void SignIn(User user, string sessionId = null)
    {
        _currentUser = user;
        SessionId = sessionId ?? System.Guid.NewGuid().ToString("N");
        Save();             // 자동 저장 위치 등으로 최소 처리
        OnChanged?.Invoke();
        Debug.Log($"[Session] Signed in: {_currentUser?.Email}");
    }

    /// <summary>명시적 로그아웃</summary>
    public void SignOut()
    {
        _currentUser = null;
        SessionId = null;
        Clear();
        OnChanged?.Invoke();
        Debug.Log("[Session] Signed out");
    }

    // ────────────────── 세션 저장/복원 관련 ──────────────────
    const string KeyUser = "session.user";    // PlayerPrefs 키 (임시/개발용)
    const string KeySess = "session.id";

    /// <summary>디스크에서 세션정보 복원(성공시 true)</summary>
    public bool TryRestore()
    {
        if (!PlayerPrefs.HasKey(KeyUser)) return false;
        try
        {
            var json = PlayerPrefs.GetString(KeyUser);
            var snap = JsonUtility.FromJson<UserSnapshot>(json);
            if (snap == null) return false;

            _currentUser = new User
            {
                Name = snap.Name,
                Email = snap.Email,
                Role = (UserRole)snap.Role,
                IsActive = snap.IsActive
            };
            SessionId = PlayerPrefs.GetString(KeySess, null);

            OnChanged?.Invoke();
            Debug.Log($"[Session] Restored: {_currentUser.Email}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Session] Restore failed: {e}");
            Clear();
            return false;
        }
    }

    /// <summary>현재 세션을 디스크에 저장</summary>
    public void Save()
    {
        if (_currentUser == null) { Clear(); return; }
        var snap = new UserSnapshot
        {
            Name = _currentUser.Name,
            Email = _currentUser.Email,
            Role = (int)_currentUser.Role,
            IsActive = _currentUser.IsActive
        };
        PlayerPrefs.SetString(KeyUser, JsonUtility.ToJson(snap));
        PlayerPrefs.SetString(KeySess, SessionId ?? "");
        PlayerPrefs.Save();
    }

    /// <summary>디스크 저장값 삭제</summary>
    public void Clear()
    {
        PlayerPrefs.DeleteKey(KeyUser);
        PlayerPrefs.DeleteKey(KeySess);
        PlayerPrefs.Save();
    }
}
