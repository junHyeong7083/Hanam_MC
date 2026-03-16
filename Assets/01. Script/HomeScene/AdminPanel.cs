using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AdminPanel - 관리자 전용 패널 (홈 화면에서 사용)
///
/// 【역할】 F1 키로 관리자 패널을 토글하여 앱 종료 또는 로그인 화면 복귀 기능을 제공한다.
///         ESC 키로 패널을 닫을 수 있다.
/// 【씬】 HomeScene (LevelSelectScene)
/// 【참조하는 곳】 HomeScene 내 Canvas에 부착되어 독립적으로 동작
/// 【참조되는 곳】 GameManager (로그아웃/앱 종료 처리)
/// 【흐름】 F1 키 → 패널 토글 → "종료" 클릭 시 앱 종료 / "로그인" 클릭 시 세션 클리어 후 로그인 씬 이동
/// </summary>
public class AdminPanel : MonoBehaviour
{
    [Header("===== 패널 =====")]
    [SerializeField] private GameObject panelRoot;           // 관리자 패널 루트 오브젝트 (활성/비활성 전환 대상)

    [Header("===== 버튼 =====")]
    [SerializeField] private Button exitButton;              // 앱 종료 버튼
    [SerializeField] private Button goToLoginButton;         // 로그인 화면 이동 버튼

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
        GameManager.Instance.QuitApplication();
    }

    private void OnGoToLoginClicked()
    {
        GameManager.Instance.Logout();
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
