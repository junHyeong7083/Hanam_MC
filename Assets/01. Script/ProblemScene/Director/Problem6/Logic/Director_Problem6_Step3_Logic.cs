using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Part6 / Problem6 / Step3 이완 훈련 로직 베이스.
/// - 여러 단계(자세, 눈 감기, 복식호흡, 이완 등)를 순서대로 재생.
/// - 시작 / 일시정지 / 재개 버튼으로 컨트롤.
/// - 각 단계마다 duration 동안 progress bar 진행.
/// - 마지막 단계까지 끝나면 StepCompletionGate 완료.
/// - 💡 이 스텝은 DB 저장 안 함.
/// </summary>
public interface IRelaxationStepData
{
    int Id { get; }
    string Title { get; }
    string Instruction { get; }
    float DurationSeconds { get; }   // 예: 3.0f, 4.0f
}

public abstract class Director_Problem6_Step3_Logic : ProblemStepBase
{
    // ===== 자식에서 주입할 추상 프로퍼티 =====

    [Header("이완 단계 데이터 (자식 주입)")]
    protected abstract IRelaxationStepData[] Steps { get; }

    [Header("UI Root")]
    protected abstract GameObject IntroRoot { get; }     // 처음 설명 + 시작 버튼
    protected abstract GameObject PlayingRoot { get; }   // 진행 중 카드 + progress + 일시정지
    protected abstract GameObject PausedRoot { get; }    // 일시정지 카드 + 계속하기

    [Header("텍스트 / 진행도 UI")]
    protected abstract Text StepCounterLabel { get; }     // "1 / 9"
    protected abstract Text StepTitleLabel { get; }       // 단계 제목
    protected abstract Text StepInstructionLabel { get; } // 단계 설명
    protected abstract Image ProgressFillImage { get; }   // 0~1 fillAmount

    [Header("호흡 원 애니메이션 루트 (옵션)")]
    protected abstract GameObject BreathingCircleRoot { get; }

    [Header("컨트롤 버튼들")]
    protected abstract Button StartButton { get; }   // "이완 훈련 시작하기"
    protected abstract Button PauseButton { get; }   // "잠시 멈추기"
    protected abstract Button ResumeButton { get; }  // "계속하기"

    [Header("완료 게이트")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("이펙트 컨트롤러")]
    protected abstract Problem6_Step3_EffectController EffectController { get; }

    [Header("완료 후 약간의 딜레이 (초)")]
    protected virtual float CompleteDelaySeconds => 2.0f;


    // ===== 내부 상태 =====

    private int _currentStepIndex;
    private bool _hasStarted;
    private bool _isPlaying;
    private bool _isCompleted;

    private Coroutine _playRoutine;
    private float _currentStepElapsed;

    // ===== ProblemStepBase Hooks =====

    protected override void OnStepEnter()
    {
        var steps = Steps;
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[Problem6_Step3] Steps 가 비어 있습니다.");
            return;
        }

        _currentStepIndex = 0;
        _hasStarted = false;
        _isPlaying = false;
        _isCompleted = false;
        _currentStepElapsed = 0f;

        // 버튼 리스너 세팅
        if (StartButton != null)
        {
            StartButton.onClick.RemoveAllListeners();
            StartButton.onClick.AddListener(OnClickStart);
        }

        if (PauseButton != null)
        {
            PauseButton.onClick.RemoveAllListeners();
            PauseButton.onClick.AddListener(OnClickPause);
        }

        if (ResumeButton != null)
        {
            ResumeButton.onClick.RemoveAllListeners();
            ResumeButton.onClick.AddListener(OnClickResume);
        }

        // 초기 UI 상태
        SetRootActive(IntroRoot, true);
        SetRootActive(PlayingRoot, false);
        SetRootActive(PausedRoot, false);

        // 호흡 원 끄기
        if (BreathingCircleRoot != null)
            BreathingCircleRoot.SetActive(false);

        // 진행도 0
        SetProgress(0f);

