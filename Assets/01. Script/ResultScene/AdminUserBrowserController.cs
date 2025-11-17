using System.Collections;
using System.Linq;             
using UnityEngine;

[RequireComponent(typeof(AdminUserBrowserUI))]
public class AdminUserBrowserController : MonoBehaviour
{
    AdminUserBrowserUI view; // UI 쪽
    UserRepository repo; // 데이터 접근용

    Coroutine debounceCo;
    const float DebounceSec = 0.25f;

    void Awake()
    {
        view = GetComponent<AdminUserBrowserUI>();
        repo = new UserRepository();

        // UI가 프레임마다 조합 포함 문자열 푸시 -> 디바운싱후 검색 실행
        view.OnQueryChanged += HandleQueryChanged;
    }

    void Start() => RefreshAll();

    void OnDestroy()
    {
        if (view) view.OnQueryChanged -= HandleQueryChanged;
    }

    // UI에서 쿼리 들어올때마다 호출
    void HandleQueryChanged(string q)
    {
        if (debounceCo != null) StopCoroutine(debounceCo);
        debounceCo = StartCoroutine(CoDebouncedSearch(q));
    }

    IEnumerator CoDebouncedSearch(string q)
    {
        yield return new WaitForSeconds(DebounceSec);

        if (string.IsNullOrWhiteSpace(q)) RefreshAll();
        else RefreshSearch(q);
    }

    // 전체목록 새로고침
    void RefreshAll()
    {
        view.ClearList();

        var me = SessionManager.Instance?.CurrentUser?.Email;  
        var list = repo.ListAllUsers(limit: 2000)
                       .Where(u => string.IsNullOrEmpty(me) || u.Email != me) 
                       .ToArray();

        foreach (var u in list) view.AddItem(u);
        if (list.Length == 0)
            view.AddItem(new UserSummary { Name = "사용자 없음", Email = "", Role = UserRole.USER, IsActive = true });
    }

    // 검색어 기반 갱신
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