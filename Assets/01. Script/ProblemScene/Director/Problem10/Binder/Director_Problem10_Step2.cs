using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step2_Logic(부모)에 있음.
///
/// [흐름]
/// 1. 4개 장르 카드 표시 (성장/휴먼코미디/다큐/가족)
/// 2. 하나 선택 → 확인 버튼 활성화
/// 3. 확인 버튼 클릭 → SelectionRoot 숨김 → Gate 완료
/// 4. Gate의 completeRoot 자동 표시
/// 5. "다음으로" 버튼은 인스펙터에서 직접 NextStep 연결
/// </summary>
public class Director_Problem10_Step2 : Director_Problem10_Step2_Logic
{
    [Header("===== 장르 데이터 =====")]
    [SerializeField] private GenreData[] genres = new GenreData[]
    {
        new GenreData
        {
            id = "growth",
            name = "성장 드라마",
            emoji = "🌱",
            description = "계속 배우고 발전하는 나"
        },
        new GenreData
        {
            id = "warmth",
            name = "따뜻한 휴먼 코미디",
            emoji = "🌈",
            description = "사람들과 함께 웃으며 살아가는 나"
        },
        new GenreData
        {
            id = "contribution",
            name = "사회에 기여하는 다큐멘터리",
            emoji = "🌍",
            description = "세상에 도움이 되는 일을 하는 나"
        },
        new GenreData
        {
            id = "family",
            name = "안정적인 가족 영화",
            emoji = "🏡",
            description = "소중한 사람들과 평온하게 지내는 나"
        }
    };

    [Header("===== 화면 루트 =====")]
    [Tooltip("장르 선택 화면 전체")]
    [SerializeField] private GameObject selectionRoot;

    [Header("===== 장르 카드 UI (4개) =====")]
    [SerializeField] private GenreCardUI[] genreCards;

    [Header("===== 확인 버튼 =====")]
    [Tooltip("장르 선택 후 확인 버튼 (\"이 장르로 포스터 만들기\")")]
    [SerializeField] private Button confirmButton;

    [Header("===== 완료 게이트 =====")]
    [Tooltip("completeRoot에 선택 확인 화면 연결")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("===== 공유 데이터 =====")]
    [Tooltip("Step3과 공유할 데이터 (같은 에셋 연결)")]
    [SerializeField] private Problem10SharedData sharedData;

    #region 부모 추상 프로퍼티 구현

    protected override GenreData[] Genres => genres;
    protected override GameObject SelectionRoot => selectionRoot;
    protected override GenreCardUI[] GenreCards => genreCards;
    protected override Button ConfirmButton => confirmButton;
    protected override StepCompletionGate CompletionGateRef => completionGate;
    protected override Problem10SharedData SharedData => sharedData;

    #endregion
}
