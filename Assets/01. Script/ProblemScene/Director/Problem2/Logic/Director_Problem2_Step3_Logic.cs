using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Step3 관점 선택 + 마이크 + 카드 플립 공통 로직 베이스.
/// - ProblemStepBase 상속
/// - 자식(Director_Problem2_Step3)은 UI 참조만 SerializeField로 들고,
///   여기서 요구하는 추상 프로퍼티로 주입해 준다.
/// </summary>
public interface IDirectorProblem2PerspectiveOption
{
    int Id { get; }
    string Text { get; }
}

public abstract class Director_Problem2_Step3_Logic : ProblemStepBase
{
    // ==========================
    // 자식에서 주입할 추상 프로퍼티
    // ==========================

    [Header("문구 설정 (자식에서 주입)")]
    protected abstract string NgSentence { get; }
    protected abstract IDirectorProblem2PerspectiveOption[] Perspectives { get; }

    [Header("초기 텍스트 설정 옵션")]
    protected abstract bool OverwriteSceneTextOnReset { get; }

    [Header("씬 카드 UI (NG / OK)")]
    protected abstract Text SceneText { get; }
    protected abstract GameObject NgBadgeRoot { get; }
    protected abstract GameObject OkBadgeRoot { get; }
    protected abstract RectTransform SceneCardRect { get; }

    [Header("카드 플립 컴포넌트")]
    protected abstract UICardFlip CardFlip { get; }

    [Header("관점 선택지 UI")]
    protected abstract GameObject PerspectiveButtonsRoot { get; }
    protected abstract Button[] PerspectiveButtons { get; }
    protected abstract Text[] PerspectiveTexts { get; }
    protected abstract GameObject[] PerspectiveSelectedMarks { get; }

    [Header("마이크 UI")]
    protected abstract GameObject MicButtonRoot { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("패널 전환")]
    protected abstract GameObject StepRoot { get; }
    protected abstract GameObject SummaryPanelRoot { get; }

