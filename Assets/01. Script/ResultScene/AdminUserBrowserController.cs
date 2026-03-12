using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// AdminUserBrowserController - 관리자용 사용자 목록 브라우저의 비즈니스 로직
///
/// 【역할】 관리자(ADMIN/SUPERADMIN) 전용 화면에서:
///         1) 전체 사용자 목록을 조회하여 UI에 표시
///         2) 검색창 입력에 따라 디바운스(0.25초) 적용 후 필터링된 목록 표시
///         3) 현재 로그인한 관리자 자신은 목록에서 제외
/// 【씬】 ResultScene (관리자 결과 조회 화면)
/// 【참조하는 곳】 ResultScene에 부착 (AdminUserBrowserUI와 같은 GameObject)
/// 【참조되는 곳】 DataService.Admin (사용자 검색), SessionManager (현재 사용자 확인)
/// 【흐름】 Start → RefreshAll() → 전체 목록 표시 / 검색 입력 → 디바운스 → RefreshSearch()
/// </summary>
[RequireComponent(typeof(AdminUserBrowserUI))]
public class AdminUserBrowserController : MonoBehaviour
{
    private AdminUserBrowserUI view;       // 사용자 목록 UI (같은 GameObject에서 자동 참조)
    private IAdminDataService admin;       // 관리자 데이터 서비스 (DataService에서 가져옴)

    private Coroutine debounceCo;          // 검색 디바운스 코루틴 참조
    const float DebounceSec = 0.25f;       // 검색 디바운스 대기 시간(초)

    void Awake()
    {
        view = GetComponent<AdminUserBrowserUI>();

        if (DataService.Instance == null || DataService.Instance.Admin == null)
        {
            Debug.LogError("[AdminUserBrowser] DataService.Admin 없음. DataService 설정을 먼저 확인하세요.");
            enabled = false;
            return;
        }

        admin = DataService.Instance.Admin;

        view.OnQueryChanged += HandleQueryChanged;
    }

    void Start() => RefreshAll();

    void OnDestroy()
    {
        if (view != null)
            view.OnQueryChanged -= HandleQueryChanged;
    }

    // ── 검색창 입력 디바운스 ───────────────────────────────
    void HandleQueryChanged(string q)
    {
        if (debounceCo != null)
            StopCoroutine(debounceCo);

        debounceCo = StartCoroutine(CoDebouncedSearch(q));
    }

    IEnumerator CoDebouncedSearch(string q)
    {
        yield return new WaitForSeconds(DebounceSec);

        if (string.IsNullOrWhiteSpace(q))
            RefreshAll();
        else
            RefreshSearch(q);
    }

    // ── 전체 목록 새로고침 ─────────────────────────────────
    void RefreshAll()
    {
        view.ClearList();

        if (admin == null)
        {
            Debug.LogError("[AdminUserBrowser] admin data service null");
            return;
        }

        var me = SessionManager.Instance?.CurrentUser?.Email;

        var res = admin.SearchUsers("");
        if (!res.Ok || res.Value == null || res.Value.Length == 0)
        {
            view.AddItem(new UserSummary
            {
                Name = "사용자 없음",
                Email = "",
                Role = UserRole.USER,
                IsActive = true
            });
            return;
        }

        var list = res.Value
            .Where(u => string.IsNullOrEmpty(me) || u.Email != me)   // 나 자신은 목록에서 제외
            .ToArray();

        foreach (var u in list)
            view.AddItem(u);

        if (list.Length == 0)
        {
            view.AddItem(new UserSummary
            {
                Name = "사용자 없음",
                Email = "",
                Role = UserRole.USER,
                IsActive = true
            });
        }
    }

    // ── 검색 결과 새로고침 ─────────────────────────────────
    void RefreshSearch(string q)
    {
        view.ClearList();

        if (admin == null)
        {
            Debug.LogError("[AdminUserBrowser] admin data service null");
            return;
        }

        var me = SessionManager.Instance?.CurrentUser?.Email;

        var res = admin.SearchUsers(q);
        if (!res.Ok || res.Value == null || res.Value.Length == 0)
        {
            view.AddItem(new UserSummary
            {
                Name = $"검색 결과 없음: {q}",
                Email = "",
                Role = UserRole.USER,
                IsActive = true
            });
            return;
        }

        var list = res.Value
            .Where(u => string.IsNullOrEmpty(me) || u.Email != me)
            .ToArray();

        foreach (var u in list)
            view.AddItem(u);

        if (list.Length == 0)
        {
            view.AddItem(new UserSummary
            {
                Name = $"검색 결과 없음: {q}",
                Email = "",
                Role = UserRole.USER,
                IsActive = true
            });
        }
    }
}
