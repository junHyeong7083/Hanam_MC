using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ThemePanelsController : MonoBehaviour
{
    [Serializable]
    public class ThemePanelBinding
    {
        [Tooltip("이 패널이 담당하는 테마 (Director / Gardener 등)")]
        public ProblemTheme theme = ProblemTheme.Director;

        [Tooltip("해당 테마의 문제 패널 UI")]
        public ThemePanelUI panel;

        [Tooltip("이 테마에 속한 문제 수")]
        public int totalProblems = 10;
    }


    [Header("각 테마별 패널 바인딩")]
    [SerializeField] ThemePanelBinding[] themePanels;

    // 🔹 패널별로 구독한 핸들러를 저장해두는 딕셔너리
    private readonly Dictionary<ThemePanelUI, Action<int>> _clickHandlers
        = new Dictionary<ThemePanelUI, Action<int>>();

    User currentUser;

    void Awake()
    {
        // 각 패널의 클릭 이벤트를 하나의 핸들러로 묶기
        if (themePanels == null) return;

        foreach (var entry in themePanels)
        {
            if (entry == null || entry.panel == null) continue;

            var theme = entry.theme;
            var panel = entry.panel;

            // 여기서 한 번 만든 핸들러를 딕셔너리에 저장
            Action<int> handler = index => HandleProblemClicked(theme, index);
            _clickHandlers[panel] = handler;

            panel.OnProblemClicked += handler;
        }
    }

    void OnDestroy()
    {
        if (themePanels == null) return;

        foreach (var entry in themePanels)
        {
            if (entry == null || entry.panel == null) continue;

            var panel = entry.panel;

            // Awake에서 저장한 같은 핸들러 인스턴스를 꺼내서 해제
            if (_clickHandlers.TryGetValue(panel, out var handler))
            {
                panel.OnProblemClicked -= handler;
            }
        }

        _clickHandlers.Clear();
    }

    void Start()
    {
        if (SessionManager.Instance == null || SessionManager.Instance.CurrentUser == null)
        {
            Debug.LogWarning("[ThemePanels] 로그인 상태가 아님. Register 화면으로 이동.");
            SceneNavigator.Instance?.GoTo(ScreenId.REGISTER);
            return;
        }

        currentUser = SessionManager.Instance.CurrentUser;

        RefreshAllPanels();
    }

    /// <summary>
    /// 모든 테마 패널 버튼 잠금/해제 상태 갱신
    /// </summary>
    void RefreshAllPanels()
    {
        if (themePanels == null) return;

        foreach (var entry in themePanels)
        {
            RefreshSinglePanel(entry);
        }
    }

    /// <summary>
    /// 특정 테마 패널 하나에 대해
    /// - 이미 푼 문제 목록 조회
    /// - 순차 제한 / 전체 오픈 규칙 적용
    /// - UI에 반영
    /// </summary>
    void RefreshSinglePanel(ThemePanelBinding entry)
    {
        if (entry == null || entry.panel == null) return;

        if (DataService.Instance == null || DataService.Instance.User == null)
        {
            Debug.LogError("[ThemePanels] DataService.Instance.User 가 준비되지 않음");
            return;
        }

        var theme = entry.theme;
        int totalProblems = Mathf.Max(1, entry.totalProblems);

        // enum 기반으로 호출 (LocalUserDataService에서 theme.ToString() -> DB로 전달)
        var res = DataService.Instance.User.FetchSolvedProblemIndexes(currentUser.Email, theme);
        int[] solved = (res.Ok && res.Value != null) ? res.Value : Array.Empty<int>();

        var solvedSet = new HashSet<int>(solved);
        bool allSolved = false;

        if (totalProblems > 0)
        {
            allSolved =
                solvedSet.Count >= totalProblems &&
                Enumerable.Range(1, totalProblems).All(i => solvedSet.Contains(i));
        }

        bool[] unlocked = new bool[totalProblems + 1]; // 1 기반

        int nextIndex = -1;
        if (!allSolved)
        {
            for (int i = 1; i <= totalProblems; i++)
            {
                if (!solvedSet.Contains(i))
                {
                    nextIndex = i;
                    break;
                }
            }

            if (nextIndex < 0) allSolved = true;
        }

        for (int i = 1; i <= totalProblems; i++)
        {
            if (allSolved)
            {
                // 10개 다 풀었으면 -> 전체 언락
                unlocked[i] = true;
            }
            else
            {
                // 아직 한 바퀴 안 돌았으면 -> '다음 문제'만 언락
                unlocked[i] = (i == nextIndex);
            }
        }

        entry.panel.ApplyUnlockState(unlocked);
    }

    /// <summary>
    /// 어떤 테마의 몇 번 문제 버튼이 눌렸는지 처리
    /// </summary>
    void HandleProblemClicked(ProblemTheme theme, int index)
    {
        // 방어 로직: totalProblems 안 넘는지 간단히 체크
        var binding = themePanels?.FirstOrDefault(b => b.theme == theme);
        if (binding == null)
        {
            Debug.LogWarning($"[ThemePanels] 테마 {theme} 바인딩을 찾을 수 없음");
            return;
        }

        if (index < 1 || index > binding.totalProblems)
        {
            Debug.LogWarning($"[ThemePanels] 잘못된 문제 번호 클릭: {theme} - {index}");
            return;
        }

        // ProblemScene에서 사용할 컨텍스트 세팅
        ProblemSession.CurrentTheme = theme;
        ProblemSession.CurrentProblemIndex = index;
        ProblemSession.CurrentProblemId = null; // ProblemScene에서 Theme+Index로 조회 후 채워도 됨

        SceneNavigator.Instance?.GoTo(ScreenId.PROBLEM);
    }
}
