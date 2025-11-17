using TMPro;
using UnityEngine;

public class AdminUserCommentPanel : MonoBehaviour
{
    public static AdminUserCommentPanel Instance { get; private set; }

    [Header("Root")]
    public GameObject root;

    [Header("Header UI")]
    public TMP_Text userNameText;

    UserSummary _user;

    void Awake()
    {
        Instance = this;

        if (!root) root = gameObject;
        root.SetActive(false);   // 처음엔 안 보이게
    }

    public void Open(UserSummary user)
    {
        _user = user;

        if (!root) root = gameObject;
        root.SetActive(true);

        if (userNameText) userNameText.text = user.Name ?? "(이름 없음)";
        Debug.Log($"[AdminUserCommentPanel] Open: {user.Email}");
    }

    public void Close()
    {
        if (root) root.SetActive(false);
        _user = null;
    }

    public void OnClickClose()
    {
        Close();
    }
}
