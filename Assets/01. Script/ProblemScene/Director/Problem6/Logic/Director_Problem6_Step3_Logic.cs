using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// IRelaxationStepData - 이완 훈련의 한 단계를 나타내는 인터페이스.
/// 단계 ID, 제목, 안내 텍스트 ID, 지속 시간 정보를 제공한다.
/// Director_Problem6_Step3에서 RelaxationStepData로 구현됨.
/// </summary>
public interface IRelaxationStepData
{
    /// <summary>단계 고유 ID</summary>
    int Id { get; }
    /// <summary>단계 제목 (UI에 직접 표시)</summary>
    string Title { get; }
    /// <summary>단계 안내 텍스트의 CSV textId (ProblemRuntime.L로 읽음)</summary>
    int InstructionTextId { get; }
    /// <summary>이 단계의 지속 시간 (초)</summary>
    float DurationSeconds { get; }
}

/// <summary>
/// Director_Problem6_Step3_Logic - 문제6 스텝3 이완 훈련 로직 (추상 클래스)
///
/// 【역할】 여러 이완 단계를 순서대로 자동 재생하는 마무리 스텝.
///          각 단계마다 지정된 duration 동안 프로그레스 바가 채워지며,
///          일시정지/재개 버튼으로 제어할 수 있다.
///          마지막 단계까지 완료되면 StepCompletionGate가 열린다.
///          이 스텝은 DB에 저장하지 않는다.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. SerializeField는 Binder(Director_Problem6_Step3)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제6 > 스텝3 (마무리 - 이완 훈련)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem6_Step3 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, Problem6_Step3_EffectController, SoundManager
/// 【흐름】 스텝 진입 → BGM 시작 → 자동 재생 시작 → 단계별 UI 세팅 + TTS 재생
///         → 프로그레스 바 채움 → 단계 완료 시 다음 단계로 → 전체 완료 시 게이트 열림
/// </summary>
public abstract class Director_Problem6_Step3_Logic : ProblemStepBase
{
    // ===== 자식(Binder)에서 주입할 추상 프로퍼티 =====

    #region Abstract Properties

    /// <summary>이완 단계 데이터 배열 (Binder에서 인스펙터로 설정)</summary>
    [Header("이완 단계 데이터 (자식 주입)")]
    protected abstract IRelaxationStepData[] Steps { get; }

    [Header("UI Root")]
    /// <summary>재생 중일 때 표시되는 UI 루트</summary>
    protected abstract GameObject PlayingRoot { get; }
    /// <summary>일시정지 중일 때 표시되는 UI 루트</summary>
    protected abstract GameObject PausedRoot { get; }

    [Header("텍스트 UI")]
    /// <summary>현재 단계 제목을 표시하는 텍스트</summary>
    protected abstract Text StepTitleLabel { get; }
    /// <summary>현재 단계 안내 문구를 표시하는 텍스트</summary>
    protected abstract Text StepInstructionLabel { get; }

    [Header("컨트롤 버튼들")]
    /// <summary>일시정지 버튼 (마지막 단계에서는 숨김)</summary>
    protected abstract Button PauseButton { get; }
    /// <summary>재개 버튼 (일시정지 화면에서 표시)</summary>
    protected abstract Button ResumeButton { get; }

    [Header("완료 게이트")]
    /// <summary>스텝 완료 판정용 게이트</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("이펙트 컨트롤러")]
    /// <summary>카드 팝인 등 시각 이펙트를 제어하는 컨트롤러</summary>
    protected abstract Problem6_Step3_EffectController EffectController { get; }

    [Header("프로그레스 바")]
    /// <summary>단계 진행률을 시각적으로 표시하는 Fill 이미지</summary>
    protected abstract Image ProgressFillImage { get; }

    #endregion

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서 (진입/완료 대사 재생)

    #region Virtual Config

    [Header("완료 후 약간의 딜레이 (초)")]
    /// <summary>모든 단계 완료 후 게이트 열기 전 대기 시간 (기본 2초)</summary>
    protected virtual float CompleteDelaySeconds => 2.0f;

    #endregion


    // ===== 내부 상태 =====

    private int _currentStepIndex;          // 현재 재생 중인 이완 단계 인덱스
    private bool _hasStarted;               // 재생이 시작되었는지 여부
    private bool _isPlaying;                // 현재 재생 중인지 (일시정지 시 false)
    private bool _isCompleted;              // 모든 단계 완료 여부

    private Coroutine _playRoutine;         // 메인 재생 코루틴 핸들 (정리용)
    private float _currentStepElapsed;      // 현재 단계에서 경과한 시간 (초)

    // ===== ProblemStepBase 생명주기 훅 =====

    /// <summary>
    /// 스텝 진입 시 호출. 상태 초기화, 버튼 리스너 등록, BGM 시작 후 자동 재생을 시작한다.
    /// </summary>
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

    /// <summary>대화 진입 완료 콜백. 이 스텝은 자동 재생이므로 별도 잠금 해제가 필요 없다.</summary>
    private void OnDialogueEnterComplete()
    {
        // 이 스텝은 자동 재생이므로 별도 잠금 해제 필요 없음
    }

    /// <summary>
    /// 스텝 퇴장 시 호출. 재생 코루틴 정지, TTS 정지, 이벤트 구독 해제.
    /// BGM은 다음 스텝(summary/Step4)까지 유지된다.
    /// </summary>
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

    // ===== UI 헬퍼 =====

    /// <summary>null 안전하게 GameObject의 SetActive를 호출한다.</summary>
    private void SetRootActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

    /// <summary>
    /// 현재 이완 단계의 UI(제목, 안내 텍스트, TTS)를 갱신한다.
    /// 마지막 단계에서는 일시정지 버튼을 숨긴다.
    /// </summary>
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

    /// <summary>
    /// 이완 훈련을 자동으로 시작한다. 첫 번째 단계부터 PlayRoutine 코루틴을 실행한다.
    /// 이미 시작했거나 완료된 경우에는 무시한다.
    /// </summary>
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

    /// <summary>
    /// 일시정지 버튼 클릭 시 호출. TTS와 BGM을 일시정지하고 PausedRoot UI를 표시한다.
    /// </summary>
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

    /// <summary>
    /// 재개 버튼 클릭 시 호출. TTS와 BGM을 재개하고 PlayingRoot UI를 표시한다.
    /// </summary>
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

    // ===== 메인 재생 루프 =====

    /// <summary>
    /// 이완 단계들을 순차적으로 재생하는 메인 코루틴.
    /// 각 단계마다 ApplyStepUI → 카드 팝인 → duration 동안 프로그레스 바 채움 → 다음 단계.
    /// 일시정지 중에는 경과 시간이 멈춘다.
    /// 모든 단계 완료 후 CompleteDelaySeconds만큼 대기한 뒤 OnAllStepsCompleted를 호출한다.
    /// </summary>
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

    /// <summary>
    /// 모든 이완 단계가 완료되었을 때 호출.
    /// CompletionGate를 열고 DialogueSequencer의 완료 텍스트를 표시한다.
    /// </summary>
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
