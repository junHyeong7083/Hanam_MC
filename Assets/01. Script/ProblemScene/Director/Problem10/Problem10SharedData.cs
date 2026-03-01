using UnityEngine;

/// <summary>
/// Problem 10 스텝 간 공유 데이터
/// - Step2에서 선택한 장르 인덱스 + 스프라이트를 Step3에서 사용
/// </summary>
[CreateAssetMenu(menuName = "MindMovie/Problem10 Shared Data", fileName = "Problem10SharedData")]
public class Problem10SharedData : ScriptableObject
{
    [HideInInspector] public int selectedGenreIndex = -1;
    [HideInInspector] public Sprite selectedSprite;
    [HideInInspector] public string posterTitle = "";
    [HideInInspector] public string posterCommitment = "";

    public void SetSelection(int index, Sprite sprite)
    {
        selectedGenreIndex = index;
        selectedSprite = sprite;
    }

    public void SetPosterTexts(string title, string commitment)
    {
        posterTitle = title ?? "";
        posterCommitment = commitment ?? "";
    }

    public void Clear()
    {
        selectedGenreIndex = -1;
        selectedSprite = null;
        posterTitle = "";
        posterCommitment = "";
    }
}
