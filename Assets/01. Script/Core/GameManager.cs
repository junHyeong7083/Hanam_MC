using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전역 관리자
/// - QuitApplication: 앱 종료
/// - GoToHome: 홈화면(온보딩)으로 이동
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
            }
            return instance;
        }
    }

    [Header("씬 설정")]
    [Tooltip("온보딩(홈) 씬 이름")]
    [SerializeField] private string onboardingSceneName = "HomeScene";

    [Tooltip("로그인 씬 이름")]
    [SerializeField] private string loginSceneName = "RegisterScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 앱 종료
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("[GameManager] 앱 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 홈화면(온보딩)으로 이동
    /// - 세션 클리어 후 온보딩 씬으로 전환
    /// </summary>
    public void GoToHome()
    {
        Debug.Log($"[GameManager] 홈화면(온보딩)으로 이동: {onboardingSceneName}");



        // SceneNavigator가 있으면 사용, 없으면 직접 로드
        if (SceneNavigator.Instance != null)
        {
            SceneNavigator.Instance.GoTo(ScreenId.HOME);
        }
        else
        {
            SceneManager.LoadScene(onboardingSceneName);
        }
    }

    /// <summary>
    /// 특정 씬으로 이동 (SceneNavigator 활용)
    /// </summary>
    public void GoToScene(ScreenId screenId)
    {
        if (SceneNavigator.Instance != null)
        {
            SceneNavigator.Instance.GoTo(screenId);
        }
        else
        {
            Debug.LogWarning("[GameManager] SceneNavigator가 없습니다.");
        }
    }

    /// <summary>
    /// 로그아웃 후 로그인 화면으로 이동
    /// - 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void Logout()
    {
        Debug.Log($"[GameManager] 로그아웃 → 로그인 화면으로 이동: {loginSceneName}");

        // 세션 클리어
        if (SessionManager.Instance != null)
            SessionManager.Instance.SignOut();

        // SceneNavigator가 있으면 사용, 없으면 직접 로드
        if (SceneNavigator.Instance != null)
        {
            SceneNavigator.Instance.GoTo(ScreenId.REGISTER);
        }
        else
        {
            SceneManager.LoadScene(loginSceneName);
        }
    }
}
