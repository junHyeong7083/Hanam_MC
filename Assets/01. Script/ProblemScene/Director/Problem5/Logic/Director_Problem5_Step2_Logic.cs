using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem5 Step2 장면 데이터 인터페이스.
/// 각 장면의 아이콘 버튼, 리빌 전/후 비주얼, 텍스트 정보를 정의한다.
/// </summary>
public interface IZoomOutSceneData
{
    int Id { get; }
    Button IconButton { get; }
    GameObject UnrevealedRoot { get; }
    GameObject RevealedRoot { get; }

    // 텍스트 (CSV textId 기반)
    int UnrevealedTextId { get; }
    int RevealedTextId { get; }
    Text UnrevealedText { get; }
    Text RevealedText { get; }
}

/// <summary>
/// Director_Problem5_Step2_Logic - 문제5 스텝2의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 여러 장면 아이콘을 배치하고, 사용자가 각 아이콘을 클릭하면
///         "확인 전(Unrevealed)" → "확인 완료(Revealed)" 상태로 전환한다.
///         모든 장면을 다 확인하면 StepCompletionGate로 완료 처리한다.
///         "줌아웃(ZoomOut)" 컨셉의 장면 탐색 활동이다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측.
/// 【문제/스텝】 Director 테마 / 문제5 / 스텝2 (메인 활동 - 장면 아이콘 탐색)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem5_Step2 (Binder 자식 클래스)
/// 【참조되는 곳】 IZoomOutSceneData (장면 데이터 인터페이스),
///               DialogueSequencer (대사), StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → 장면 초기화 → enter 대사 → 아이콘 클릭 →
///         Unrevealed→Revealed 전환 → 모든 장면 확인 → completed 대사 → 다음 스텝
/// </summary>
public abstract class Director_Problem5_Step2_Logic : ProblemStepBase
{
    // ==== 자식에서 제공할 추상 프로퍼티 ====

    /// <summary>장면 데이터 배열 (자식에서 SerializeField로 바인딩)</summary>
    protected abstract IZoomOutSceneData[] Scenes { get; }

    /// <summary>완료 게이트 - 모든 장면 확인 시 다음 스텝 진행</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;  // 대사 시퀀서

    // ==== 내부 상태 ====

    /// <summary>대사 재생 중 상호작용 잠금 플래그</summary>
    private bool _interactionLocked = true;

    /// <summary>각 장면의 확인 완료 여부 배열</summary>
    private bool[] _revealedFlags;

    /// <summary>확인 완료된 장면 수</summary>
    private int _revealedCount;

    // ======================================================
    // ProblemStepBase Hooks
    // ======================================================

    protected override void OnStepEnter()
    {
        var scenes = Scenes;
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogWarning("[Problem5_Step2] scenes 데이터가 비어있음");
            return;
        }

        int count = scenes.Length;
        _revealedFlags = new bool[count];
        _revealedCount = 0;

        // 각 장면 초기화
        for (int i = 0; i < scenes.Length; i++)
        {
            int capturedIndex = i;
            var scene = scenes[i];

            // 버튼 리스너 설정
            var btn = scene.IconButton;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickScene(capturedIndex));
            }

            // 비주얼 초기화
            if (scene.UnrevealedRoot != null)
                scene.UnrevealedRoot.SetActive(true);
            if (scene.RevealedRoot != null)
                scene.RevealedRoot.SetActive(false);

            // 텍스트 초기화 (CSV)
            if (scene.UnrevealedText != null && scene.UnrevealedTextId > 0)
                scene.UnrevealedText.text = ProblemRuntime.L(scene.UnrevealedTextId);
            if (scene.RevealedText != null && scene.RevealedTextId > 0)
                scene.RevealedText.text = ProblemRuntime.L(scene.RevealedTextId);
        }

        // Gate 초기화
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
        Debug.Log("[Problem5_Step2] OnDialogueEnterComplete → _interactionLocked = false");
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;
    }

    // ======================================================
    // 장면 클릭 처리
    // ======================================================

    /// <summary>
    /// 장면 아이콘 클릭 핸들러. 해당 장면을 Revealed 상태로 전환하고,
    /// 모든 장면 확인 시 완료 게이트 + completed 대사를 처리한다.
    /// </summary>
    /// <param name="index">클릭된 장면 인덱스</param>
    public void OnClickScene(int index)
    {
        Debug.Log($"[Problem5_Step2] OnClickScene({index}) called, _interactionLocked={_interactionLocked}");
        if (_interactionLocked) return;
        var scenes = Scenes;
        if (scenes == null || index < 0 || index >= scenes.Length) return;

        // 이미 본 장면이면 무시
        if (_revealedFlags != null && index < _revealedFlags.Length && _revealedFlags[index])
            return;

        var scene = scenes[index];

        // 장면 "확인 완료" 상태로 표시
        if (_revealedFlags != null && index < _revealedFlags.Length)
            _revealedFlags[index] = true;

        _revealedCount++;

        // 아이콘 비주얼 전환
        if (scene.UnrevealedRoot != null)
            scene.UnrevealedRoot.SetActive(false);
        if (scene.RevealedRoot != null)
            scene.RevealedRoot.SetActive(true);

        // 모든 장면을 다 봤다면 완료 처리
        var allScenes = Scenes;
        if (allScenes != null && _revealedCount >= allScenes.Length)
        {
            var gate = CompletionGate;
            if (gate != null)
                gate.MarkOneDone();

            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();
        }
    }
}
