using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step2 로직 베이스.
/// - 여러 오해 장면 아이콘을 터치해서 "줌 아웃"해 보는 스텝.
/// - 모든 장면을 한 번씩 열람하면 StepCompletionGate를 통해
///   completeRoot(CTA 버튼 루트)를 켜준다.
/// </summary>
public interface IZoomOutSceneData
{
    int Id { get; }
    string CloseUpEmoji { get; }
    string CloseUpText { get; }
    string[] FullSceneEmojis { get; }
    string FullSceneText { get; }

    Button IconButton { get; }
    Text IconEmojiLabel { get; }
    Text IconLabel { get; }
    GameObject UnrevealedRoot { get; }
    GameObject RevealedRoot { get; }
}

public abstract class Director_Problem5_Step2_Logic : ProblemStepBase
{
    // ==== 자식에서 주입할 추상 프로퍼티 ====

    [Header("장면 데이터들 (자식 주입)")]
    protected abstract IZoomOutSceneData[] Scenes { get; }

    [Header("줌 아웃 모달 UI")]
    protected abstract GameObject ZoomModalRoot { get; }
    protected abstract GameObject ModalCloseUpRoot { get; }
    protected abstract GameObject ModalFullSceneRoot { get; }
    protected abstract Text ModalCloseUpEmojiLabel { get; }
    protected abstract Text ModalFullSceneEmojisLabel { get; }
    protected abstract Text ModalFullSceneTextLabel { get; }

    [Header("애니메이션 타이밍")]
    protected abstract float ZoomDuration { get; }
    protected abstract float FullSceneHoldDuration { get; }

    [Header("진행도 인디케이터 (옵션)")]
    protected abstract Image[] ProgressDots { get; }
    protected abstract Color ProgressInactiveColor { get; }
    protected abstract Color ProgressActiveColor { get; }

    [Header("완료 게이트 ")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // ==== 내부 상태 ====

    private bool[] _revealedFlags;
    private int _revealedCount;
    private int _currentSceneIndex = -1;
    private bool _isAnimating;
    private Coroutine _zoomRoutine;

    // ======================================================
    // ProblemStepBase Hooks
    // ======================================================

    protected override void OnStepEnter()
    {

        var scenes = Scenes;
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogWarning("[Problem5_Step2] scenes 데이터가 비어 있음");
            return;
        }

        int count = scenes.Length;
        _revealedFlags = new bool[count];
        _revealedCount = 0;
        _currentSceneIndex = -1;
        _isAnimating = false;

        // 모달 초기화
        if (ZoomModalRoot != null) ZoomModalRoot.SetActive(false);
        if (ModalCloseUpRoot != null) ModalCloseUpRoot.SetActive(false);
        if (ModalFullSceneRoot != null) ModalFullSceneRoot.SetActive(false);

        // 아이콘 버튼 리스너 연결 + 초기 상태 세팅
        for (int i = 0; i < scenes.Length; i++)
        {
            int capturedIndex = i;
            var scene = scenes[i];

            var btn = scene.IconButton;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickScene(capturedIndex));
            }

            if (scene.IconEmojiLabel != null)
                scene.IconEmojiLabel.text = scene.CloseUpEmoji;

            if (scene.IconLabel != null)
                scene.IconLabel.text = scene.CloseUpText;

            if (scene.UnrevealedRoot != null)
                scene.UnrevealedRoot.SetActive(true);
            if (scene.RevealedRoot != null)
                scene.RevealedRoot.SetActive(false);
        }

        UpdateProgressDots();

        // Gate: 이 스텝에서 "모든 장면 다 봤다" = 1칸 채우는 구조
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_zoomRoutine != null)
        {
            StopCoroutine(_zoomRoutine);
            _zoomRoutine = null;
        }
    }

    // ======================================================
    // 아이콘 클릭 처리
    // ======================================================

    public void OnClickScene(int index)
    {
        if (_isAnimating) return;

        var scenes = Scenes;
        if (scenes == null || index < 0 || index >= scenes.Length) return;

        if (_revealedFlags != null && index < _revealedFlags.Length && _revealedFlags[index])
            return; // 이미 본 장면

        _currentSceneIndex = index;

        if (_zoomRoutine != null)
        {
            StopCoroutine(_zoomRoutine);
            _zoomRoutine = null;
        }

        _zoomRoutine = StartCoroutine(ZoomSequenceCoroutine(index));
    }

    private IEnumerator ZoomSequenceCoroutine(int index)
    {
        _isAnimating = true;

        var scenes = Scenes;
        var scene = scenes[index];

        // 1) 모달 켜고 클로즈업 상태로 세팅
        if (ZoomModalRoot != null) ZoomModalRoot.SetActive(true);

        if (ModalCloseUpRoot != null) ModalCloseUpRoot.SetActive(true);
        if (ModalFullSceneRoot != null) ModalFullSceneRoot.SetActive(false);

        if (ModalCloseUpEmojiLabel != null)
            ModalCloseUpEmojiLabel.text = scene.CloseUpEmoji;

        // 2) 줌(클로즈업) 상태 유지
        if (ZoomDuration > 0f)
            yield return new WaitForSeconds(ZoomDuration);

        // 3) 전체 상황 화면으로 전환
        if (ModalCloseUpRoot != null) ModalCloseUpRoot.SetActive(false);
        if (ModalFullSceneRoot != null) ModalFullSceneRoot.SetActive(true);

        if (ModalFullSceneEmojisLabel != null)
        {
            var emojis = scene.FullSceneEmojis;
            if (emojis != null && emojis.Length > 0)
                ModalFullSceneEmojisLabel.text = string.Join(" ", emojis);
            else
                ModalFullSceneEmojisLabel.text = string.Empty;
        }

        if (ModalFullSceneTextLabel != null)
            ModalFullSceneTextLabel.text = scene.FullSceneText;

        if (FullSceneHoldDuration > 0f)
            yield return new WaitForSeconds(FullSceneHoldDuration);

        // 4) 모달 닫기
        if (ZoomModalRoot != null) ZoomModalRoot.SetActive(false);

        // 5) 장면을 "열람 완료" 상태로 표시
        if (_revealedFlags != null && index < _revealedFlags.Length)
            _revealedFlags[index] = true;

        _revealedCount++;

        // 아이콘 비주얼 갱신
        if (scene.UnrevealedRoot != null)
            scene.UnrevealedRoot.SetActive(false);
        if (scene.RevealedRoot != null)
            scene.RevealedRoot.SetActive(true);

        UpdateProgressDots();

        _isAnimating = false;
        _currentSceneIndex = -1;
        _zoomRoutine = null;

        // 6) 모든 장면을 다 봤다면  Gate 완료 처리
        var allScenes = Scenes;
        if (allScenes != null && _revealedCount >= allScenes.Length)
        {
            var gate = CompletionGate;
            if (gate != null)
            {
                gate.MarkOneDone();   
            }
        }
    }

    // ======================================================
    // 진행도 처리
    // ======================================================

    private void UpdateProgressDots()
    {
        var dots = ProgressDots;
        if (dots == null || dots.Length == 0)
            return;

        for (int i = 0; i < dots.Length; i++)
        {
            var img = dots[i];
            if (img == null) continue;

            bool active = (_revealedFlags != null &&
                           i >= 0 && i < _revealedFlags.Length &&
                           _revealedFlags[i]);

            img.color = active ? ProgressActiveColor : ProgressInactiveColor;
        }
    }
}
