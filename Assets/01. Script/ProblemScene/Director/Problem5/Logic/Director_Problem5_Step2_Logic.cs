using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step2 ���� ���̽�.
/// - ���� ���� ��� �������� ��ġ�ؼ� "�� �ƿ�"�� ���� ����.
/// - ��� ����� �� ���� �����ϸ� StepCompletionGate�� ����
///   completeRoot(CTA ��ư ��Ʈ)�� ���ش�.
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
    GameObject GlowImage { get; }
}

public abstract class Director_Problem5_Step2_Logic : ProblemStepBase
{
    // ==== �ڽĿ��� ������ �߻� ������Ƽ ====

    [Header("��� �����͵� (�ڽ� ����)")]
    protected abstract IZoomOutSceneData[] Scenes { get; }

    [Header("�� �ƿ� ��� UI")]
    protected abstract GameObject ZoomModalRoot { get; }
    protected abstract GameObject ModalCloseUpRoot { get; }
    protected abstract GameObject ModalFullSceneRoot { get; }
    protected abstract Text ModalCloseUpEmojiLabel { get; }
    protected abstract Text ModalFullSceneEmojisLabel { get; }
    protected abstract Text ModalFullSceneTextLabel { get; }

    [Header("�ִϸ��̼� Ÿ�̹�")]
    protected abstract float ZoomDuration { get; }
    protected abstract float FullSceneHoldDuration { get; }

    [Header("���൵ �ε������� (�ɼ�)")]
    protected abstract Image[] ProgressDots { get; }
    protected abstract Color ProgressInactiveColor { get; }
    protected abstract Color ProgressActiveColor { get; }

    [Header("�Ϸ� ����Ʈ ")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // ==== ���� ���� ====

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
            Debug.LogWarning("[Problem5_Step2] scenes �����Ͱ� ��� ����");
            return;
        }

        int count = scenes.Length;
        _revealedFlags = new bool[count];
        _revealedCount = 0;
        _currentSceneIndex = -1;
        _isAnimating = false;

        // ��� �ʱ�ȭ
        if (ZoomModalRoot != null) ZoomModalRoot.SetActive(false);
        if (ModalCloseUpRoot != null) ModalCloseUpRoot.SetActive(false);
        if (ModalFullSceneRoot != null) ModalFullSceneRoot.SetActive(false);

        // ������ ��ư ������ ���� + �ʱ� ���� ����
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

        // Gate: �� ���ܿ��� "��� ��� �� �ô�" = 1ĭ ä��� ����
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
    // ������ Ŭ�� ó��
    // ======================================================

    public void OnClickScene(int index)
    {
        if (_isAnimating) return;

        var scenes = Scenes;
        if (scenes == null || index < 0 || index >= scenes.Length) return;

        if (_revealedFlags != null && index < _revealedFlags.Length && _revealedFlags[index])
            return; // �̹� �� ���

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

        // 1) ��� �Ѱ� Ŭ����� ���·� ����
        if (ZoomModalRoot != null) ZoomModalRoot.SetActive(true);

        if (ModalCloseUpRoot != null) ModalCloseUpRoot.SetActive(true);
        if (ModalFullSceneRoot != null) ModalFullSceneRoot.SetActive(false);

        if (ModalCloseUpEmojiLabel != null)
            ModalCloseUpEmojiLabel.text = scene.CloseUpEmoji;

        // 2) ��(Ŭ�����) ���� ����
        if (ZoomDuration > 0f)
            yield return new WaitForSeconds(ZoomDuration);

        // 3) ��ü ��Ȳ ȭ������ ��ȯ
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

        // 4) ��� �ݱ�
        if (ZoomModalRoot != null) ZoomModalRoot.SetActive(false);

        // 5) ����� "���� �Ϸ�" ���·� ǥ��
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

        UpdateProgressDots();

        _isAnimating = false;
        _currentSceneIndex = -1;
        _zoomRoutine = null;

        // 6) ��� ����� �� �ôٸ�  Gate �Ϸ� ó��
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
    // ���൵ ó��
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
