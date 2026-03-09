using UnityEngine;
using UnityEngine.UI;

public class Director_Problem2_Step3 : Director_Problem2_Step3_Logic
{
    [Header("데이터")]
    [SerializeField] private int ngSentenceTextId = 0;

    [Header("상단 안내 텍스트 (Retry용)")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdRetry = 0;

    [Header("씬 카드 UI (NG / OK)")]
    [SerializeField] private RectTransform sceneCardRect;
    [SerializeField] private GameObject okSceneCard;

    [Header("관점 선택 버튼")]
    [SerializeField] private SelectionSlot[] selectionSlots;

    [Header("마이크 UI")]
    [SerializeField] private GameObject micButtonRoot;
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("녹음 중 표시 이미지 (SetActive 토글)")]
    [SerializeField] private GameObject recordingOverlay;

    [Header("패널 전환")]
    [SerializeField] private GameObject stepRoot;

    [Header("완료 시 선택한 관점 텍스트")]
    [SerializeField] private Text completionText;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override string NgSentence => ProblemRuntime.L(ngSentenceTextId);

    protected override Text GuideText => guideText;
    protected override int GuideTextId_Retry => guideTextIdRetry;

    protected override RectTransform SceneCardRect => sceneCardRect;
    protected override GameObject OkSceneCard => okSceneCard;

    protected override SelectionSlot[] SelectionSlots => selectionSlots;

    protected override GameObject MicButtonRoot => micButtonRoot;
    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject RecordingOverlay => recordingOverlay;

    protected override GameObject StepRoot => stepRoot;

    protected override Text CompletionText => completionText;
    protected override StepCompletionGate CompletionGate => completionGate;
}
