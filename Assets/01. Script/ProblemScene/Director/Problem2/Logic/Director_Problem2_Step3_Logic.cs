using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Step3 ���� ���� + ����ũ + ī�� �ø� ���� ���� ���̽�.
/// - ProblemStepBase ���
/// - �ڽ�(Director_Problem2_Step3)�� UI ������ SerializeField�� ���,
///   ���⼭ �䱸�ϴ� �߻� ������Ƽ�� ������ �ش�.
/// </summary>
public interface IDirectorProblem2PerspectiveOption
{
    int Id { get; }
    string Text { get; }
}
public abstract class Director_Problem2_Step3_Logic : ProblemStepBase
{
    // ==========================
    // �� ���� ���� �α� payload
    // ==========================
    [Serializable]
    private class RefilmLogPayload
    {
        public string ngText;
        public int selectedId;
        public string selectedText;
        public bool recorded;
    }

    // ==========================
    // �ڽĿ��� ������ �߻� ������Ƽ
    // ==========================

    [Header("���� ���� (�ڽĿ��� ����)")]
    protected abstract string NgSentence { get; }
    protected abstract IDirectorProblem2PerspectiveOption[] Perspectives { get; }

    [Header("�ʱ� �ؽ�Ʈ ���� �ɼ�")]
    protected abstract bool OverwriteSceneTextOnReset { get; }

    [Header("�� ī�� UI (NG / OK)")]
    protected abstract Text SceneText { get; }
    protected abstract GameObject NgBadgeRoot { get; }
    protected abstract GameObject OkBadgeRoot { get; }
    protected abstract RectTransform SceneCardRect { get; }

    [Header("ī�� �ø� ������Ʈ")]
    protected abstract CardFlip CardFlip { get; }

    [Header("���� ������ UI")]
    protected abstract GameObject PerspectiveButtonsRoot { get; }
    protected abstract Button[] PerspectiveButtons { get; }
    protected abstract Text[] PerspectiveTexts { get; }
    protected abstract GameObject[] PerspectiveSelectedMarks { get; }

    [Header("����ũ UI")]
    protected abstract GameObject MicButtonRoot { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("�г� ��ȯ")]
    protected abstract GameObject StepRoot { get; }
    protected abstract GameObject SummaryPanelRoot { get; }

