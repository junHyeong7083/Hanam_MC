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

    private bool _isOptionOpen = false;
    private Coroutine _optionAnimCoroutine;

    private void Start()
    {
        // 옵션 패널 초기 상태
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
            _isOptionOpen = false;
        }
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

    /// <summary>
    /// 옵션 패널 토글 - 버튼 OnClick에 연결
    /// </summary>
    public void ToggleOptionPanel()
    {
        if (optionPanel == null) return;

        if (_optionAnimCoroutine != null)
            StopCoroutine(_optionAnimCoroutine);

        if (_isOptionOpen)
        {
            _optionAnimCoroutine = StartCoroutine(CloseOptionPanel());
        }
        else
        {
            _optionAnimCoroutine = StartCoroutine(OpenOptionPanel());
        }
    }

    private IEnumerator OpenOptionPanel()
    {
        _isOptionOpen = true;
        optionPanel.SetActive(true);

        float elapsed = 0f;
        Vector3 scale = optionPanel.transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

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
        float elapsed = 0f;
        Vector3 scale = optionPanel.transform.localScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / animationDuration);

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
        _isOptionOpen = false;
    }
}
