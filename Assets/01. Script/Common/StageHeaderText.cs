using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 타이틀/설명 텍스트 자동 세팅
/// - isTitle=true → 101000010 + stageIndex (타이틀)
/// - isTitle=false → 101000020 + stageIndex (설명)
/// - showReviewLabel=true → "지난 주 복습" 표시 (설명 전용)
/// </summary>
[RequireComponent(typeof(Text))]
public class StageHeaderText : MonoBehaviour
{
    [Tooltip("체크: 타이틀 / 해제: 설명")]
    [SerializeField] private bool isTitle = true;

    [Tooltip("체크 시 '지난 주 복습' 표시 (설명 텍스트 전용)")]
    [SerializeField] private bool showReviewLabel = false;

    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void OnEnable()
    {
        if (_text == null) return;

        if (showReviewLabel)
        {
            _text.text = "지난 주 복습";
            return;
        }

        int stageIndex = ProblemSession.CurrentProblemIndex;
        if (stageIndex <= 0) return;

        int baseId = isTitle ? 101000010 : 101000020;
        _text.text = ProblemRuntime.L(baseId + stageIndex);
    }
}
