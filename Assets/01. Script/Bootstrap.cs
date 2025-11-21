using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private static bool s_Initialized = false;
    void Awake()
    {
        // 중복 방지
        if (s_Initialized) { Destroy(gameObject); return; }
        s_Initialized = true;

        DontDestroyOnLoad(gameObject);

        // 세션 상태 기준으로 첫 씬 결정
        bool authed = SessionManager.Instance && SessionManager.Instance.IsSignedIn;
        SceneNavigator.Instance.GoTo(authed ? ScreenId.HOME : ScreenId.REGISTER);
    }
}
