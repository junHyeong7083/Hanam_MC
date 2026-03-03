using System;
using System.Collections;
using UnityEngine;

public class ProblemSceneController : MonoBehaviour
{
    [Header("Theme Roots")]
    [SerializeField] private GameObject directorRoot;
    [SerializeField] private GameObject gardenerRoot;

    [Header("Option Panel")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private CanvasGroup optionCanvasGroup;
    [SerializeField] private float animationDuration = 0.1f;

    [Header("Linked Panel (옵션과 함께 켜졌다 꺼질 패널 1개)")]
    [SerializeField] private GameObject linkedPanel;

    private GameObject _activeRoot;
    private bool _isOptionOpen = false;
    private Coroutine _optionAnimCoroutine;

    void Start()
    {
        if (DataService.Instance == null || DataService.Instance.Problems == null)
        {
            Debug.LogError("[ProblemScene] DataService.Problems 없음.");
            enabled = false;
            return;
        }

        if (ProblemSession.CurrentProblemIndex <= 0)
        {
            Debug.LogError("[ProblemScene] ProblemSession.CurrentProblemIndex가 0 이하입니다.");
            enabled = false;
            return;
        }

        SetupThemeRoot();

        if (_activeRoot == null)
        {
            Debug.LogError("[ProblemScene] 활성화된 테마 루트가 없습니다.");
            enabled = false;
            return;
        }

        ActivateSingleProblem(ProblemSession.CurrentProblemIndex);

        // 옵션 패널 초기 상태
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
            _isOptionOpen = false;
        }

        // 같이 꺼질 패널 초기 상태
        if (linkedPanel != null)
            linkedPanel.SetActive(false);

        // (선택) 캔버스그룹/스케일 초기화까지 안전하게
        if (optionCanvasGroup != null)
            optionCanvasGroup.alpha = 0f;

        if (optionPanel != null)
        {
            Vector3 s = optionPanel.transform.localScale;
            s.y = 0f;
            optionPanel.transform.localScale = s;
        }
    }

    private void SetupThemeRoot()
    {
        bool isDirector = ProblemSession.CurrentTheme == ProblemTheme.Director;
        bool isGardener = ProblemSession.CurrentTheme == ProblemTheme.Gardener;

        if (directorRoot != null)
            directorRoot.SetActive(isDirector);
        if (gardenerRoot != null)
            gardenerRoot.SetActive(isGardener);

        if (isDirector) _activeRoot = directorRoot;
        else if (isGardener) _activeRoot = gardenerRoot;
        else _activeRoot = null;
    }

    private void ActivateSingleProblem(int problemIndex)
    {
        if (_activeRoot == null) return;

        Transform rootTr = _activeRoot.transform;

        int childCount = rootTr.childCount;
        if (childCount == 0)
        {
            Debug.LogWarning("[ProblemScene] 활성 루트에 자식이 없습니다.");
            return;
        }

        int targetIdx = problemIndex - 1;

        if (targetIdx < 0 || targetIdx >= childCount)
        {
            Debug.LogError($"[ProblemScene] ProblemIndex={problemIndex} 범위 초과. (childCount={childCount})");
            return;
        }

        for (int i = 0; i < childCount; i++)
        {
            bool active = (i == targetIdx);
            rootTr.GetChild(i).gameObject.SetActive(active);
        }
    }

    // ===== 버튼 이벤트용 함수 =====

    public void GoToHome()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToHome();
        }
        else
        {
            Debug.LogWarning("[ProblemSceneController] GameManager가 없습니다.");
        }
    }

    public void GoToStageSelect()
    {
        ProblemSession.ReturnTarget = HomeReturnTarget.LevelSelect;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToHome();
    }

    public void GoToChapterSelect()
    {
        ProblemSession.ReturnTarget = HomeReturnTarget.None;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToHome();
    }

    public void Logout()
    {
        if (GameManager.Instance != null) GameManager.Instance.Logout();
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
            _optionAnimCoroutine = StartCoroutine(CloseOptionPanel());
        else
            _optionAnimCoroutine = StartCoroutine(OpenOptionPanel());
    }

    /// <summary>옵션 열기 - 기본 버튼(Button1) OnClick에 연결</summary>
    public void ShowOptionPanel()
    {
        if (optionPanel == null || _isOptionOpen) return;
        if (_optionAnimCoroutine != null) StopCoroutine(_optionAnimCoroutine);
        _optionAnimCoroutine = StartCoroutine(OpenOptionPanel());
    }

    /// <summary>옵션 닫기 - 딤드 레이어 위 버튼(Button2) OnClick에 연결</summary>
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

        // (선택) 열릴 때 클릭 막힘 방지
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
        // (선택) 닫히는 중 클릭 뚫림/씹힘 방지
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

    public void QuitApplication()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitApplication();
        }
        else
        {
            Debug.LogWarning("[ProblemSceneController] GameManager가 없습니다.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}