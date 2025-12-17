using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step2 로직 베이스
/// - 3개의 시나리오를 순차적으로 진행
/// - 각 시나리오마다 3개 선택지 (공격적/수동적/건강한 표현)
/// - 건강한 표현(c) 선택 시 OK컷 → 다음 시나리오
/// - 그 외 선택 시 NG → 다시 시도
/// - 3개 모두 완료 시 Gate 완료
/// </summary>
public abstract class Director_Problem9_Step2_Logic : ProblemStepBase
{
    #region Data Classes

    [Serializable]
    public class ScenarioData
    {
        public int id;
        [TextArea(2, 4)]
        public string situation;
        public ChoiceData[] choices;
        [TextArea(2, 4)]
        public string okResponse;
        [TextArea(2, 4)]
        public string ngResponse;
    }

    [Serializable]
    public class ChoiceData
    {
        public string id; // a, b, c
        public ChoiceType type;
        [TextArea(2, 4)]
        public string text;
        public string label;
    }

    public enum ChoiceType
    {
        Aggressive,
        Passive,
        Healthy
    }

    [Serializable]
    public class ScenarioUI
    {
        [Header("시나리오 텍스트")]
        public Text situationText;

        [Header("선택지 버튼들")]
        public Button choiceButtonA;
        public Button choiceButtonB;
        public Button choiceButtonC;

        [Header("선택지 텍스트")]
        public Text choiceTextA;
        public Text choiceTextB;
        public Text choiceTextC;
    }

    [Serializable]
    public class ResultUI
    {
        [Header("결과 메시지")]
        public Text responseText;

        [Header("다음/다시시도 버튼")]
        public Button actionButton;

        [Header("완료 버튼 (OkResultUI 전용)")]
        public Button endButton;
    }

    [Serializable]
    public class ProgressDot
    {
        public Image dotImage;
        public Color pendingColor;
        public Color currentColor;
        public Color completedColor;
    }

    // DB 저장용 DTO (Problem3 패턴 참고)
    [Serializable]
    public class ScenarioAttemptDto
    {
        public string stepKey;              // Step 키
        public int scenarioId;              // 시나리오 ID
        public int scenarioIndex;           // 시나리오 인덱스 (0, 1, 2)
        public string situation;            // 시나리오 상황 텍스트
        public string selectedChoiceId;     // 선택한 답 ID (a, b, c)
        public string selectedChoiceText;   // 선택한 답 텍스트
        public string selectedChoiceType;   // 선택 유형 (Aggressive, Passive, Healthy)
        public bool isCorrect;              // 정답 여부
        public int attemptCount;            // 해당 시나리오 시도 횟수
        public DateTime answeredAt;         // 응답 시각
    }

    #endregion

    #region Abstract Properties

    [Header("===== 시나리오 데이터 =====")]
    protected abstract ScenarioData[] Scenarios { get; }

    [Header("===== 화면 루트 =====")]
    protected abstract GameObject ScenarioRoot { get; }
    protected abstract GameObject OkResultRoot { get; }
    protected abstract GameObject NgResultRoot { get; }

    [Header("===== UI 참조 =====")]
    protected abstract ScenarioUI ScenarioUIRef { get; }
    protected abstract ResultUI OkResultUIRef { get; }
    protected abstract ResultUI NgResultUIRef { get; }

    [Header("===== 진행도 표시 =====")]
    protected abstract ProgressDot[] ProgressDots { get; }

