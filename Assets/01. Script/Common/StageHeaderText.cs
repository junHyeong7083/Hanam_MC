using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 헤더 텍스트의 표시 모드.
/// 각 모드에 따라 다른 textId 규칙으로 CSV에서 텍스트를 가져온다.
/// </summary>
public enum StageHeaderMode
{
    /// <summary>스테이지 제목 (baseId 101000010 + stageIndex)</summary>
    TITLE   ,
    /// <summary>스테이지 설명 (baseId 101000020 + stageIndex)</summary>
    DESCRIPTION,
    /// <summary>복습 화면 고정 텍스트 (textId 900000050)</summary>
    REVIEW,
    /// <summary>시작 화면 고정 텍스트 (textId 900000051)</summary>
    START,
    /// <summary>보상 화면 고정 텍스트 (textId 101000053)</summary>
    REWARD
}

/// <summary>
/// StageHeaderText - 스테이지 타이틀/설명 텍스트를 자동으로 세팅하는 컴포넌트
///
/// 【역할】 스테이지(문제) 선택 화면이나 헤더 영역에서 해당 스테이지의 제목, 설명,
///          복습/시작/보상 등의 고정 텍스트를 CSV DataTable에서 가져와 표시한다.
///          TITLE/DESCRIPTION 모드는 stageIndex 기반으로 동적 textId를 계산하고,
///          REVIEW/START/REWARD 모드는 고정 textId를 사용한다.
///
/// 【참조하는 곳】 ProblemScene, LevelSelectScene 등의 헤더 UI 오브젝트에 부착
/// 【참조되는 곳】 ProblemRuntime.L(textId) — CSV에서 텍스트 로드,
///                ProblemSession.CurrentProblemIndex — 현재 선택된 스테이지 번호
///
/// 【흐름】
///   1. Awake()에서 Text 컴포넌트 캐싱
///   2. OnEnable() 시 mode에 따라 textId를 결정하고 텍스트 설정
///   3. TITLE/DESCRIPTION: 101000010(또는 20) + stageIndex로 동적 계산
///   4. REVIEW/START/REWARD: 고정 textId 사용
/// </summary>
[RequireComponent(typeof(Text))]
public class StageHeaderText : MonoBehaviour
{
    /// <summary>표시 모드 (인스펙터에서 선택)</summary>
    [SerializeField] private StageHeaderMode mode = StageHeaderMode.TITLE;

    /// <summary>복습 화면 고정 textId</summary>
    private const int ReviewTextId = 900000050;
    /// <summary>시작 화면 고정 textId</summary>
    private const int StartTextId = 900000051;
    /// <summary>보상 화면 고정 textId</summary>
    private const int RewardTextId = 101000053;

    /// <summary>캐싱된 Text 컴포넌트 참조</summary>
    private Text _text;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    /// <summary>
    /// 활성화 시 mode에 따라 적절한 텍스트를 CSV에서 가져와 표시한다.
    /// TITLE/DESCRIPTION 모드에서는 ProblemSession.CurrentProblemIndex를 사용하여
    /// baseId + stageIndex로 동적 textId를 계산한다.
    /// </summary>
    private void OnEnable()
    {
        if (_text == null) return;

        // 고정 textId 모드: 스테이지 번호와 무관한 공용 텍스트
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

        // 동적 textId 모드: baseId + stageIndex로 계산
        // 예) TITLE 모드, stageIndex=3 → textId = 101000010 + 3 = 101000013
        int stageIndex = ProblemSession.CurrentProblemIndex;
        if (stageIndex <= 0) return;

        int baseId = mode == StageHeaderMode.TITLE ? 101000010 : 101000020;
        _text.text = ProblemRuntime.L(baseId + stageIndex);
    }
}
