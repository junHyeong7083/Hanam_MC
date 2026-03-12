using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EndingPosterDisplay - 엔딩 포스터에 날짜를 표시하고 홈 복귀를 처리하는 컴포넌트
///
/// 【역할】 Director 테마 전체 완주 후 표시되는 엔딩 포스터에서:
///         1) 현재 날짜를 "yyyy.MM.dd" 형식으로 표시
///         2) "홈으로" 버튼 클릭 시 테마에 따라 적절한 복귀 대상을 설정하고 홈으로 이동
/// 【씬】 HomeScene (LevelSelectScene) - DirectorEndingPanel 내부
/// 【참조하는 곳】 ThemePanelsController (엔딩 패널 표시 시 활성화)
/// 【참조되는 곳】 ProblemSession (테마/복귀 대상 정보), GameManager (홈 이동)
/// 【흐름】 엔딩 패널 활성화(OnEnable) → 날짜 표시 → GoToHome() 호출 → 홈 화면 이동
/// </summary>
public class EndingPosterDisplay : MonoBehaviour
{
    [SerializeField] private Text dateText;  // 날짜 표시용 텍스트 (yyyy.MM.dd 형식)

    private void OnEnable()
    {
        if (dateText != null)
            dateText.text = DateTime.Now.ToString("yyyy.MM.dd");
    }
   
    /// <summary>
    /// 홈 화면으로 복귀한다.
    /// Director 테마인 경우 LevelSelect로, 그 외에는 테마 선택 화면(None)으로 복귀 대상 설정.
    /// 엔딩 패널의 "홈으로" 버튼 OnClick에 연결하여 사용한다.
    /// </summary>
    public void GoToHome()
    {
        // Director 테마: LevelSelectPanel 또는 EndingPanel로 복귀
        if (ProblemSession.CurrentTheme == ProblemTheme.Director)
            ProblemSession.ReturnTarget = HomeReturnTarget.LevelSelect;

        else
        {
            ProblemSession.ReturnTarget = HomeReturnTarget.None;
        }

        Debug.Log($"[CommonRewardStep] GoToHome - Theme={ProblemSession.CurrentTheme}, Index={ProblemSession.CurrentProblemIndex}, ReturnTarget={ProblemSession.ReturnTarget}");
        GameManager.Instance.GoToHome();
    }

}
