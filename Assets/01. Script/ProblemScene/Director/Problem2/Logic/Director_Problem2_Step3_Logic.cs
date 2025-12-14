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
    string[] Keywords { get; }  // STT 매칭용 키워드 (null이면 Text 사용)
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
    protected abstract RectTransform SceneCardRect { get; }
    protected abstract GameObject OkSceneCard { get; }
    protected abstract Text OkSceneText { get; }

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

        // MicIndicator STT 이벤트 구독
        var indicator = MicIndicator;
        if (indicator != null)
        {
            indicator.OnKeywordMatched -= OnSTTKeywordMatched;
            indicator.OnKeywordMatched += OnSTTKeywordMatched;
            indicator.OnNoMatch -= OnSTTNoMatch;
            indicator.OnNoMatch += OnSTTNoMatch;
        }

        var gate = CompletionGate;
        if (gate != null)
        {
            // Step3�� ���� �� �Ϸ�Ǹ� ���� ������ �� ī��Ʈ 1�� ���
            gate.ResetGate(1);
        }
    }

    protected override void OnStepExit()
    {
        // MicIndicator 이벤트 구독 해제
        var indicator = MicIndicator;
        if (indicator != null)
        {
            indicator.OnKeywordMatched -= OnSTTKeywordMatched;
            indicator.OnNoMatch -= OnSTTNoMatch;
        }
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

        // ī�� ���� (NG ī�� ǥ��, OK ī�� ����)
        var sceneCardRect = SceneCardRect;
        var okSceneCard = OkSceneCard;
        if (sceneCardRect != null) sceneCardRect.gameObject.SetActive(true);
        if (okSceneCard != null) okSceneCard.SetActive(false);

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
        // STT 결과는 OnSTTKeywordMatched에서 처리
    }

    /// <summary>
    /// STT 키워드 매칭 결과 처리
    /// </summary>
    private void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Step3] OnSTTKeywordMatched: index={matchedIndex}");

        if (_isFinished)
        {
            Debug.Log("[Step3] 이미 완료 상태");
            return;
        }

        if (_selected == null)
        {
            Debug.LogWarning("[Step3] 선택된 관점이 없음");
            return;
        }

        // 선택한 관점의 인덱스 찾기
        var perspectives = Perspectives;
        int selectedIndex = -1;
        for (int i = 0; i < perspectives.Length; i++)
        {
            if (perspectives[i] == _selected)
            {
                selectedIndex = i;
                break;
            }
        }

        Debug.Log($"[Step3] 선택된 관점 인덱스: {selectedIndex}, STT 매칭 인덱스: {matchedIndex}");

        // 매칭된 인덱스가 선택한 관점과 일치하면 플립 실행
        if (matchedIndex == selectedIndex)
        {
            _hasRecordedAnswer = true;
            _isRecording = false;
            Debug.Log("[Step3] STT 매칭 성공! PlayRefilmCompleteSequence 시작");
            StartCoroutine(PlayRefilmCompleteSequence());
        }
        else
        {
            Debug.Log($"[Step3] STT 매칭 불일치 - 선택: {selectedIndex}, 인식: {matchedIndex}");
            // 필요시 다시 녹음하도록 안내
        }
    }

    /// <summary>
    /// STT 매칭 실패 처리
    /// </summary>
    private void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Step3] OnSTTNoMatch: STT 결과가 키워드와 일치하지 않습니다. 결과: {sttResult}");
        // 매칭 실패 시 플립하지 않음 - 사용자가 다시 녹음할 수 있음
        _isRecording = false;
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

        // OK ī�忡 ���õ� ������ �ؽ�Ʈ ����
        var okSceneText = OkSceneText;
        if (okSceneText != null && _selected != null)
            okSceneText.text = _selected.Text;

        // ī�� ��ü: NG ī�� ���� OK ī�� Ŵ
        var sceneCardRect = SceneCardRect;
        var okSceneCard = OkSceneCard;

        if (sceneCardRect != null) sceneCardRect.gameObject.SetActive(false);
        if (okSceneCard != null) okSceneCard.SetActive(true);

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
