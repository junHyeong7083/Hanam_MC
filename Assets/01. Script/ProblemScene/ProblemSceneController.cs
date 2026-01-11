using System;
using UnityEngine;

public class ProblemSceneController : MonoBehaviour
{
    [Header("Theme Roots")]
    [SerializeField] private GameObject directorRoot;   // Canvas �� Director�� ��Ʈ
    [SerializeField] private GameObject gardenerRoot;   // Canvas �� Gardener�� ��Ʈ

    private GameObject _activeRoot;

    void Start()
    {
        // 1) DataService / ProblemQueryService ���� üũ
        if (DataService.Instance == null || DataService.Instance.Problems == null)
        {
            Debug.LogError("[ProblemScene] DataService.Problems ����. DataService ������ ���� Ȯ���ϼ���.");
            enabled = false;
            return;
        }

        // 2) ProblemSession �� ��ȿ�� üũ
        if (ProblemSession.CurrentProblemIndex <= 0)
        {
            Debug.LogError("[ProblemScene] ProblemSession.CurrentProblemIndex�� 0 �����Դϴ�. HomeScene���� ������ �� �� ���·� �Ѿ�� �� �����ϴ�.");
            enabled = false;
            return;
        }

        // 3) �׸� ��Ʈ ����/Ȱ��ȭ
        SetupThemeRoot();

        if (_activeRoot == null)
        {
            Debug.LogError("[ProblemScene] Ȱ��ȭ�� �׸� ��Ʈ�� �����ϴ�. directorRoot/gardenerRoot �Ҵ��� Ȯ���ϼ���.");
            enabled = false;
            return;
        }

        // 4) �ش� ���� �ε����� Ȱ��ȭ
        ActivateSingleProblem(ProblemSession.CurrentProblemIndex);

        // 5) (���� �ܰ��) ���� ���� DB���� �ҷ����� �� UI�� ���ε�
        // LoadProblemDataAndBind();
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
        else _activeRoot = null; // enum�� �ٸ� ���� ���� ���
    }

    private void ActivateSingleProblem(int problemIndex)
    {
        if (_activeRoot == null) return;

        Transform rootTr = _activeRoot.transform;

        int childCount = rootTr.childCount;
        if (childCount == 0)
        {
            Debug.LogWarning("[ProblemScene] Ȱ�� ��Ʈ�� �ڽ��� �����ϴ�.");
            return;
        }

        // index�� 1-based, Transform.GetChild�� 0-based
        int targetIdx = problemIndex - 1;

        if (targetIdx < 0 || targetIdx >= childCount)
        {
            Debug.LogError($"[ProblemScene] ProblemIndex={problemIndex} �� �ڽ� �� ������ ������ϴ�. (childCount={childCount})");
            return;
        }

        for (int i = 0; i < childCount; i++)
        {
            bool active = (i == targetIdx);
            rootTr.GetChild(i).gameObject.SetActive(active);
        }
    }

    // ===== 버튼 이벤트용 함수 =====

    /// <summary>
    /// 홈화면으로 이동 (로그아웃 없음) - 버튼 OnClick에 연결
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
    /// 앱 종료 - 버튼 OnClick에 연결
    /// </summary>
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
