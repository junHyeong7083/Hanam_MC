using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager - 게임 전역 관리자 (싱글톤)
///
/// 【역할】 앱 종료, 홈 화면 이동, 로그아웃 등 앱 전체에서 공통으로 사용하는 고수준 기능을 제공.
///          UI 버튼의 OnClick 이벤트에 직접 연결하여 사용 가능하다.
/// 【참조하는 곳】 CommonRewardStep (문제 완료 후 홈 이동), ProblemSceneController (문제 종료),
///                HomeSceneManager (홈 화면 버튼), EndingPosterDisplay (엔딩 후 홈 이동),
///                SceneNavigator (씬 전환 연동), UI 버튼 OnClick 이벤트
/// 【참조되는 곳】 SceneNavigator (씬 전환), SessionManager (로그아웃 처리)
/// 【흐름】 GoToHome() → SceneNavigator.GoTo(HOME)
///         Logout() → SessionManager.SignOut() → SceneNavigator.GoTo(REGISTER)
///         QuitApplication() → Application.Quit()
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>싱글톤 인스턴스. Lazy 초기화로 씬에서 자동 검색됨</summary>
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

    /// <summary>온보딩(홈) 씬 이름. SceneNavigator 없이 직접 로드할 때 사용</summary>
    [Header("씬 설정")]
    [Tooltip("온보딩(홈) 씬 이름")]
    [SerializeField] private string onboardingSceneName = "HomeScene";

    /// <summary>로그인/회원가입 씬 이름. SceneNavigator 없이 직접 로드할 때 사용</summary>
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
