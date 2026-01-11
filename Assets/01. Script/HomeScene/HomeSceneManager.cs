using UnityEngine;

/// <summary>
/// HomeScene 버튼 이벤트 연결용 스크립트
/// - 버튼 OnClick에 연결해서 사용
/// </summary>
public class HomeSceneManager : MonoBehaviour
{
    /// <summary>
    /// 로그아웃 후 로그인 화면으로 이동
    /// </summary>
    public void Logout()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Logout();
        }
        else
        {
            Debug.LogWarning("[HomeSceneManager] GameManager가 없습니다.");
        }
    }

    /// <summary>
    /// 앱 종료
    /// </summary>
    public void QuitApplication()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitApplication();
        }
        else
        {
            Debug.LogWarning("[HomeSceneManager] GameManager가 없습니다.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
