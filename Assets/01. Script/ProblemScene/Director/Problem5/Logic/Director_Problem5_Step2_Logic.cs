using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem5 Step2 장면 데이터 인터페이스
/// - 각 아이콘 클릭 시 클로즈업 팝업 → 풀씬 팝업 순서로 표시
/// </summary>
public interface IZoomOutSceneData
{
    int Id { get; }

    // 아이콘 버튼
    Button IconButton { get; }
    GameObject UnrevealedRoot { get; }
    GameObject RevealedRoot { get; }
    GameObject GlowImage { get; }

    // 팝업 이미지들
    PopupImageDisplay CloseUpPopup { get; }
    PopupImageDisplay FullScenePopup { get; }
}

/// <summary>
/// Director / Problem5 / Step2 로직 베이스
/// - 여러 장면 아이콘 클릭 → 클로즈업 팝업 → 풀씬 팝업 순서로 표시
/// - 모든 장면을 다 보면 StepCompletionGate 완료
/// </summary>
/// 

public abstract class Director_Problem5_Step2_Logic : ProblemStepBase
{
    // ==== 자식에서 제공할 추상 프로퍼티 ====

    protected abstract IZoomOutSceneData[] Scenes { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    // ==== 내부 상태 ====

    private bool[] _revealedFlags;
    private int _revealedCount;
    private int _currentSceneIndex = -1;
    private bool _isAnimating;
    private Coroutine _sequenceRoutine;

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
        _currentSceneIndex = -1;
        _isAnimating = false;

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
            if (scene.GlowImage != null)
                scene.GlowImage.SetActive(true);

            // 팝업 초기화
            if (scene.CloseUpPopup != null)
                scene.CloseUpPopup.ResetToInitial();
            if (scene.FullScenePopup != null)
                scene.FullScenePopup.ResetToInitial();
        }

        // Gate 초기화
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        // 모든 팝업 숨기기
        var scenes = Scenes;
        if (scenes != null)
        {
            foreach (var scene in scenes)
            {
                if (scene.CloseUpPopup != null)
                    scene.CloseUpPopup.HideImmediate();
                if (scene.FullScenePopup != null)
                    scene.FullScenePopup.HideImmediate();
            }
        }
    }

    // ======================================================
    // 장면 클릭 처리
    // ======================================================

    public void OnClickScene(int index)
    {
        if (_isAnimating) return;

        var scenes = Scenes;
        if (scenes == null || index < 0 || index >= scenes.Length) return;

        // 이미 본 장면이면 무시
        if (_revealedFlags != null && index < _revealedFlags.Length && _revealedFlags[index])
            return;

        _currentSceneIndex = index;

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        _sequenceRoutine = StartCoroutine(PopupSequenceCoroutine(index));
    }

    private IEnumerator PopupSequenceCoroutine(int index)
    {
        _isAnimating = true;

        var scenes = Scenes;
        var scene = scenes[index];

        // 1) 클로즈업 팝업 표시 (자동 숨김됨)
        bool closeUpDone = false;
        if (scene.CloseUpPopup != null)
        {
            scene.CloseUpPopup.Show(() => closeUpDone = true);

            // 클로즈업 완료 대기
            while (!closeUpDone)
                yield return null;
        }

        // 2) 풀씬 팝업 표시 (자동 숨김됨)
        bool fullSceneDone = false;
        if (scene.FullScenePopup != null)
        {
            scene.FullScenePopup.Show(() => fullSceneDone = true);

            // 풀씬 완료 대기
            while (!fullSceneDone)
                yield return null;
        }

        // 3) 장면 "확인 완료" 상태로 표시
        if (_revealedFlags != null && index < _revealedFlags.Length)
            _revealedFlags[index] = true;

        _revealedCount++;

        // 아이콘 비주얼 전환
        if (scene.UnrevealedRoot != null)
            scene.UnrevealedRoot.SetActive(false);
        if (scene.RevealedRoot != null)
            scene.RevealedRoot.SetActive(true);
        if (scene.GlowImage != null)
            scene.GlowImage.SetActive(false);

        _isAnimating = false;
        _currentSceneIndex = -1;
        _sequenceRoutine = null;

        // 4) 모든 장면을 다 봤다면 Gate 완료 처리
        var allScenes = Scenes;
        if (allScenes != null && _revealedCount >= allScenes.Length)
        {
            var gate = CompletionGate;
            if (gate != null)
                gate.MarkOneDone();
        }
    }
}
