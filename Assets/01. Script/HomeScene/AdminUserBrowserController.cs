using System.Collections;
using System.Linq;                // ← 추가
using UnityEngine;

[RequireComponent(typeof(AdminUserBrowserUI))]
public class AdminUserBrowserController : MonoBehaviour
{
    AdminUserBrowserUI view;
    UserRepository repo;

    Coroutine debounceCo;
    const float DebounceSec = 0.25f;

    void Awake()
    {
        view = GetComponent<AdminUserBrowserUI>();
        repo = new UserRepository();
        view.OnQueryChanged += HandleQueryChanged;
    }

    void Start() => RefreshAll();

    void OnDestroy()
    {
        if (view) view.OnQueryChanged -= HandleQueryChanged;
    }

    void HandleQueryChanged(string q)
    {
        if (debounceCo != null) StopCoroutine(debounceCo);
        debounceCo = StartCoroutine(CoDebouncedSearch(q));
    }

    IEnumerator CoDebouncedSearch(string q)
    {
        yield return new WaitForSeconds(DebounceSec);

        // before: if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) RefreshAll();
        if (string.IsNullOrWhiteSpace(q)) RefreshAll();
        else RefreshSearch(q); // ← 1글자부터 검색
    }

    void RefreshAll()
    {
        view.ClearList();

        var me = SessionManager.Instance?.CurrentUser?.Email;  // ← 현재 로그인 이메일
        var list = repo.ListAllUsers(limit: 2000)
                       .Where(u => string.IsNullOrEmpty(me) || u.Email != me) // ← 나 제외
                       .ToArray();

        foreach (var u in list) view.AddItem(u);
        if (list.Length == 0)
            view.AddItem(new UserSummary { Name = "사용자 없음", Email = "", Role = UserRole.USER, IsActive = true });
    }

    void RefreshSearch(string q)
    {
        view.ClearList();

        var me = SessionManager.Instance?.CurrentUser?.Email;  // ← 현재 로그인 이메일
        var list = repo.SearchUsersFriendly(q)
                       .Where(u => string.IsNullOrEmpty(me) || u.Email != me) // ← 나 제외
                       .ToArray();

        foreach (var u in list) view.AddItem(u);
        if (list.Length == 0)
            view.AddItem(new UserSummary { Name = $"검색 결과 없음: {q}", Email = "", Role = UserRole.USER, IsActive = true });
    }
}
