using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ProblemSceneController - ProblemScene 씬의 루트 컨트롤러
///
/// 【역할】 ProblemScene이 로드되면, ProblemSession에 저장된 테마(Director/Gardener)와
///          문제 번호(CurrentProblemIndex)를 읽어 적절한 테마 루트를 활성화하고,
///          해당 테마 루트 아래에서 문제 번호에 맞는 자식 오브젝트만 켠다.
///          또한 옵션 패널(설정/메뉴)의 열기/닫기 애니메이션과
///          홈/스테이지 선택/챕터 선택/로그아웃/종료 등 네비게이션 버튼 이벤트를 처리한다.
/// 【참조하는 곳】 ProblemScene 씬의 루트 Canvas에 부착 → 버튼 OnClick 이벤트에서 호출
/// 【참조되는 곳】 ProblemSession (테마/문제번호), DataService (DB 유효성 체크),
///                GameManager (씬 전환), SceneNavigator
/// 【흐름】 Start() → SetupThemeRoot() → ActivateSingleProblem()
///          → 이후 StepFlowController가 스텝 진행을 담당
/// </summary>
public class ProblemSceneController : MonoBehaviour
{
    [Header("Theme Roots")]
    [SerializeField] private GameObject directorRoot;   // Director 테마의 최상위 루트 오브젝트 (하위에 Problem_1 ~ Problem_10)
    [SerializeField] private GameObject gardenerRoot;   // Gardener 테마의 최상위 루트 오브젝트

    [Header("Option Panel")]
    [SerializeField] private GameObject optionPanel;        // 옵션(설정) 패널 GameObject
    [SerializeField] private CanvasGroup optionCanvasGroup; // 옵션 패널의 CanvasGroup (페이드 애니메이션용)
    [SerializeField] private float animationDuration = 0.1f; // 옵션 패널 열기/닫기 애니메이션 시간 (초)

    [Header("Linked Panel (옵션과 함께 켜졌다 꺼질 패널 1개)")]
    [SerializeField] private GameObject linkedPanel; // 옵션 패널과 함께 표시/숨김되는 딤드(어두운 배경) 패널

    private GameObject _activeRoot;            // 현재 활성화된 테마 루트 (directorRoot 또는 gardenerRoot)
    private bool _isOptionOpen = false;        // 옵션 패널이 현재 열려있는지 여부
    private Coroutine _optionAnimCoroutine;    // 옵션 패널 애니메이션 코루틴 (중복 방지용)

    /// <summary>
    /// 씬 초기화: DataService/ProblemSession 유효성 검증 → 테마 루트 설정 → 문제 활성화 → 옵션 패널 초기화
    /// </summary>
    void Start()
    {
        // DataService가 준비되지 않았으면 씬 동작 중지
        if (DataService.Instance == null || DataService.Instance.Problems == null)
        {
            Debug.LogError("[ProblemScene] DataService.Problems 없음.");
            enabled = false;
            return;
        }

        // HomeScene에서 문제를 선택하지 않고 바로 들어온 경우 방어
        if (ProblemSession.CurrentProblemIndex <= 0)
        {
            Debug.LogError("[ProblemScene] ProblemSession.CurrentProblemIndex가 0 이하입니다.");
            enabled = false;
            return;
        }

        // 테마에 맞는 루트 오브젝트 활성화
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

    /// <summary>
    /// ProblemSession.CurrentTheme에 따라 directorRoot 또는 gardenerRoot를 활성화하고,
    /// 나머지는 비활성화한다. _activeRoot에 현재 활성 루트를 저장한다.
    /// </summary>
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

    /// <summary>
    /// 활성 테마 루트 아래의 자식 중 problemIndex에 해당하는 것만 활성화하고 나머지는 비활성화한다.
    /// 자식은 Problem_1, Problem_2, ... 순서로 배치되어 있으므로, problemIndex - 1이 배열 인덱스가 된다.
    /// </summary>
    /// <param name="problemIndex">문제 번호 (1-based, 예: 1~10)</param>
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

        // problemIndex는 1-based이므로 0-based 배열 인덱스로 변환
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

    // ===== 버튼 이벤트용 함수 (UI Button OnClick에 연결) =====

    /// <summary>
    /// 홈 화면으로 이동한다. GameManager를 통해 씬 전환.
    /// </summary>
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

    /// <summary>
    /// 스테이지 선택 화면(LevelSelectPanel)으로 이동한다.
    /// ReturnTarget을 LevelSelect로 설정한 후 홈으로 전환하면, HomeScene이 LevelSelectPanel을 자동으로 연다.
    /// </summary>
    public void GoToStageSelect()
    {
        ProblemSession.ReturnTarget = HomeReturnTarget.LevelSelect;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToHome();
    }

    /// <summary>
    /// 챕터(테마) 선택 화면으로 이동한다.
    /// ReturnTarget을 None으로 설정하면 HomeScene이 기본 테마 선택 패널을 표시한다.
    /// </summary>
    public void GoToChapterSelect()
    {
        ProblemSession.ReturnTarget = HomeReturnTarget.None;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToHome();
    }

    /// <summary>로그아웃 처리. GameManager를 통해 세션 정리 후 로그인 화면으로 이동.</summary>
    public void Logout()
    {
        if (GameManager.Instance != null) GameManager.Instance.Logout();
    }

    /// <summary>
    /// 옵션 패널 열기 - 버튼 OnClick에 연결 (닫기는 패널 내 닫기 버튼으로)
    /// </summary>
    public void ToggleOptionPanel()
    {
        ShowOptionPanel();
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

    /// <summary>
    /// 옵션 패널 열기 애니메이션 코루틴.
    /// Y 스케일 0→1 + 알파 0→1로 펼쳐지는 효과.
    /// linkedPanel(딤드 배경)도 함께 활성화한다.
    /// </summary>
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

    /// <summary>
    /// 옵션 패널 닫기 애니메이션 코루틴.
    /// Y 스케일 1→0 + 알파 1→0으로 접히는 효과.
    /// 애니메이션 완료 후 패널과 linkedPanel을 비활성화한다.
    /// </summary>
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

    /// <summary>앱 종료. GameManager를 통해 처리하거나, 없으면 직접 Application.Quit() 호출.</summary>
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