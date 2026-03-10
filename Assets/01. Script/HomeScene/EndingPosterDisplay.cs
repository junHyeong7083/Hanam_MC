using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HomeScene 엔딩 패널 - 오늘 날짜 표시
/// </summary>
public class EndingPosterDisplay : MonoBehaviour
{
    [SerializeField] private Text dateText;

    private void OnEnable()
    {
        if (dateText != null)
            dateText.text = DateTime.Now.ToString("yyyy.MM.dd");
    }
   
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