    [Header("완료 게이트 (요약 버튼 / 기타 숨김은 Gate에서 처리)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("연출 옵션")]
    protected abstract float FlipDelay { get; }
    protected abstract float FlipDuration { get; } // 아직 안 쓰더라도 보존


    // ==========================
    // 내부 상태
    // ==========================

    private IDirectorProblem2PerspectiveOption _selected;
    private bool _isRecording;
    private bool _hasRecordedAnswer;
    private bool _isFinished;


    // ==========================
    // ProblemStepBase 구현
    // ==========================

    protected override void OnStepEnter()
    {
        Debug.Log("[Step3] OnStepEnter 호출");
        ResetState();

        var gate = CompletionGate;
        if (gate != null)
        {
            // Step3는 “한 번 완료되면 끝” 구조라서 총 카운트 1로 사용
            gate.ResetGate(1);
        }
    }

    protected override void OnStepExit()
    {
        // 필요하면 상태 정리
    }

    // ==========================
    // 초기화 로직
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

        // Step / Summary 패널 기본 상태
        if (stepRoot != null) stepRoot.SetActive(true);
        if (summaryPanelRoot != null) summaryPanelRoot.SetActive(false);

        // NG 문장으로 강제 덮어쓸지 여부
        if (sceneText != null && OverwriteSceneTextOnReset)
        {
            sceneText.text = NgSentence;
        }

        if (ngBadgeRoot != null) ngBadgeRoot.SetActive(true);
        if (okBadgeRoot != null) okBadgeRoot.SetActive(false);

        if (perspectiveButtonsRoot != null)
            perspectiveButtonsRoot.SetActive(true);

        // 마이크는 처음엔 숨김
        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        // 선택지 텍스트 세팅
        if (perspectiveTexts != null && perspectives != null)
        {
            int count = Mathf.Min(perspectiveTexts.Length, perspectives.Length);
            for (int i = 0; i < count; i++)
            {
                if (perspectiveTexts[i] != null && perspectives[i] != null)
                    perspectiveTexts[i].text = perspectives[i].Text;
            }
        }

        // 버튼/체크마크 초기화
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
    // 버튼 콜백들
    // ==========================

    /// <summary>
    /// 관점 선택 버튼에서 호출
    /// Button OnClick → Director_Problem2_Step3.OnClickPerspective(인덱스)
    /// 인덱스 = 0,1,2 ... : perspectives 배열의 인덱스
    /// </summary>
    public void OnClickPerspective(int optionIndex)
    {
        Debug.Log($"[Step3] OnClickPerspective 호출: optionIndex={optionIndex}");

        if (_isFinished)
        {
            Debug.Log("[Step3] 이미 완료 상태라 클릭 무시");
            return;
        }

        var perspectives = Perspectives;
        if (perspectives == null || perspectives.Length == 0)
        {
            Debug.LogWarning("[Step3] perspectives 배열이 비어있음");
            return;
        }

        if (optionIndex < 0 || optionIndex >= perspectives.Length)
        {
            Debug.LogWarning($"[Step3] optionIndex 범위 밖: {optionIndex} / len={perspectives.Length}");
            return;
        }

        _selected = perspectives[optionIndex];
        Debug.Log($"[Step3] 선택된 관점: id={_selected.Id}, text={_selected.Text}");

        // 선택 표시
        var perspectiveSelectedMarks = PerspectiveSelectedMarks;
        if (perspectiveSelectedMarks != null)
        {
            for (int i = 0; i < perspectiveSelectedMarks.Length; i++)
            {
                if (perspectiveSelectedMarks[i] != null)
                    perspectiveSelectedMarks[i].SetActive(i == optionIndex);
            }
        }

        // 선택 후 다른 버튼 잠금 (1회 선택 구조)
        var perspectiveButtons = PerspectiveButtons;
        if (perspectiveButtons != null)
        {
            for (int i = 0; i < perspectiveButtons.Length; i++)
            {
                if (perspectiveButtons[i] != null)
                    perspectiveButtons[i].interactable = (i == optionIndex);
            }
        }

        // 마이크 버튼 노출
        var micButtonRoot = MicButtonRoot;
        if (micButtonRoot != null)
        {
            micButtonRoot.SetActive(true);
            Debug.Log("[Step3] micButtonRoot 활성화");
        }
    }

    /// <summary>
    /// 마이크 버튼 클릭
    /// - 첫 클릭: 녹음 시작 (Indicator On)
    /// - 두 번째 클릭: 녹음 종료로 보고 카드 플립 → OK 장면
    /// </summary>
    public void OnClickMic()
    {
        Debug.Log("[Step3] OnClickMic");

        if (_selected == null)
        {
            Debug.LogWarning("[Step3] 아직 선택된 관점이 없음");
            return;
        }

        if (_isFinished)
        {
            Debug.Log("[Step3] 이미 완료 상태, 마이크 무시");
            return;
        }

        _isRecording = !_isRecording;

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();

        Debug.Log($"[Step3] _isRecording={_isRecording}");

        // false가 된 시점 = 녹음 종료라고 간주
        if (!_isRecording)
        {
            _hasRecordedAnswer = true;
            Debug.Log("[Step3] 녹음 종료 → PlayRefilmCompleteSequence 시작");
            StartCoroutine(PlayRefilmCompleteSequence());
        }
    }

    // ==========================
    // 카드 NG -> OK 플립 연출
    // ==========================

    private IEnumerator PlayRefilmCompleteSequence()
    {
        // 마이크 버튼은 더 이상 사용하지 않으니 숨김
        var micButtonRoot = MicButtonRoot;
        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        // 관점 선택지는 "보이긴 하되" 더 이상 클릭은 안 되게 처리
        var perspectiveButtonsRoot = PerspectiveButtonsRoot;
        if (perspectiveButtonsRoot != null)
            perspectiveButtonsRoot.SetActive(true);

        // 말하기가 끝난 뒤 잠시 대기 후 플립 시작
        float delay = FlipDelay;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 버튼 알파/인터랙션 정리
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

        // 카드 플립 코루틴 실행
        var cardFlip = CardFlip;
        if (cardFlip != null)
        {
            yield return StartCoroutine(cardFlip.PlayFlipRoutine());
        }

        // OK 문장으로 텍스트 교체
        var sceneText = SceneText;
        if (sceneText != null && _selected != null)
            sceneText.text = _selected.Text;

        var ngBadgeRoot = NgBadgeRoot;
        var okBadgeRoot = OkBadgeRoot;

        if (ngBadgeRoot != null) ngBadgeRoot.SetActive(false);
        if (okBadgeRoot != null) okBadgeRoot.SetActive(true);

        // 플립 완료 후 상태 마무리
        _isFinished = true;
        Debug.Log("[Step3] 플립 완료, _isFinished = true");

        // 완료 연출: Gate에 “완료했다”만 알림
        var gate = CompletionGate;
        if (gate != null)
        {
            gate.MarkOneDone();
        }
    }

    /// <summary>
    /// "요약 보기" 버튼에서 호출
    /// - Attempt DB 저장
    /// - 패널 전환
    /// (실제 버튼 GameObject는 StepCompletionGate.completeRoot로 관리)
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
    /// ProblemStepBase.SaveAttempt를 이용해 Attempt 저장
    /// </summary>
    private void SaveRefilmLogToDb()
    {
        if (_selected == null)
        {
            Debug.Log("[Director_Problem2_Step3] 선택된 관점이 없어 저장 스킵");
            return;
        }

        var body = new
        {
            ngText = NgSentence,
            selectedId = _selected.Id,
            selectedText = _selected.Text,
            // recorded = _hasRecordedAnswer
        };

        // Base에서 context/stepKey 검사 + SaveStepAttempt 호출
        SaveAttempt(body);
    }
}
