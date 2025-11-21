using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Bootstrap : MonoBehaviour
{
    private static bool s_Initialized = false;

    void Awake()
    {
        // 중복 방지
        if (s_Initialized)
        {
            Destroy(gameObject);
            return;
        }
        s_Initialized = true;

        DontDestroyOnLoad(gameObject);

        // 바로 GoTo 호출하지 말고, 코루틴에서 SceneNavigator 준비될 때까지 기다리기
        StartCoroutine(InitRoutine());
    }

    IEnumerator InitRoutine()
    {
        // SceneNavigator.Instance가 살아날 때까지 한 프레임씩 대기
        while (SceneNavigator.Instance == null)
            yield return null;

        // 세션 상태 기준으로 첫 씬 결정
        var session = SessionManager.Instance;
        bool authed = (session != null && session.IsSignedIn);

        SceneNavigator.Instance.GoTo(authed ? ScreenId.HOME : ScreenId.REGISTER);
    }
}
