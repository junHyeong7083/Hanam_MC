using UnityEngine;

/// <summary>
/// Problem10SharedData - 문제10의 여러 스텝 간 공유 데이터 (ScriptableObject)
///
/// 【역할】 Step2에서 선택한 장르 인덱스와 스프라이트를 Step3에서 참조할 수 있도록
///          런타임 공유 데이터를 제공한다. Step3 완료 시 포스터 텍스트도 저장하여
///          Problem10CompletePosterDisplay에서 참조한다.
/// 【패턴】 ScriptableObject 기반 런타임 공유 데이터 패턴.
///          인스펙터에서 같은 에셋을 Step2, Step3, CompletePosterDisplay에 연결하면
///          스텝 간 데이터를 전달할 수 있다.
/// 【문제/스텝】 Director 테마 > 문제10 (Step2 ↔ Step3 ↔ CompletePosterDisplay)
/// 【참조하는 곳】 Director_Problem10_Step2_Logic, Director_Problem10_Step3_Logic,
///               Problem10CompletePosterDisplay
/// 【주의】 ScriptableObject는 에디터에서 값이 유지되므로 시작 시 Clear() 필요.
/// </summary>
[CreateAssetMenu(menuName = "MindMovie/Problem10 Shared Data", fileName = "Problem10SharedData")]
public class Problem10SharedData : ScriptableObject
{
    [HideInInspector] public int selectedGenreIndex = -1;    // Step2에서 선택한 장르 인덱스 (0~3, -1은 미선택)
    [HideInInspector] public Sprite selectedSprite;          // Step2에서 선택한 장르 스프라이트
    [HideInInspector] public string posterTitle = "";        // 포스터 제목 (현재 미사용, 확장용)
    [HideInInspector] public string posterCommitment = "";   // 포스터 다짐 텍스트 (Step3에서 설정)

    /// <summary>
    /// Step2에서 호출. 선택한 장르 인덱스와 스프라이트를 저장한다.
    /// </summary>
    /// <param name="index">선택한 장르 인덱스 (0~3)</param>
    /// <param name="sprite">선택한 장르의 카드 스프라이트</param>
    public void SetSelection(int index, Sprite sprite)
    {
        selectedGenreIndex = index;
        selectedSprite = sprite;
    }

    /// <summary>
    /// Step3에서 호출. 포스터에 작성된 제목과 다짐 텍스트를 저장한다.
    /// </summary>
    /// <param name="title">포스터 제목 (현재 빈 문자열 전달)</param>
    /// <param name="commitment">포스터 다짐 텍스트 (STT로 인식한 텍스트)</param>
    public void SetPosterTexts(string title, string commitment)
    {
        posterTitle = title ?? "";
        posterCommitment = commitment ?? "";
    }

    /// <summary>
    /// 모든 공유 데이터를 초기 상태로 리셋한다.
    /// 문제 시작 시 호출하여 이전 플레이 데이터를 정리해야 한다.
    /// </summary>
    public void Clear()
    {
        selectedGenreIndex = -1;
        selectedSprite = null;
        posterTitle = "";
        posterCommitment = "";
    }
}
