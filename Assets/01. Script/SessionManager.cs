using System;
using UnityEngine;

/// <summary>
/// 앱 전체의 로그인 상태/세션 정보를 관리하는 세션 매니저.
/// - 런타임 로그인 상태(IsSignedIn), 현재 유저정보(CurrentUser)
/// - 로그인/로그아웃 API
/// - (옵션) 세션 저장/복원 (PlayerPrefs 이용: 임시/개발용)
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Serializable]
    public class UserSnapshot   // 직렬화용 최소 데이터 (인게임용 참조)
    {
        public string Name;
        public string Email;
        public int Role;      // enum 대신에 int
        public bool IsActive;
    }

    public bool IsSignedIn => _currentUser != null;
    public User CurrentUser => _currentUser;
    public string SessionId { get; private set; }   // 필요 시 사용(세션 관리/트래킹)

    public event Action OnChanged;

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
