using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private static bool s_Initialized = false;

    [SerializeField] SceneNavigator navigator;   // 같은 씬에 배치

    void Awake()
    {
        // 중복 방지
        if (s_Initialized) { Destroy(gameObject); return; }
        s_Initialized = true;

        DontDestroyOnLoad(gameObject);

        if (!navigator)
        {
            Debug.LogError("[Bootstrap] SceneNavigator 참조가 없습니다.");
            return;
        }

        // 세션 상태 기준으로 첫 씬 결정
        bool authed = SessionManager.Instance && SessionManager.Instance.IsSignedIn;
        navigator.GoTo(authed ? ScreenId.HOME : ScreenId.REGISTER);
    }
}
