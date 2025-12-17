using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step3_Logic(부모)에 있음.
///
/// [흐름]
/// 1. SharedData에서 선택한 장르 인덱스 로드 → 해당 인덱스 이미지만 표시
/// 2. title(영화 제목) 녹음: 마이크 프리팹에서 녹음 완료 → OnRecordingComplete
/// 3. commitment(다짐 선언) 녹음: 마이크 프리팹에서 녹음 완료 → OnRecordingComplete
/// 4. RecordingRoot 숨김 → Gate 완료 → completeRoot(포스터 완성!) 자동 표시
/// 5. "촬영 정리하기" 버튼은 인스펙터에서 직접 NextStep 연결
///
/// [마이크 프리팹 연동]
/// - MicRecordingIndicator의 OnKeywordMatched/OnNoMatch 이벤트 구독
/// - 녹음 완료 시 OnRecordingComplete 호출
/// </summary>
public class Director_Problem10_Step3 : Director_Problem10_Step3_Logic
{
    [Header("===== 단계 데이터 =====")]
    [SerializeField] private PhaseData[] phaseDataList = new PhaseData[]
    {
        new PhaseData
        {
            id = "title",
            instruction = "정말 멋진 장르예요! 이제 이 영화에 제목을 붙여주세요.\n마이크 버튼을 누르고 '나의 용기 있는 첫걸음'처럼 영화 제목을 말씀해주시면 포스터에 새겨드릴게요."
        },
        new PhaseData
        {
            id = "commitment",
            instruction = "좋아요! 마지막으로, 이 영화를 세상에 공개하며 당신의 다짐을 선언해주세요!"
        }
    };

    [Header("===== 화면 루트 =====")]
    [Tooltip("녹음 화면 (title, commitment 공용)")]
    [SerializeField] private GameObject recordingRoot;

    [Header("===== 녹음 화면 - 장르별 포스터 UI =====")]
    [Tooltip("장르 인덱스별 이미지 (선택된 인덱스만 SetActive(true))")]
    [SerializeField] private GameObject[] genreImages;

    [Tooltip("장르별 영화 제목 Text (genreImages 순서와 동일)")]
    [SerializeField] private Text[] posterTitleTexts;

    [Tooltip("장르별 다짐 Text (genreImages 순서와 동일)")]
    [SerializeField] private Text[] posterCommitmentTexts;

    [Header("===== 녹음 화면 UI =====")]
    [Tooltip("조감독 안내 텍스트")]
    [SerializeField] private Text instructionText;

    [Header("===== 공유 데이터 =====")]
    [Tooltip("Step2와 공유하는 데이터 (같은 에셋 연결)")]
    [SerializeField] private Problem10SharedData sharedData;

    [Header("===== 완료 게이트 =====")]
    [Tooltip("completeRoot에 포스터 완성 화면 연결")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("===== 마이크 프리팹 =====")]
    [Tooltip("마이크 버튼 클릭 시 녹음 시작/종료")]
    [SerializeField] private MicRecordingIndicator micRecordingIndicator;

    #region 부모 추상 프로퍼티 구현

    protected override PhaseData[] PhaseDataList => phaseDataList;
    protected override GameObject RecordingRoot => recordingRoot;
    protected override GameObject[] GenreImages => genreImages;
    protected override Text[] PosterTitleTexts => posterTitleTexts;
    protected override Text[] PosterCommitmentTexts => posterCommitmentTexts;
    protected override Text InstructionText => instructionText;
    protected override Problem10SharedData SharedData => sharedData;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    #endregion

    #region 마이크 이벤트 연결

    protected override void OnStepEnter()
    {
        base.OnStepEnter();

        // 마이크 녹음 종료 이벤트 구독 (STT 결과 사용)
        if (micRecordingIndicator != null)
        {
            micRecordingIndicator.OnKeywordMatched -= HandleKeywordMatched;
            micRecordingIndicator.OnKeywordMatched += HandleKeywordMatched;
            micRecordingIndicator.OnNoMatch -= HandleNoMatch;
            micRecordingIndicator.OnNoMatch += HandleNoMatch;
        }
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        // 이벤트 구독 해제
        if (micRecordingIndicator != null)
        {
            micRecordingIndicator.OnKeywordMatched -= HandleKeywordMatched;
            micRecordingIndicator.OnNoMatch -= HandleNoMatch;
        }
    }

    private void HandleKeywordMatched(int index)
    {
        // 키워드 매칭 시에도 녹음 완료 처리
        // 참고: MicRecordingIndicator의 keywords를 빈 배열로 설정하면
        //       항상 OnNoMatch가 호출되어 STT 결과 텍스트를 받을 수 있음
        OnRecordingComplete("", 0f);
    }

    private void HandleNoMatch(string result)
    {
        // STT 인식 결과를 그대로 전달 → 포스터에 표시
        OnRecordingComplete(result, 0f);
    }

    #endregion
}