    [Header("===== 완료 게이트 =====")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    /// <summary>정답 선택지 ID (기본: c = 건강한 표현)</summary>
    protected virtual string CorrectChoiceId => "c";

    /// <summary>결과 표시 후 대기 시간</summary>
    protected virtual float DelayAfterResult => 0.5f;

    #endregion

    // 내부 상태
    private int _currentScenarioIndex;
    private int _attemptCount;
    private string _selectedChoiceId;
    private bool _isShowingResult;

    #region Step Lifecycle

    protected override void OnStepEnter()
    {
        _currentScenarioIndex = 0;
        _attemptCount = 0;
        _selectedChoiceId = null;
        _isShowingResult = false;

        // Gate 초기화 (3개 시나리오 완료 필요)
        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(Scenarios.Length);

        // 버튼 초기 상태 (endButton 숨김)
        if (OkResultUIRef?.endButton != null)
            OkResultUIRef.endButton.gameObject.SetActive(false);

        // 초기 화면 설정
        ShowScenarioScreen();
        UpdateProgressDots();
        RegisterListeners();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        RemoveListeners();
    }

    #endregion

    #region UI Control

    private void ShowScenarioScreen()
    {
        if (ScenarioRoot != null) ScenarioRoot.SetActive(true);
        if (OkResultRoot != null) OkResultRoot.SetActive(false);
        if (NgResultRoot != null) NgResultRoot.SetActive(false);

        _isShowingResult = false;

        // 현재 시나리오 데이터로 UI 업데이트
        ApplyScenarioToUI();
    }

    private void ShowResultScreen(bool isOk)
    {
        if (ScenarioRoot != null) ScenarioRoot.SetActive(false);

        if (isOk)
        {
            if (OkResultRoot != null) OkResultRoot.SetActive(true);
            if (NgResultRoot != null) NgResultRoot.SetActive(false);

            // OK 결과 UI 업데이트
            var data = GetCurrentScenario();
            if (data != null && OkResultUIRef != null)
            {
                if (OkResultUIRef.responseText != null)
                    OkResultUIRef.responseText.text = data.okResponse;

                // 마지막 시나리오면 endButton 표시, 아니면 actionButton 표시
                bool isLast = _currentScenarioIndex >= Scenarios.Length - 1;
                if (isLast)
                {
                    // 마지막: endButton 활성화, actionButton 비활성화
                    if (OkResultUIRef.actionButton != null)
                        OkResultUIRef.actionButton.gameObject.SetActive(false);
                    if (OkResultUIRef.endButton != null)
                        OkResultUIRef.endButton.gameObject.SetActive(true);
                }
                else
                {
                    // 진행 중: actionButton 활성화, endButton 비활성화
                    if (OkResultUIRef.actionButton != null)
                        OkResultUIRef.actionButton.gameObject.SetActive(true);
                    if (OkResultUIRef.endButton != null)
                        OkResultUIRef.endButton.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (OkResultRoot != null) OkResultRoot.SetActive(false);
            if (NgResultRoot != null) NgResultRoot.SetActive(true);

            // NG 결과 UI 업데이트
            var data = GetCurrentScenario();
            if (data != null && NgResultUIRef != null)
            {
                if (NgResultUIRef.responseText != null)
                    NgResultUIRef.responseText.text = data.ngResponse;
            }
        }

        _isShowingResult = true;
    }

    private void ApplyScenarioToUI()
    {
        var data = GetCurrentScenario();
        if (data == null) return;

        var ui = ScenarioUIRef;
        if (ui == null) return;

        // 상황 텍스트
        if (ui.situationText != null)
            ui.situationText.text = data.situation;

        // 선택지 업데이트
        if (data.choices != null && data.choices.Length >= 3)
        {
            ApplyChoiceToUI(ui.choiceTextA, data.choices[0]);
            ApplyChoiceToUI(ui.choiceTextB, data.choices[1]);
            ApplyChoiceToUI(ui.choiceTextC, data.choices[2]);
        }
    }

    private void ApplyChoiceToUI(Text textUI, ChoiceData choice)
    {
        if (choice == null) return;

        if (textUI != null)
            textUI.text = choice.text;
    }

    private void UpdateProgressDots()
    {
        var dots = ProgressDots;
        if (dots == null) return;

        for (int i = 0; i < dots.Length; i++)
        {
            var dot = dots[i];
            if (dot == null || dot.dotImage == null) continue;

            if (i < _currentScenarioIndex)
            {
                // 완료된 시나리오
                dot.dotImage.color = dot.completedColor;
            }
            else if (i == _currentScenarioIndex)
            {
                // 현재 시나리오
                dot.dotImage.color = dot.currentColor;
            }
            else
            {
                // 대기 중인 시나리오
                dot.dotImage.color = dot.pendingColor;
            }
        }
    }

    #endregion

    #region Listeners

    private void RegisterListeners()
    {
        var ui = ScenarioUIRef;
        if (ui == null) return;

        if (ui.choiceButtonA != null)
        {
            ui.choiceButtonA.onClick.RemoveAllListeners();
            ui.choiceButtonA.onClick.AddListener(() => OnChoiceClicked("a"));
        }

        if (ui.choiceButtonB != null)
        {
            ui.choiceButtonB.onClick.RemoveAllListeners();
            ui.choiceButtonB.onClick.AddListener(() => OnChoiceClicked("b"));
        }

        if (ui.choiceButtonC != null)
        {
            ui.choiceButtonC.onClick.RemoveAllListeners();
            ui.choiceButtonC.onClick.AddListener(() => OnChoiceClicked("c"));
        }

        // 결과 화면 버튼
        if (OkResultUIRef?.actionButton != null)
        {
            OkResultUIRef.actionButton.onClick.RemoveAllListeners();
            OkResultUIRef.actionButton.onClick.AddListener(OnOkActionClicked);
        }

        if (OkResultUIRef?.endButton != null)
        {
            OkResultUIRef.endButton.onClick.RemoveAllListeners();
            OkResultUIRef.endButton.onClick.AddListener(OnOkActionClicked);
        }

        if (NgResultUIRef?.actionButton != null)
        {
            NgResultUIRef.actionButton.onClick.RemoveAllListeners();
            NgResultUIRef.actionButton.onClick.AddListener(OnNgActionClicked);
        }
    }

    private void RemoveListeners()
    {
        var ui = ScenarioUIRef;
        if (ui != null)
        {
            ui.choiceButtonA?.onClick.RemoveAllListeners();
            ui.choiceButtonB?.onClick.RemoveAllListeners();
            ui.choiceButtonC?.onClick.RemoveAllListeners();
        }

        OkResultUIRef?.actionButton?.onClick.RemoveAllListeners();
        OkResultUIRef?.endButton?.onClick.RemoveAllListeners();
        NgResultUIRef?.actionButton?.onClick.RemoveAllListeners();
    }

    #endregion

    #region Event Handlers

    private void OnChoiceClicked(string choiceId)
    {
        if (_isShowingResult) return;

        _selectedChoiceId = choiceId;
        _attemptCount++;

        bool isCorrect = choiceId == CorrectChoiceId;

        // DB 저장 (Problem3 패턴: 매 선택마다 저장)
        var data = GetCurrentScenario();
        var choice = GetChoiceById(choiceId);

        SaveAttempt(new ScenarioAttemptDto
        {
            stepKey = context != null ? context.CurrentStepKey : null,
            scenarioId = data?.id ?? 0,
            scenarioIndex = _currentScenarioIndex,
            situation = data?.situation ?? "",
            selectedChoiceId = choiceId,
            selectedChoiceText = choice?.text ?? "",
            selectedChoiceType = choice?.type.ToString() ?? "",
            isCorrect = isCorrect,
            attemptCount = _attemptCount,
            answeredAt = DateTime.UtcNow
        });

        // 정답일 때 Gate 완료 처리 (OkResultUI의 버튼에서 하지 않음)
        if (isCorrect)
        {
            var gate = CompletionGateRef;
            if (gate != null)
                gate.MarkOneDone();
        }

        // 결과 화면 표시
        ShowResultScreen(isCorrect);
    }

    private void OnOkActionClicked()
    {
        bool isLastScenario = _currentScenarioIndex >= Scenarios.Length - 1;

        if (!isLastScenario)
        {
            // 다음 시나리오로 이동
            _currentScenarioIndex++;
            _attemptCount = 0;
            UpdateProgressDots();
            ShowScenarioScreen();
        }
        // else: 마지막 시나리오 → endButton은 인스펙터에서 NextStep 연결
    }

    private void OnNgActionClicked()
    {
        // 같은 시나리오 다시 시도
        ShowScenarioScreen();
    }

    #endregion

    #region Helpers

    private ScenarioData GetCurrentScenario()
    {
        var scenarios = Scenarios;
        if (scenarios == null || _currentScenarioIndex >= scenarios.Length)
            return null;

        return scenarios[_currentScenarioIndex];
    }

    private ChoiceData GetChoiceById(string choiceId)
    {
        var data = GetCurrentScenario();
        if (data?.choices == null) return null;

        foreach (var choice in data.choices)
        {
            if (choice.id == choiceId)
                return choice;
        }

        return null;
    }

    #endregion
}
