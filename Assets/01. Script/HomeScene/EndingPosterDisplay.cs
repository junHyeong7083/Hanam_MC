using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HomeScene 엔딩 패널에서 Problem10 포스터를 표시
/// - Problem10SharedData에서 선택 장르 이미지 + 제목/다짐 텍스트를 읽어옴
/// </summary>
public class EndingPosterDisplay : MonoBehaviour
{
    [Header("===== 공유 데이터 =====")]
    [SerializeField] private Problem10SharedData sharedData;

    [Header("===== 포스터 UI =====")]
    [SerializeField] private Image posterImage;
    [SerializeField] private Text titleText;
    [SerializeField] private Text commitmentText;

    private void OnEnable()
    {
        if (sharedData == null) return;

        if (posterImage != null && sharedData.selectedSprite != null)
            posterImage.sprite = sharedData.selectedSprite;

        if (titleText != null)
            titleText.text = sharedData.posterTitle;

        if (commitmentText != null)
            commitmentText.text = sharedData.posterCommitment;
    }
}
