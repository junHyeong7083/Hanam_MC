using TMPro;
using UnityEngine;

/// <summary>
/// AdminUserCommentPanel - 관리자가 특정 사용자에 대해 코멘트를 입력하는 패널
///
/// 【역할】 사용자 목록에서 특정 사용자를 선택하면 이 패널이 열리며,
///         선택된 사용자의 이름을 헤더에 표시한다.
///         싱글턴 패턴(Instance)으로 AdminUserItemUI에서 직접 접근 가능하다.
/// 【씬】 ResultScene (관리자 결과 조회 화면)
/// 【참조하는 곳】 AdminUserItemUI (코멘트 버튼 클릭 시 Open() 호출)
/// 【참조되는 곳】 없음 (싱글턴으로 자체 접근)
/// 【흐름】 AdminUserItemUI.OnClickComment() → AdminUserCommentPanel.Instance.Open(user) → 패널 표시
/// </summary>
public class AdminUserCommentPanel : MonoBehaviour
{
    /// <summary>싱글턴 인스턴스 (씬 내 유일)</summary>
    public static AdminUserCommentPanel Instance { get; private set; }

    [Header("Root")]
    public GameObject root;              // 패널 루트 오브젝트 (활성/비활성 전환 대상)

    [Header("Header UI")]
    public TMP_Text userNameText;        // 선택된 사용자 이름 표시 텍스트

    private UserSummary _user;           // 현재 열려 있는 사용자 정보

    void Awake()
    {
        Instance = this;

        if (!root) root = gameObject;
        root.SetActive(false);   // ó���� �� ���̰�
    }

    /// <summary>
    /// 특정 사용자의 코멘트 패널을 연다.
    /// 사용자 이름을 헤더에 표시하고 패널을 활성화한다.
    /// </summary>
    public void Open(UserSummary user)
    {
        _user = user;

        if (!root) root = gameObject;
        root.SetActive(true);

        if (userNameText) userNameText.text = user.Name ?? "(�̸� ����)";
        Debug.Log($"[AdminUserCommentPanel] Open: {user.Email}");
    }

    /// <summary>패널을 닫고 사용자 정보를 초기화한다</summary>
    public void Close()
    {
        if (root) root.SetActive(false);
        _user = null;
    }

    /// <summary>닫기 버튼 OnClick에 연결하여 사용</summary>
    public void OnClickClose()
    {
        Close();
    }
}
