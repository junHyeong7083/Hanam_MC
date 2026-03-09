using UnityEngine;
using UnityEngine.UI;

public enum StageHeaderMode
{
    TITLE   ,
    DESCRIPTION,
    REVIEW,
    START,
    REWARD
}

/// <summary>
/// 스테이지 타이틀/설명 텍스트 자동 세팅
/// - Title → 101000010 + stageIndex
/// - Description → 101000020 + stageIndex
/// - Review → textId 900000050
/// - Start → textId 900000051
/// - Reward → textId 101000053
/// </summary>
[RequireComponent(typeof(Text))]
public class StageHeaderText : MonoBehaviour
{
    [SerializeField] private StageHeaderMode mode = StageHeaderMode.TITLE;

    private const int ReviewTextId = 900000050;
    private const int StartTextId = 900000051;
    private const int RewardTextId = 101000053;
    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        if (_text == null) return;

        switch (mode)
        {
            case StageHeaderMode.REVIEW:
                _text.text = ProblemRuntime.L(ReviewTextId);
                return;
            case StageHeaderMode.START:
                _text.text = ProblemRuntime.L(StartTextId);
                return;
            case StageHeaderMode.REWARD:
                _text.text = ProblemRuntime.L(RewardTextId);
                return;
        }

        int stageIndex = ProblemSession.CurrentProblemIndex;
        if (stageIndex <= 0) return;

        int baseId = mode == StageHeaderMode.TITLE ? 101000010 : 101000020;
        _text.text = ProblemRuntime.L(baseId + stageIndex);
    }
}