    [Header("�Ϸ� ����Ʈ (��� ��ư / ��Ÿ ������ Gate���� ó��)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("���� �ɼ�")]
    protected abstract float FlipDelay { get; }
    protected abstract float FlipDuration { get; } // ���� �� ������ ����


    // ==========================
    // ���� ����
    // ==========================

    private IDirectorProblem2PerspectiveOption _selected;
    private bool _isRecording;
    private bool _hasRecordedAnswer;
    private bool _isFinished;


    // ==========================
    // ProblemStepBase ����
    // ==========================

    protected override void OnStepEnter()
    {
        Debug.Log("[Step3] OnStepEnter ȣ��");
        ResetState();

        var gate = CompletionGate;
        if (gate != null)
        {
            // Step3�� ���� �� �Ϸ�Ǹ� ���� ������ �� ī��Ʈ 1�� ���
            gate.ResetGate(1);
        }
    }

    protected override void OnStepExit()
    {
        // �ʿ��ϸ� ���� ����
    }

    // ==========================
    // �ʱ�ȭ ����
    // ==========================

    private void ResetState()
    {
        Debug.Log("[Step3] ResetState");

        _selected = null;
        _isRecording = false;
        _hasRecordedAnswer = false;
        _isFinished = false;

        var stepRoot = StepRoot;
        var summaryPanelRoot = SummaryPanelRoot;
        var sceneText = SceneText;
        var ngBadgeRoot = NgBadgeRoot;
        var okBadgeRoot = OkBadgeRoot;
        var perspectiveButtonsRoot = PerspectiveButtonsRoot;
        var micButtonRoot = MicButtonRoot;
        var perspectiveTexts = PerspectiveTexts;
        var perspectives = Perspectives;
        var perspectiveButtons = PerspectiveButtons;
        var perspectiveSelectedMarks = PerspectiveSelectedMarks;

        // Step / Summary �г� �⺻ ����
        if (stepRoot != null) stepRoot.SetActive(true);
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);

        // NG �������� ���� ����� ����
        if (sceneText != null && OverwriteSceneTextOnReset)
        {
            sceneText.text = NgSentence;
        }

        if (ngBadgeRoot != null) ngBadgeRoot.SetActive(true);
        if (okBadgeRoot != null) okBadgeRoot.SetActive(false);

        if (perspectiveButtonsRoot != null)
            perspectiveButtonsRoot.SetActive(true);

        // ����ũ�� ó���� ����
        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        // ������ �ؽ�Ʈ ����
        if (perspectiveTexts != null && perspectives != null)
        {
            int count = Mathf.Min(perspectiveTexts.Length, perspectives.Length);
            for (int i = 0; i < count; i++)
            {
                if (perspectiveTexts[i] != null && perspectives[i] != null)
                    perspectiveTexts[i].text = perspectives[i].Text;
            }
        }

        // ��ư/üũ��ũ �ʱ�ȭ
        if (perspectiveButtons != null)
        {
            foreach (var btn in perspectiveButtons)
            {
                if (btn != null)
                    btn.interactable = true;
            }
        }

        if (perspectiveSelectedMarks != null)
        {
            foreach (var mark in perspectiveSelectedMarks)
            {
                if (mark != null) mark.SetActive(false);
            }
        }
    }

    // ==========================
    // ��ư �ݹ��
    // ==========================

    /// <summary>
    /// ���� ���� ��ư���� ȣ��
    /// Button OnClick �� Director_Problem2_Step3.OnClickPerspective(�ε���)
    /// �ε��� = 0,1,2 ... : perspectives �迭�� �ε���
    /// </summary>
    public void OnClickPerspective(int optionIndex)
    {
        Debug.Log($"[Step3] OnClickPerspective ȣ��: optionIndex={optionIndex}");

        if (_isFinished)
        {
            Debug.Log("[Step3] �̹� �Ϸ� ���¶� Ŭ�� ����");
            return;
        }

        var perspectives = Perspectives;
        if (perspectives == null || perspectives.Length == 0)
        {
            Debug.LogWarning("[Step3] perspectives �迭�� �������");
            return;
        }

        if (optionIndex < 0 || optionIndex >= perspectives.Length)
        {
            Debug.LogWarning($"[Step3] optionIndex ���� ��: {optionIndex} / len={perspectives.Length}");
            return;
        }

        _selected = perspectives[optionIndex];
        Debug.Log($"[Step3] ���õ� ����: id={_selected.Id}, text={_selected.Text}");

        // ���� ǥ��
        var perspectiveSelectedMarks = PerspectiveSelectedMarks;
        if (perspectiveSelectedMarks != null)
        {
            for (int i = 0; i < perspectiveSelectedMarks.Length; i++)
            {
                if (perspectiveSelectedMarks[i] != null)
                    perspectiveSelectedMarks[i].SetActive(i == optionIndex);
            }
        }

        // ���� �� �ٸ� ��ư ��� (1ȸ ���� ����)
        var perspectiveButtons = PerspectiveButtons;
        if (perspectiveButtons != null)
        {
            for (int i = 0; i < perspectiveButtons.Length; i++)
            {
                if (perspectiveButtons[i] != null)
                    perspectiveButtons[i].interactable = (i == optionIndex);
            }
        }

        // ����ũ ��ư ����
        var micButtonRoot = MicButtonRoot;
        if (micButtonRoot != null)
        {
            micButtonRoot.SetActive(true);
            Debug.Log("[Step3] micButtonRoot Ȱ��ȭ");
        }
    }

    /// <summary>
    /// ����ũ ��ư Ŭ��
    /// - ù Ŭ��: ���� ���� (Indicator On)
    /// - �� ��° Ŭ��: ���� ����� ���� ī�� �ø� �� OK ���
    /// </summary>
    public void OnClickMic()
    {
        Debug.Log("[Step3] OnClickMic");

        if (_selected == null)
        {
            Debug.LogWarning("[Step3] ���� ���õ� ������ ����");
            return;
        }

        if (_isFinished)
        {
            Debug.Log("[Step3] �̹� �Ϸ� ����, ����ũ ����");
            return;
        }

        _isRecording = !_isRecording;

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();

        Debug.Log($"[Step3] _isRecording={_isRecording}");

        // false�� �� ���� = ���� ������ ����
        if (!_isRecording)
        {
            _hasRecordedAnswer = true;
            Debug.Log("[Step3] ���� ���� �� PlayRefilmCompleteSequence ����");
            StartCoroutine(PlayRefilmCompleteSequence());
        }
    }

    // ==========================
    // ī�� NG -> OK �ø� ����
    // ==========================

    private IEnumerator PlayRefilmCompleteSequence()
    {
        // ����ũ ��ư�� �� �̻� ������� ������ ����
        var micButtonRoot = MicButtonRoot;
        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        // ���� �������� "���̱� �ϵ�" �� �̻� Ŭ���� �� �ǰ� ó��
        var perspectiveButtonsRoot = PerspectiveButtonsRoot;
        if (perspectiveButtonsRoot != null)
            perspectiveButtonsRoot.SetActive(true);

        // ���ϱⰡ ���� �� ��� ��� �� �ø� ����
        float delay = FlipDelay;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // ��ư ����/���ͷ��� ����
        var perspectiveButtons = PerspectiveButtons;
        if (perspectiveButtons != null)
        {
            foreach (var btn in perspectiveButtons)
            {
                if (btn == null) continue;

                btn.interactable = false;

                if (btn.targetGraphic != null)
                {
                    var c = btn.targetGraphic.color;
                    c.a = 0.3f;
                    btn.targetGraphic.color = c;
                }

                var childGraphics = btn.GetComponentsInChildren<Graphic>(true);
                foreach (var g in childGraphics)
                {
                    var gc = g.color;
                    gc.a = 0.3f;
                    g.color = gc;
                }
            }
        }

        // ī�� �ø� �ڷ�ƾ ����
        var cardFlip = CardFlip;
        if (cardFlip != null)
        {
            yield return StartCoroutine(cardFlip.PlayFlipRoutine());
        }

        // OK �������� �ؽ�Ʈ ��ü
        var sceneText = SceneText;
        if (sceneText != null && _selected != null)
            sceneText.text = _selected.Text;

        var ngBadgeRoot = NgBadgeRoot;
        var okBadgeRoot = OkBadgeRoot;

        if (ngBadgeRoot != null) ngBadgeRoot.SetActive(false);
        if (okBadgeRoot != null) okBadgeRoot.SetActive(true);

        // �ø� �Ϸ� �� ���� ������
        _isFinished = true;
        Debug.Log("[Step3] �ø� �Ϸ�, _isFinished = true");

        // �Ϸ� ����: Gate�� ���Ϸ��ߴ١��� �˸�
        var gate = CompletionGate;
        if (gate != null)
        {
            gate.MarkOneDone();
        }
    }

    /// <summary>
    /// "��� ����" ��ư���� ȣ��
    /// - Attempt DB ����
    /// - �г� ��ȯ
    /// (���� ��ư GameObject�� StepCompletionGate.completeRoot�� ����)
    /// </summary>
    public void OnClickSummaryButton()
    {
        Debug.Log("[Step3] OnClickSummaryButton");

        SaveRefilmLogToDb();

        var stepRoot = StepRoot;
        var summaryPanelRoot = SummaryPanelRoot;

        if (stepRoot != null)
            stepRoot.SetActive(false);

        if (summaryPanelRoot != null)
            summaryPanelRoot.SetActive(true);
    }

    /// <summary>
    /// ProblemStepBase.SaveAttempt�� �̿��� Attempt ����
    /// </summary>
    private void SaveRefilmLogToDb()
    {
        if (_selected == null)
        {
            Debug.Log("[Director_Problem2_Step3] ���õ� ������ ���� ���� ��ŵ");
            return;
        }

        var body = new RefilmLogPayload
        {
            ngText = NgSentence,
            selectedId = _selected.Id,
            selectedText = _selected.Text,
            recorded = _hasRecordedAnswer
        };

        // Base���� context/stepKey �˻� + SaveStepAttempt ȣ��
        SaveAttempt(body);
    }
}
