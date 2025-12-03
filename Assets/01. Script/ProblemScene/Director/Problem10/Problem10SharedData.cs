using UnityEngine;

/// <summary>
/// Problem 10 스텝 간 공유 데이터
/// - Step2에서 선택한 장르를 Step3에서 사용
/// - ScriptableObject로 인스펙터에서 같은 에셋 연결
/// </summary>
[CreateAssetMenu(menuName = "MindMovie/Problem10 Shared Data", fileName = "Problem10SharedData")]
public class Problem10SharedData : ScriptableObject
{
    [Header("===== Step2에서 선택한 장르 =====")]
    public string selectedGenreId;
    public string selectedGenreName;
    public string selectedGenreEmoji;
    public string selectedGenreDescription;

    [Header("===== Step3에서 녹음한 내용 =====")]
    public string movieTitle;
    public string commitment;

    /// <summary>데이터 초기화</summary>
    public void Clear()
    {
        selectedGenreId = "";
        selectedGenreName = "";
        selectedGenreEmoji = "";
        selectedGenreDescription = "";
        movieTitle = "";
        commitment = "";
    }

    /// <summary>Step2에서 장르 선택 시 호출</summary>
    public void SetSelectedGenre(string id, string name, string emoji, string description)
    {
        selectedGenreId = id;
        selectedGenreName = name;
        selectedGenreEmoji = emoji;
        selectedGenreDescription = description;
    }

    /// <summary>Step3에서 녹음 결과 저장</summary>
    public void SetMovieTitle(string title)
    {
        movieTitle = title;
    }

    public void SetCommitment(string text)
    {
        commitment = text;
    }
}
