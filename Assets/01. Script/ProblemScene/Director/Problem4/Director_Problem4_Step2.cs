using DA_Assets.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem4_Step2 : Director_Problem4_Step2_Logic
{
    [Serializable]
    public class FilmCutData : IFilmCutData
    {
        [Tooltip("컷 ID")]
        public string cutID;

        [TextArea]
        [Tooltip("화면에 표시할 컷 문장")]
        public string text;

        [Tooltip("생각 컷이면 true, 사실이면 false")]
        public bool isThinking;

        string IFilmCutData.CutId => cutID;
        string IFilmCutData.Text => text;
        bool IFilmCutData.IsThinking => isThinking;
    }

    [Header("컷 데이터")]
    [SerializeField] private FilmCutData[] filmCuts;

    [Header("필름 카드 UI")]
    [SerializeField] private GameObject filmCardRoot;
    [SerializeField] private Text filmSentenceLabel;
    [SerializeField] private Text filmIndexLabel;

    [Header("오류 메세지 UI")]
    [SerializeField] private GameObject errorRoot;
    [SerializeField] private Text errorLabel;
    [SerializeField] private string defaultErrorMessage = "다시 생각해보세요!";

    [Header("컬러 복원 연출용 UI")]
    [SerializeField] private GameObject colorRestoreRoot;
    [SerializeField] private GameObject beforeColorRoot;

    [Header("하단 버튼")]
    [SerializeField] private Button cutBtn;
    [SerializeField] private Button passBtn;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    [Header("오류 메시지 유지 시간")]
    [SerializeField] private float errorShowDuration = 1f;

    [Header("카드 위치/등장 연출")]
    [SerializeField] private RectTransform filmCardRect;
    [SerializeField] private CanvasGroup filmCardCanvasGroup;
    [SerializeField] private RectTransform filmAppearStart;
    [SerializeField] private float appearDuration = 0.4f;

    [Header("PASS 연출 (통과 카드 이동 위치)")]
    [SerializeField] private RectTransform passTargetRect;
    [SerializeField] private float passMoveDuration = 0.5f;

    [Header("가위 연출")]
    [SerializeField] private RectTransform scissorsRect;
    [SerializeField] private float scissorsMoveDuration = 0.4f;
    [SerializeField] private Vector2 scissorsOffsetFromCard = new Vector2(0f, 150f);

    [Header("분할 카드 연출")]
    [SerializeField] private RectTransform cardLeftRect;
    [SerializeField] private RectTransform cardRightRect;
    [SerializeField] private CanvasGroup cardLeftCanvas;
    [SerializeField] private CanvasGroup cardRightCanvas;
    [SerializeField] private float splitDuration = 0.6f;
    [SerializeField] private float splitHorizontalOffset = 120f;
    [SerializeField] private float splitFallDistance = 200f;
    [SerializeField] private float splitRotateAngle = 18f;

    // ====== 베이스 주입용 override 프로퍼티 ======

    protected override IFilmCutData[] FilmCuts => filmCuts;

    protected override GameObject FilmCardRoot => filmCardRoot;
    protected override Text FilmSentenceLabel => filmSentenceLabel;
    protected override Text FilmIndexLabel => filmIndexLabel;

    protected override GameObject ErrorRoot => errorRoot;
    protected override Text ErrorLabel => errorLabel;
    protected override string DefaultErrorMessage => defaultErrorMessage;

    protected override GameObject ColorRestoreRoot => colorRestoreRoot;
    protected override GameObject BeforeColorRoot => beforeColorRoot;

    protected override Button CutBtn => cutBtn;
    protected override Button PassBtn => passBtn;

    protected override StepCompletionGate StepCompletionGate => stepCompletionGate;

    protected override float ErrorShowDuration => errorShowDuration;

    protected override RectTransform FilmCardRect => filmCardRect;
    protected override CanvasGroup FilmCardCanvasGroup => filmCardCanvasGroup;
    protected override RectTransform FilmAppearStart => filmAppearStart;
    protected override float AppearDuration => appearDuration;

    protected override RectTransform PassTargetRect => passTargetRect;
    protected override float PassMoveDuration => passMoveDuration;

    protected override RectTransform ScissorsRect => scissorsRect;
    protected override float ScissorsMoveDuration => scissorsMoveDuration;
    protected override Vector2 ScissorsOffsetFromCard => scissorsOffsetFromCard;

    protected override RectTransform CardLeftRect => cardLeftRect;
    protected override RectTransform CardRightRect => cardRightRect;
    protected override CanvasGroup CardLeftCanvas => cardLeftCanvas;
    protected override CanvasGroup CardRightCanvas => cardRightCanvas;
    protected override float SplitDuration => splitDuration;
    protected override float SplitHorizontalOffset => splitHorizontalOffset;
    protected override float SplitFallDistance => splitFallDistance;
    protected override float SplitRotateAngle => splitRotateAngle;
}
