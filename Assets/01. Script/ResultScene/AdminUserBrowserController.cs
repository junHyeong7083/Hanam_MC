using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AdminUserBrowserUI))]
public class AdminUserBrowserController : MonoBehaviour
{
    AdminUserBrowserUI view;
    IAdminDataService admin;      // 🔗 DataService에서 가져옴

    Coroutine debounceCo;
    const float DebounceSec = 0.25f;

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
