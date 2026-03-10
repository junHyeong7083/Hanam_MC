using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Part6 / Problem6 / Step3 이완 훈련 로직 베이스.
/// - 여러 단계를 순서대로 재생.
/// - 일시정지 / 재개 버튼으로 컨트롤.
/// - 각 단계마다 duration 동안 대기 후 다음 단계.
/// - 마지막 단계까지 끝나면 StepCompletionGate 완료.
/// - 이 스텝은 DB 저장 안 함.
/// </summary>
public interface IRelaxationStepData
{
    int Id { get; }
    string Title { get; }
    int InstructionTextId { get; }
    float DurationSeconds { get; }
}

public abstract class Director_Problem6_Step3_Logic : ProblemStepBase
{
    // ===== 자식에서 주입할 추상 프로퍼티 =====

    [Header("이완 단계 데이터 (자식 주입)")]
    protected abstract IRelaxationStepData[] Steps { get; }

    [Header("UI Root")]
    protected abstract GameObject PlayingRoot { get; }
    protected abstract GameObject PausedRoot { get; }

    [Header("텍스트 UI")]
    protected abstract Text StepTitleLabel { get; }
    protected abstract Text StepInstructionLabel { get; }

    [Header("컨트롤 버튼들")]
    protected abstract Button PauseButton { get; }
    protected abstract Button ResumeButton { get; }

    [Header("완료 게이트")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("이펙트 컨트롤러")]
    protected abstract Problem6_Step3_EffectController EffectController { get; }

    [Header("프로그레스 바")]
    protected abstract Image ProgressFillImage { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

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
        SetRootActive(PlayingRoot, false);
        SetRootActive(PausedRoot, false);

        // 프로그레스 바 초기화
        if (ProgressFillImage != null)
            ProgressFillImage.fillAmount = 0f;

        // 게이트 리셋
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);

        // BGM 시작 (편안한 자세 화면부터 loop)
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM("BGM_C01_S06");

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;

        // IntroStep3에서 넘어오므로 자동 시작
        AutoStart();
    }

    private void OnDialogueEnterComplete()
    {
        // 이 스텝은 자동 재생이므로 별도 잠금 해제 필요 없음
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        // TTS 정지 (BGM은 summary/Step4까지 유지)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopTTS();
        }
    }

    // ===== UI Helper =====

    private void SetRootActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

private void ApplyStepUI(IRelaxationStepData step, int index, int total)
    {
        if (StepTitleLabel != null)
            StepTitleLabel.text = step.Title;

        if (StepInstructionLabel != null)
            StepInstructionLabel.text = ProblemRuntime.L(step.InstructionTextId);

        if (PauseButton != null)
        {
            bool isLastStep = (index >= total - 1);
            PauseButton.gameObject.SetActive(!isLastStep);
        }

        // 단계 텍스트 TTS 재생
        if (step.InstructionTextId > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(step.InstructionTextId);
    }

    // ===== 자동 시작 =====

    private void AutoStart()
    {
        if (_isCompleted) return;
        if (_hasStarted) return;

        _hasStarted = true;
        _isPlaying = true;
        _currentStepIndex = 0;
        _currentStepElapsed = 0f;

        SetRootActive(PlayingRoot, true);
        SetRootActive(PausedRoot, false);

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    // ===== 버튼 콜백 =====

    public void OnClickPause()
    {
        if (!_hasStarted) return;
        if (_isCompleted) return;
        if (!_isPlaying) return;

        _isPlaying = false;

        // TTS, BGM 일시정지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PauseTTS();
            SoundManager.Instance.PauseBGM();
        }

        SetRootActive(PlayingRoot, false);
        SetRootActive(PausedRoot, true);
    }

    public void OnClickResume()
    {
        if (!_hasStarted) return;
        if (_isCompleted) return;
        if (_isPlaying) return;

        _isPlaying = true;

        // TTS, BGM 재개
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResumeTTS();
            SoundManager.Instance.ResumeBGM();
        }

        SetRootActive(PlayingRoot, true);
        SetRootActive(PausedRoot, false);
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

            // 프로그레스 바 리셋
            if (ProgressFillImage != null)
                ProgressFillImage.fillAmount = 0f;

            // 카드 팝인 애니메이션
            if (effect != null)
                effect.PlayCardPopIn();

            // duration 동안 대기 (일시정지 시에는 시간 멈춤)
            while (_currentStepElapsed < duration)
            {
                if (_isPlaying)
                    _currentStepElapsed += Time.deltaTime;

                // 프로그레스 바 업데이트
                if (ProgressFillImage != null)
                    ProgressFillImage.fillAmount = Mathf.Clamp01(_currentStepElapsed / duration);

                yield return null;
            }

            // 다음 단계로
            _currentStepIndex++;

            // 다음 단계가 있으면 전환 SFX 재생
            if (_currentStepIndex < total && SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("SFX_C01_S06_nextStep");
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

        SetRootActive(PlayingRoot, true);
        SetRootActive(PausedRoot, false);

        if (CompletionGate != null)
            CompletionGate.MarkOneDone();
        else
            Debug.LogWarning("[Problem6_Step3] CompletionGate가 설정되어 있지 않습니다.");

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }
}
