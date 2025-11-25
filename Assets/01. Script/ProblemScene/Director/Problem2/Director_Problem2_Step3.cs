using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem2 / Step3
/// - 인스펙터에서 UI 오브젝트 + 데이터만 바인딩.
/// - 실제 동작은 Director_Problem2_Step3_Logic(부모)에서 처리.
/// </summary>
public class Director_Problem2_Step3 : Director_Problem2_Step3_Logic
{
    [Serializable]
    public class PerspectiveOption : IDirectorProblem2PerspectiveOption
    {
        public int id;          // 1..N (인스펙터에서 부여)
        [TextArea]
        public string text;     // 예: "상대도 긴장했을 수도 있어"

        // 인터페이스 구현 (로직 베이스에서 읽기용으로 사용)
        int IDirectorProblem2PerspectiveOption.Id => id;
        string IDirectorProblem2PerspectiveOption.Text => text;
    }

    [Header("문구 설정")]
    [TextArea]
    [SerializeField] private string ngSentence = "모두 나를 이상하게 생각할 거야";
    [SerializeField] private PerspectiveOption[] perspectives;

    [Header("초기 텍스트 설정 옵션")]
    [Tooltip("true면 Reset 시 항상 ngSentence로 덮어씀, false면 외부에서 미리 넣어둔 sceneText를 그대로 사용")]
    [SerializeField] private bool overwriteSceneTextOnReset = false;

    [Header("씬 카드 UI (NG / OK)")]
    [SerializeField] private Text sceneText;                // 카드 안에 들어갈 문장 텍스트
    [SerializeField] private GameObject ngBadgeRoot;        // "NG" 배지 오브젝트
    [SerializeField] private GameObject okBadgeRoot;        // "OK" 배지 오브젝트
    [SerializeField] private RectTransform sceneCardRect;

    [Header("카드 플립 컴포넌트 ")]
    [SerializeField] private UICardFlip cardFlip;

    [Header("관점 선택지 UI")]
    [SerializeField] private GameObject perspectiveButtonsRoot;      // 전체 선택지 묶음 루트
    [SerializeField] private Button[] perspectiveButtons;            // 각 버튼
    [SerializeField] private Text[] perspectiveTexts;                // 버튼 안에 들어갈 텍스트
    [SerializeField] private GameObject[] perspectiveSelectedMarks;  // 체크마크 등 선택 표시

    [Header("마이크 UI")]
    [SerializeField] private GameObject micButtonRoot;          // 마이크 버튼 루트
    [SerializeField] private MicRecordingIndicator micIndicator; // Indicator

    [Header("패널 전환")]
    [SerializeField] private GameObject stepRoot;               // 현재 Step3 패널 루트
    [SerializeField] private GameObject summaryPanelRoot;       // 요약 패널 루트

    [Header("완료 게이트 (요약 버튼 / 기타 숨김은 Gate에서 처리)")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("연출 옵션")]
    [SerializeField] private float flipDelay = 0.3f;    // 말하기 끝 ~ 플립 시작까지 대기 시간
    [SerializeField] private float flipDuration = 0.5f; // 카드 한 번 뒤집히는 전체 시간


    // ===== 베이스에 값 주입용 프로퍼티 구현 =====

    protected override string NgSentence => ngSentence;
    protected override IDirectorProblem2PerspectiveOption[] Perspectives => perspectives;
    protected override bool OverwriteSceneTextOnReset => overwriteSceneTextOnReset;

    protected override Text SceneText => sceneText;
    protected override GameObject NgBadgeRoot => ngBadgeRoot;
    protected override GameObject OkBadgeRoot => okBadgeRoot;
    protected override RectTransform SceneCardRect => sceneCardRect;

    protected override UICardFlip CardFlip => cardFlip;

    protected override GameObject PerspectiveButtonsRoot => perspectiveButtonsRoot;
    protected override Button[] PerspectiveButtons => perspectiveButtons;
    protected override Text[] PerspectiveTexts => perspectiveTexts;
    protected override GameObject[] PerspectiveSelectedMarks => perspectiveSelectedMarks;

    protected override GameObject MicButtonRoot => micButtonRoot;
    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject StepRoot => stepRoot;
    protected override GameObject SummaryPanelRoot => summaryPanelRoot;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float FlipDelay => flipDelay;
    protected override float FlipDuration => flipDuration;
}