        // 게이트 리셋 (한 번 끝나면 완료로 취급)
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
    }

    // ===== UI Helper =====

    private void SetRootActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

    private void SetProgress(float t01)
    {
        if (ProgressFillImage != null)
        {
            ProgressFillImage.fillAmount = Mathf.Clamp01(t01);
        }
    }

    private void ApplyStepUI(IRelaxationStepData step, int index, int total)
    {
        if (StepCounterLabel != null)
            StepCounterLabel.text = $"{index + 1} / {total}";

        if (StepTitleLabel != null)
            StepTitleLabel.text = step.Title;

        if (StepInstructionLabel != null)
            StepInstructionLabel.text = step.Instruction;

        // 호흡 단계: React 기준으로 3~5번만 circle 사용
        bool breathingOn = step.Id >= 3 && step.Id <= 5;
        if (BreathingCircleRoot != null)
            BreathingCircleRoot.SetActive(breathingOn);
        if (PauseButton != null)
        {
            bool isLastStep = (index >= total - 1);
            PauseButton.gameObject.SetActive(!isLastStep);
        }
    }

    // ===== 버튼 콜백 =====

    public void OnClickStart()
    {
        if (_isCompleted) return;
        if (_hasStarted) return; // 이미 시작했다가 멈춘 경우는 Resume 사용

        _hasStarted = true;
        _isPlaying = true;
        _currentStepIndex = 0;
        _currentStepElapsed = 0f;

        // Intro -> Playing 전환
        SetRootActive(IntroRoot, false);
        SetRootActive(PlayingRoot, true);
        SetRootActive(PausedRoot, false);

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void OnClickPause()
    {
        if (!_hasStarted) return;
        if (_isCompleted) return;
        if (!_isPlaying) return;

        _isPlaying = false;

        // Playing -> Paused
        SetRootActive(PlayingRoot, false);
        SetRootActive(PausedRoot, true);

        // 호흡 애니메이션 일시정지
        if (EffectController != null)
            EffectController.PauseBreathingAnimation();
    }

    public void OnClickResume()
    {
        if (!_hasStarted) return;
        if (_isCompleted) return;
        if (_isPlaying) return;

        _isPlaying = true;

        // Paused -> Playing
        SetRootActive(PlayingRoot, true);
        SetRootActive(PausedRoot, false);

        // 호흡 애니메이션 재개
        if (EffectController != null)
            EffectController.ResumeBreathingAnimation();
    }

    // ===== 메인 루프 =====

    private IEnumerator PlayRoutine()
    {
        var steps = Steps;
        int total = steps.Length;
        var effect = EffectController;

        while (_currentStepIndex < total)
        {
            var step = steps[_currentStepIndex];
            float duration = Mathf.Max(0.1f, step.DurationSeconds);

            // 단계 UI 세팅
            ApplyStepUI(step, _currentStepIndex, total);
            _currentStepElapsed = 0f;
            SetProgress(0f);

            // 카드 팝인 애니메이션
            if (effect != null)
                effect.PlayCardPopIn();

            // 호흡 단계(3~5)면 호흡 애니메이션 시작
            bool isBreathingStep = step.Id >= 3 && step.Id <= 5;
            if (isBreathingStep && effect != null)
                effect.StartBreathingAnimation();

            // duration 동안 진행 (일시정지 시에는 시간 멈춤)
            while (_currentStepElapsed < duration)
            {
                if (_isPlaying)
                {
                    float dt = Time.deltaTime;
                    _currentStepElapsed += dt;

                    float t = Mathf.Clamp01(_currentStepElapsed / duration);
                    SetProgress(t);
                }

                yield return null;
            }

            // 호흡 애니메이션 정지
            if (isBreathingStep && effect != null)
                effect.StopBreathingAnimation();

            // 다음 단계로
            _currentStepIndex++;
            SetProgress(0f);
        }

        // 모두 끝났을 때
        yield return new WaitForSeconds(CompleteDelaySeconds);
        OnAllStepsCompleted();

        _playRoutine = null;
    }

    private void OnAllStepsCompleted()
    {
        if (_isCompleted) return;

        _isCompleted = true;
        _isPlaying = false;

        // 마지막 카드 그대로 두고, 그냥 Gate만 완료
        SetRootActive(PlayingRoot, true);
        SetRootActive(PausedRoot, false);
        SetRootActive(IntroRoot, false);

        if (CompletionGate != null)
            CompletionGate.MarkOneDone();
        else
            Debug.LogWarning("[Problem6_Step3] CompletionGate가 설정되어 있지 않습니다.");
    }
}
