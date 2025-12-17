using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 관리자 패널
/// - F11 키로 토글
/// - 종료 / 로그인 화면으로 이동 버튼
/// </summary>
public class AdminPanel : MonoBehaviour
{
    [Header("===== 패널 =====")]
    [SerializeField] private GameObject panelRoot;

    [Header("===== 버튼 =====")]
    [SerializeField] private Button exitButton;
    [SerializeField] private Button goToLoginButton;

    [Header("===== 설정 =====")]
    [Tooltip("로그인 씬 이름")]
    [SerializeField] private string loginSceneName = "LoginScene";

    private void Start()
    {
        // 초기 상태: 패널 숨김
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // 버튼 리스너 등록
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(OnGoToLoginClicked);
    }

    private void Update()
    {
        // F11 키로 패널 토글
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("??");
            TogglePanel();
        }

        // ESC로 패널 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && panelRoot != null && panelRoot.activeSelf)
        {
            panelRoot.SetActive(false);
        }
    }

    private void TogglePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(!panelRoot.activeSelf);
    }

    #region Button Handlers

    private void OnExitClicked()
    {
        Debug.Log("[AdminPanel] 앱 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnGoToLoginClicked()
    {
        Debug.Log($"[AdminPanel] 로그인 화면으로 이동: {loginSceneName}");

        // 세션 클리어 (필요 시)
        if (SessionManager.Instance != null)
            SessionManager.Instance.SignOut();

        SceneManager.LoadScene(loginSceneName);
    }

    #endregion

    #region Public API

    /// <summary>패널 열기</summary>
    public void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>패널 닫기</summary>
    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    #endregion
}
