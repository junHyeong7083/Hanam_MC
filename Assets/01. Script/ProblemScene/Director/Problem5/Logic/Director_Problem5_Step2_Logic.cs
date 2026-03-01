using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem5 Step2 장면 데이터 인터페이스
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
/// Director / Problem5 / Step2 로직 베이스
/// - 여러 장면 아이콘 클릭 → Reveal 전환
/// - 모든 장면을 다 보면 StepCompletionGate 완료
/// </summary>

public abstract class Director_Problem5_Step2_Logic : ProblemStepBase
{
    // ==== 자식에서 제공할 추상 프로퍼티 ====

    protected abstract IZoomOutSceneData[] Scenes { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("하남 박스")]
    protected abstract Text HanamText { get; }
    protected abstract int GuideTextId { get; }
    protected abstract int CompletionTextId { get; }

    // ==== 내부 상태 ====

    private bool[] _revealedFlags;
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

        // 가이드 텍스트
        if (HanamText != null && GuideTextId > 0)
            HanamText.text = ProblemRuntime.L(GuideTextId);

        // Gate 초기화
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
    }

    // ======================================================
    // 장면 클릭 처리
    // ======================================================

    public void OnClickScene(int index)
    {
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
            // 완료 텍스트
            if (HanamText != null && CompletionTextId > 0)
                HanamText.text = ProblemRuntime.L(CompletionTextId);

            var gate = CompletionGate;
            if (gate != null)
                gate.MarkOneDone();
        }
    }
}
