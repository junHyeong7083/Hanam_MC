using System.Collections;
using UnityEngine;

/// <summary>
/// HomeScene 버튼 이벤트 연결용 스크립트
/// - 버튼 OnClick에 연결해서 사용
/// </summary>
public class HomeSceneManager : MonoBehaviour
{
    [Header("Option Panel")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private CanvasGroup optionCanvasGroup;
    [SerializeField] private float animationDuration = 0.1f;

    [Header("Linked Panel (옵션과 함께 켜졌다 꺼질 패널 1개)")]
    [SerializeField] private GameObject linkedPanel;

    private bool _isOptionOpen = false;
    private Coroutine _optionAnimCoroutine;

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
