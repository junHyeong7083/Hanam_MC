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

    /// <summary>스테이지 선택 화면으로 이동 (LevelSelectPanel)</summary>
    public void GoToStageSelect()
    {
        ProblemSession.ReturnTarget = HomeReturnTarget.LevelSelect;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToHome();
    }

    /// <summary>챕터 선택 화면으로 이동 (HomeScene 기본)</summary>
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
        {
            // 닫기 애니메이션
            _optionAnimCoroutine = StartCoroutine(CloseOptionPanel());
        }
        else
        {
            // 열기 애니메이션
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

            // Alpha 0 → 1
            if (optionCanvasGroup != null)
                optionCanvasGroup.alpha = t;

            // Scale Y 0 → 1
            scale.y = t;
            optionPanel.transform.localScale = scale;

            yield return null;
        }

        // 최종값 보정
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

            // Alpha 1 → 0
            if (optionCanvasGroup != null)
                optionCanvasGroup.alpha = t;

            // Scale Y 1 → 0
            scale.y = t;
            optionPanel.transform.localScale = scale;

            yield return null;
        }

        // 최종값 보정
        if (optionCanvasGroup != null)
            optionCanvasGroup.alpha = 0f;
        scale.y = 0f;
        optionPanel.transform.localScale = scale;

        optionPanel.SetActive(false);
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
