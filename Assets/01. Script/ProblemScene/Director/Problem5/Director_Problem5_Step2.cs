using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step2
/// - 인스펙터에서 장면 데이터 + UI 참조만 들고 있는 래퍼.
/// - 실제 로직은 Director_Problem5_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem5_Step2 : Director_Problem5_Step2_Logic
{
    [Serializable]
    public class SceneData : IZoomOutSceneData
    {
        [Tooltip("장면 ID (로그용)")]
        public int id = 1;

        [Header("아이콘(클로즈업) 설정")]
        [Tooltip("아이콘 위에 표시할 이모지/문자 (예: 😠)")]
        public string closeUpEmoji;

        [TextArea]
        [Tooltip("아이콘 아래에 표시할 짧은 설명 (예: '동료의 화난 표정')")]
        public string closeUpText;

        [Header("아이콘 관련 UI")]
        public Button iconButton;           // 클릭용 버튼 (카드 전체)
        public Text iconEmojiLabel;         // 아이콘 안의 이모지 Text
        public Text iconLabel;              // 아이콘 아래 라벨 Text
        public GameObject unrevealedRoot;   // 아직 클릭 안 한 상태의 비주얼
        public GameObject revealedRoot;     // 클릭 완료 후 비주얼
        public GameObject glowImage;        // 글로우 이미지 (revealed 시 비활성화)

        [Header("줌 아웃 장면 설정")]
        [Tooltip("줌 아웃 화면에 보여줄 이모지들 (예: 😤, 💻, 📊)")]
        public string[] fullSceneEmojis;

        [TextArea]
        [Tooltip("줌 아웃 화면에 표시할 전체 상황 설명")]
        public string fullSceneText;

        // ==== IZoomOutSceneData 구현 ====
        public int Id => id;
        public string CloseUpEmoji => closeUpEmoji;
        public string CloseUpText => closeUpText;
        public string[] FullSceneEmojis => fullSceneEmojis;
        public string FullSceneText => fullSceneText;
        public Button IconButton => iconButton;
        public Text IconEmojiLabel => iconEmojiLabel;
        public Text IconLabel => iconLabel;
        public GameObject UnrevealedRoot => unrevealedRoot;
        public GameObject RevealedRoot => revealedRoot;
        public GameObject GlowImage => glowImage;
    }

    [Header("장면 데이터들 (씬에서 아이콘 1:1 대응)")]
    [SerializeField] private SceneData[] scenes;

    [Header("줌 아웃 모달 UI")]
    [SerializeField] private GameObject zoomModalRoot;
    [SerializeField] private GameObject modalCloseUpRoot;
    [SerializeField] private GameObject modalFullSceneRoot;
    [SerializeField] private Text modalCloseUpEmojiLabel;
    [SerializeField] private Text modalFullSceneEmojisLabel;
    [SerializeField] private Text modalFullSceneTextLabel;

    [Header("애니메이션 타이밍")]
    [SerializeField] private float zoomDuration = 1.5f;
    [SerializeField] private float fullSceneHoldDuration = 2f;

    [Header("진행도 인디케이터 (옵션)")]
    [SerializeField] private Image[] progressDots;
    [SerializeField] private Color progressInactiveColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color progressActiveColor = new Color(1f, 0.54f, 0.24f);

    [Header("완료 게이트 (다음 스텝 진행)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ==== 베이스에 값 주입용 override ====

    protected override IZoomOutSceneData[] Scenes => scenes;

    protected override GameObject ZoomModalRoot => zoomModalRoot;
    protected override GameObject ModalCloseUpRoot => modalCloseUpRoot;
    protected override GameObject ModalFullSceneRoot => modalFullSceneRoot;
    protected override Text ModalCloseUpEmojiLabel => modalCloseUpEmojiLabel;
    protected override Text ModalFullSceneEmojisLabel => modalFullSceneEmojisLabel;
    protected override Text ModalFullSceneTextLabel => modalFullSceneTextLabel;

    protected override float ZoomDuration => zoomDuration;
    protected override float FullSceneHoldDuration => fullSceneHoldDuration;

    protected override Image[] ProgressDots => progressDots;
    protected override Color ProgressInactiveColor => progressInactiveColor;
    protected override Color ProgressActiveColor => progressActiveColor;


    protected override StepCompletionGate CompletionGate => completionGate;
}
