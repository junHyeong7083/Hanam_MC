using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem10 Step3 완료 화면의 포스터 표시
/// - Gate의 completeRoot에 붙이는 스크립트
/// - OnEnable() 시 SharedData에서 데이터 읽어서 UI 업데이트
/// </summary>
public class Problem10CompletePosterDisplay : MonoBehaviour
{
    [Header("===== 공유 데이터 =====")]
    [Tooltip("Step2, Step3과 같은 SharedData 에셋 연결")]
    [SerializeField] private Problem10SharedData sharedData;

    [Header("===== 포스터 UI =====")]
    [Tooltip("장르 이모지")]
    [SerializeField] private Text genreEmojiText;

    [Tooltip("영화 제목")]
    [SerializeField] private Text movieTitleText;

    [Tooltip("다짐 선언")]
    [SerializeField] private Text commitmentText;

    [Tooltip("장르 이름")]
    [SerializeField] private Text genreNameText;

    private void OnEnable()
    {
        UpdateDisplay();
    }

    /// <summary>SharedData에서 데이터 읽어서 UI 업데이트</summary>
    public void UpdateDisplay()
    {
        if (sharedData == null)
        {
            Debug.LogWarning("[Problem10CompletePosterDisplay] SharedData가 연결되지 않았습니다.");
            return;
        }

        if (genreEmojiText != null)
            genreEmojiText.text = sharedData.selectedGenreEmoji;

        if (movieTitleText != null)
            movieTitleText.text = sharedData.movieTitle;

        if (commitmentText != null)
            commitmentText.text = sharedData.commitment;

        if (genreNameText != null)
            genreNameText.text = sharedData.selectedGenreName;
    }
}
