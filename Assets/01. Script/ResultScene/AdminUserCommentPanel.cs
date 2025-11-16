using TMPro;
using UnityEngine;

public class AdminUserCommentPanel : MonoBehaviour
{
    public static AdminUserCommentPanel Instance { get; private set; }

    [Header("commentPanel")]
    public GameObject commentPanel;           // 패널 전체 (없으면 this.gameObject 사용)

    [Header("Header UI")]
    public TMP_Text userNameText;
    public TMP_Text userEmailText;

    [Header("Comment UI")]
    public TMP_InputField commentInput;
    public TMP_InputField scoreInput; // 점수 입력할 거면

    IAdminDataService _adminData = new LocalAdminDataService();

    UserSummary _user;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!commentPanel) commentPanel = gameObject;
        commentPanel.SetActive(false);
    }

    /// <summary>
    /// 어떤 유저에 대한 코멘트 패널을 열지 요청
    /// </summary>
    public void Open(UserSummary user)
    {
        _user = user;

        if (!commentPanel) commentPanel = gameObject;
        commentPanel.SetActive(true);

        if (userNameText) userNameText.text = user.Name ?? "(이름 없음)";
        if (userEmailText) userEmailText.text = user.Email ?? "";

        // TODO: 필요하면 여기서 해당 유저의 최근 결과/피드백 로딩
        // var res = _adminData.FetchResultsByUser(user.Email);
        // res.Ok이면 result 리스트를 다른 UI에 뿌리거나,
        // 가장 최근 ResultDoc 하나 잡아서 기본 타겟으로 쓰면 됨
    }

    public void Close()
    {
        if (commentPanel) commentPanel.SetActive(false);
        _user = null;
    }

    // 닫기 버튼에 연결
    public void OnClickClose()
    {
        Close();
    }

    // "저장" 버튼에 연결하는 예시 (LiteDB에 Feedback 저장)
    public void OnClickSaveFeedback(string resultId)
    {
        if (_user == null) return;

        float? score = null;
        if (scoreInput && float.TryParse(scoreInput.text, out var s))
            score = s;

        var fb = new Feedback
        {
            AdminEmail = SessionManager.Instance?.CurrentUser?.Email,
            Comment = commentInput ? commentInput.text : null,
            Score = score
        };

        var r = _adminData.SubmitFeedback(resultId, fb);
        if (!r.Ok)
        {
            Debug.LogWarning($"[AdminUserCommentPanel] SubmitFeedback 실패: {r.Error}");
        }
        else
        {
            Debug.Log($"[AdminUserCommentPanel] 피드백 저장 완료: user={_user.Email}, result={resultId}");
        }
    }
}
