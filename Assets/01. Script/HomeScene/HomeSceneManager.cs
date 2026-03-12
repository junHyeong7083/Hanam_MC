using System.Collections;
using UnityEngine;

/// <summary>
/// HomeSceneManager - 홈 화면의 버튼 이벤트 및 옵션 패널 관리
///
/// 【역할】 홈 화면에서 로그아웃, 앱 종료, 옵션 패널 열기/닫기 등의 UI 이벤트를 처리한다.
///         옵션 패널은 Y축 스케일 + 알파 애니메이션으로 열고 닫는다.
/// 【씬】 HomeScene (LevelSelectScene)
/// 【참조하는 곳】 Canvas 내 버튼 OnClick 이벤트에 직접 연결
/// 【참조되는 곳】 GameManager (로그아웃, 앱 종료)
/// 【흐름】 버튼 클릭 → Logout() / QuitApplication() / ToggleOptionPanel()
/// </summary>
public class HomeSceneManager : MonoBehaviour
{
    [Header("Option Panel")]
    [SerializeField] private GameObject optionPanel;          // 옵션 패널 루트 오브젝트
    [SerializeField] private CanvasGroup optionCanvasGroup;   // 옵션 패널 알파/상호작용 제어용 CanvasGroup
    [SerializeField] private float animationDuration = 0.1f;  // 옵션 패널 열기/닫기 애니메이션 시간(초)

    [Header("Linked Panel (옵션과 함께 켜졌다 꺼질 패널 1개)")]
    [SerializeField] private GameObject linkedPanel;          // 옵션 패널과 함께 활성/비활성되는 연동 패널

    private bool _isOptionOpen = false;                       // 옵션 패널 현재 열림 상태
    private Coroutine _optionAnimCoroutine;                   // 현재 실행 중인 애니메이션 코루틴 참조

    private void Start()
    {
        // 옵션 패널 초기 상태
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
            _isOptionOpen = false;

            Vector3 s = optionPanel.transform.localScale;
            s.y = 0f;
            optionPanel.transform.localScale = s;
        }

        if (optionCanvasGroup != null)
            optionCanvasGroup.alpha = 0f;

        if (linkedPanel != null)
            linkedPanel.SetActive(false);
    }


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

    /// <summary>옵션 패널 열기 - 버튼 OnClick에 연결 (닫기는 패널 내 닫기 버튼으로)</summary>
    public void ToggleOptionPanel()
    {
        ShowOptionPanel();
    }

    /// <summary>옵션 열기</summary>
    public void ShowOptionPanel()
    {
        if (optionPanel == null || _isOptionOpen) return;
        if (_optionAnimCoroutine != null) StopCoroutine(_optionAnimCoroutine);
        _optionAnimCoroutine = StartCoroutine(OpenOptionPanel());
    }

    /// <summary>옵션 닫기 - 패널 내 닫기 버튼 OnClick에 연결</summary>
    public void HideOptionPanel()
    {
        if (optionPanel == null || !_isOptionOpen) return;
        if (_optionAnimCoroutine != null) StopCoroutine(_optionAnimCoroutine);
        _optionAnimCoroutine = StartCoroutine(CloseOptionPanel());
    }

    private IEnumerator OpenOptionPanel()
    {
        _isOptionOpen = true;
        optionPanel.SetActive(true);
        if (linkedPanel != null)
            linkedPanel.SetActive(true);

        if (optionCanvasGroup != null)
        {
            optionCanvasGroup.blocksRaycasts = true;
            optionCanvasGroup.interactable = true;
        }

        float elapsed = 0f;
        Vector3 scale = optionPanel.transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            if (optionCanvasGroup != null)
                optionCanvasGroup.alpha = t;

            scale.y = t;
            optionPanel.transform.localScale = scale;

            yield return null;
        }

        if (optionCanvasGroup != null)
            optionCanvasGroup.alpha = 1f;
        scale.y = 1f;
        optionPanel.transform.localScale = scale;
    }

    private IEnumerator CloseOptionPanel()
    {
        if (optionCanvasGroup != null)
        {
            optionCanvasGroup.blocksRaycasts = false;
            optionCanvasGroup.interactable = false;
        }

        float elapsed = 0f;
        Vector3 scale = optionPanel.transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / animationDuration);

            if (optionCanvasGroup != null)
                optionCanvasGroup.alpha = t;

            scale.y = t;
            optionPanel.transform.localScale = scale;

            yield return null;
        }

        if (optionCanvasGroup != null)
            optionCanvasGroup.alpha = 0f;
        scale.y = 0f;
        optionPanel.transform.localScale = scale;

        optionPanel.SetActive(false);
        if (linkedPanel != null)
            linkedPanel.SetActive(false);
        _isOptionOpen = false;
    }
}
