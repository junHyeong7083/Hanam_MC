using System;
using UnityEngine;

public class ProblemSceneController : MonoBehaviour
{
    [Header("Theme Roots")]
    [SerializeField] private GameObject directorRoot;   // Canvas 밑 Director용 루트
    [SerializeField] private GameObject gardenerRoot;   // Canvas 밑 Gardener용 루트

    private GameObject _activeRoot;

    void Start()
    {
        // 1) DataService / ProblemQueryService 존재 체크
        if (DataService.Instance == null || DataService.Instance.Problems == null)
        {
            Debug.LogError("[ProblemScene] DataService.Problems 없음. DataService 세팅을 먼저 확인하세요.");
            enabled = false;
            return;
        }

        // 2) ProblemSession 값 유효성 체크
        if (ProblemSession.CurrentProblemIndex <= 0)
        {
            Debug.LogError("[ProblemScene] ProblemSession.CurrentProblemIndex가 0 이하입니다. HomeScene에서 세팅이 안 된 상태로 넘어온 것 같습니다.");
            enabled = false;
            return;
        }

        // 3) 테마 루트 선택/활성화
        SetupThemeRoot();

        if (_activeRoot == null)
        {
            Debug.LogError("[ProblemScene] 활성화할 테마 루트가 없습니다. directorRoot/gardenerRoot 할당을 확인하세요.");
            enabled = false;
            return;
        }

        // 4) 해당 문제 인덱스만 활성화
        ActivateSingleProblem(ProblemSession.CurrentProblemIndex);

        // 5) (다음 단계용) 문제 내용 DB에서 불러오기 → UI에 바인딩
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
        else _activeRoot = null; // enum에 다른 값이 들어온 경우
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

        // index는 1-based, Transform.GetChild는 0-based
        int targetIdx = problemIndex - 1;

        if (targetIdx < 0 || targetIdx >= childCount)
        {
            Debug.LogError($"[ProblemScene] ProblemIndex={problemIndex} 가 자식 수 범위를 벗어났습니다. (childCount={childCount})");
            return;
        }

        for (int i = 0; i < childCount; i++)
        {
            bool active = (i == targetIdx);
            rootTr.GetChild(i).gameObject.SetActive(active);
        }
    }
}
