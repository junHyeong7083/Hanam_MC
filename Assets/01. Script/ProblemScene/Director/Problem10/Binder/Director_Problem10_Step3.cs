using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step3_Logic(부모)에 있음.
///
/// [흐름]
/// 1. SharedData에서 선택한 장르 로드 → 포스터 프리뷰 표시
/// 2. title(영화 제목) 녹음: 마이크 클릭 → 녹음 → 클릭 → STT 결과
/// 3. commitment(다짐 선언) 녹음: 마이크 클릭 → 녹음 → 클릭 → STT 결과
/// 4. RecordingRoot 숨김 → Gate 완료 → completeRoot(포스터 완성!) 자동 표시
/// 5. "촬영 정리하기" 버튼은 인스펙터에서 직접 NextStep 연결
///
/// [STT 연동]
/// - StartMicrophoneRecording() / StopMicrophoneRecording() override하여 실제 STT 구현
/// - 현재는 더미 (빈 문자열 반환)
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

    [Header("===== 포스터 프리뷰 UI =====")]
    [Tooltip("장르 이모지 표시")]
    [SerializeField] private Text posterGenreEmoji;

    [Tooltip("장르 이름 표시")]
    [SerializeField] private Text posterGenreName;

    [Tooltip("영화 제목 표시 (STT 결과로 업데이트)")]
    [SerializeField] private Text posterTitleText;

    [Tooltip("다짐 표시 (STT 결과로 업데이트)")]
    [SerializeField] private Text posterCommitmentText;

    [Header("===== 녹음 화면 UI =====")]
    [Tooltip("조감독 안내 텍스트")]
    [SerializeField] private Text instructionText;

    [Tooltip("마이크 버튼")]
    [SerializeField] private Button micButton;

    [Tooltip("마이크 버튼 이미지 (색상 변경용)")]
    [SerializeField] private Image micButtonImage;

    [Tooltip("녹음 상태 텍스트")]
    [SerializeField] private Text recordingStatusText;

    [Header("===== 공유 데이터 =====")]
    [Tooltip("Step2와 공유하는 데이터 (같은 에셋 연결)")]
    [SerializeField] private Problem10SharedData sharedData;

    [Header("===== 완료 게이트 =====")]
    [Tooltip("completeRoot에 포스터 완성 화면 연결")]
    [SerializeField] private StepCompletionGate completionGate;

    #region 부모 추상 프로퍼티 구현

    protected override PhaseData[] PhaseDataList => phaseDataList;
    protected override GameObject RecordingRoot => recordingRoot;
    protected override Text PosterGenreEmoji => posterGenreEmoji;
    protected override Text PosterGenreName => posterGenreName;
    protected override Text PosterTitleText => posterTitleText;
    protected override Text PosterCommitmentText => posterCommitmentText;
    protected override Text InstructionText => instructionText;
    protected override Button MicButton => micButton;
    protected override Image MicButtonImage => micButtonImage;
    protected override Text RecordingStatusText => recordingStatusText;
    protected override Problem10SharedData SharedData => sharedData;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    #endregion

    #region STT 구현 (나중에 실제 STT 연동 시 override)

    // [TODO] 실제 STT 구현 시 아래 메서드 override
    // protected override void StartMicrophoneRecording() { ... }
    // protected override string StopMicrophoneRecording() { ... return sttResult; }

    #endregion
}
