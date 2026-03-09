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
}
