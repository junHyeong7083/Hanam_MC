using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem1_Step2_Logic - 문제1 스텝2의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 필름 조각(FilmFragment)들을 화면에 배치하고, 사용자가 각 필름을 터치하면
///         체크마크 표시 + 플래시 효과 + 밝기 변경 등의 처리를 수행한다.
///         모든 필름을 터치하면 StepCompletionGate를 통해 다음 스텝으로 진행한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측. 필름 배열, 알파값, 완료 게이트 등은 abstract property.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝2 (메인 활동 - 필름 조각 찾기)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem1_Step2 (Binder), 인스펙터에서 OnFilmClicked 버튼 연결
/// 【참조되는 곳】 FilmCardWiggle (흔들림 애니메이션), IntroElement/ShakeTrigger (인트로 연출),
///               DialogueSequencer (대사 시퀀스), StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → 대사 재생(enter) → 대사 완료 후 상호작용 잠금 해제 →
///         사용자가 필름 클릭 → 체크/플래시/밝기 처리 → 모든 필름 완료 → completed 대사
/// </summary>
public abstract class Director_Problem1_Step2_Logic : ProblemStepBase
{
    /// <summary>
    /// 개별 필름 조각의 데이터 구조.
    /// 각 필름에는 고유 ID, 체크마크, 플래시 오버레이, 어둡기 조절 대상, 텍스트,
    /// 흔들림 애니메이션, 인트로 요소, 쉐이크 트리거 등이 포함된다.
    /// </summary>
    [System.Serializable]
    public class FilmFragment
    {
        public int id;                        // 필름 고유 ID (OnFilmClicked에서 식별용)
        public GameObject checkMark;          // 터치 완료 시 표시할 체크마크 오브젝트
        public GameObject flashOverlay;       // 터치 시 순간적으로 보이는 플래시 효과
        public Graphic dimTarget;             // 어둡기 조절 대상 (터치 전=어두움, 터치 후=밝음)
        public Text buttonText;              // 필름 위에 표시할 텍스트
        public FilmCardWiggle wiggle;        // 필름 카드 흔들림 애니메이션 컴포넌트
        public IntroElement introElement;    // 인트로 등장 애니메이션 요소
        public ShakeTrigger shakeTrigger;    // 인트로 완료 후 시작되는 흔들림 트리거
    }

    /// <summary>필름 조각 배열 (자식에서 SerializeField로 바인딩)</summary>
    protected abstract FilmFragment[] Films { get; }

    /// <summary>터치 전 필름의 어둡기 알파값 (0~1, 낮을수록 어두움)</summary>
    protected abstract float DimAlpha { get; }

    /// <summary>터치 후 필름의 밝기 알파값 (보통 1.0)</summary>
    protected abstract float NormalAlpha { get; }

    /// <summary>완료 게이트 - 모든 필름 터치 시 다음 스텝 진행</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;  // 대사 시퀀서 (enter/completed 대사 재생)

    [Header("필름 터치 효과음")]
    [SerializeField] private string filmClickSfx = "SFX_C01_S01_filmClick";  // 필름 클릭 시 재생할 효과음 키

    /// <summary>필름 ID → FilmFragment 매핑 딕셔너리 (빠른 조회용)</summary>
    private readonly Dictionary<int, FilmFragment> _filmMap = new Dictionary<int, FilmFragment>();

    /// <summary>이미 체크된(터치된) 필름 ID 집합</summary>
    private readonly HashSet<int> _checkedIds = new HashSet<int>();

    /// <summary>IntroElement.OnArrived 이벤트에 바인딩한 ShakeTrigger 핸들러 목록 (정리용)</summary>
    private readonly List<(IntroElement intro, System.Action handler)> _shakeBindings
        = new List<(IntroElement, System.Action)>();

    /// <summary>모든 필름 터치 완료 여부</summary>
    private bool _completed = false;

    /// <summary>대사 재생 중 사용자 상호작용 잠금 플래그</summary>
    private bool _interactionLocked = true;

    /// <summary>
    /// 스텝 진입 시 호출. 필름 맵 구성, 상태 초기화, 쉐이크 트리거 바인딩,
    /// 대사 시퀀스 시작 후 완료까지 상호작용을 잠근다.
    /// </summary>
    protected override void OnStepEnter()
    {
        BuildFilmMap();
        ResetState();

        BindShakeTriggers();

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    /// <summary>enter 대사 시퀀스 완료 콜백 → 상호작용 잠금 해제</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>
    /// 스텝 퇴장 시 호출. 이벤트 구독 해제, 상태 초기화.
    /// </summary>
    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        UnbindShakeTriggers();

        _checkedIds.Clear();
        _filmMap.Clear();
        _completed = false;
        _interactionLocked = true;
    }

    /// <summary>Films 배열을 순회하여 ID 기반 딕셔너리(_filmMap)를 구성한다.</summary>
    private void BuildFilmMap()
    {
        _filmMap.Clear();

        var films = Films;
        if (films == null) return;

        foreach (var f in films)
        {
            if (f == null) continue;
            if (!_filmMap.ContainsKey(f.id))
                _filmMap.Add(f.id, f);
        }
    }

    /// <summary>
    /// 모든 필름의 시각 상태를 초기화한다.
    /// 체크마크/플래시 숨김, 텍스트 숨김, 어둡기 적용, 랜덤 회전 설정.
    /// CompletionGate도 필름 수만큼 리셋한다.
    /// </summary>
    private void ResetState()
    {
        _checkedIds.Clear();
        _completed = false;

        var films = Films;
        if (films != null)
        {
            foreach (var f in films)
            {
                if (f == null) continue;

                if (f.checkMark != null) f.checkMark.SetActive(false);
                if (f.flashOverlay != null) f.flashOverlay.SetActive(false);
                if (f.buttonText != null) f.buttonText.gameObject.SetActive(false);

                if (f.dimTarget != null)
                {
                    var c = f.dimTarget.color;
                    c.a = DimAlpha;
                    f.dimTarget.color = c;
                }

                if (f.wiggle != null)
                    f.wiggle.SetRandomRotationImmediate();
            }
        }

        var gate = CompletionGate;
        if (gate != null)
        {
            int total = (films != null) ? films.Length : 0;
            gate.ResetGate(total);
        }
    }

    /// <summary>
    /// 필름 조각 클릭 이벤트 핸들러. 인스펙터에서 Button.onClick에 연결한다.
    /// 효과음 재생, 플래시 표시, 체크마크 표시, 밝기 복원, 다른 필름 회전 변경,
    /// CompletionGate 카운트 증가, 모든 필름 완료 여부 확인을 수행한다.
    /// </summary>
    /// <param name="id">클릭된 필름의 고유 ID</param>
    public void OnFilmClicked(int id)
    {
        if (_interactionLocked) return;

        if (!_filmMap.TryGetValue(id, out var fragment))
            return;

        // 효과음 재생
        if (!string.IsNullOrEmpty(filmClickSfx))
        {
            var sm = SoundManager.Instance;
            if (sm != null) sm.PlaySFX(filmClickSfx);
        }

        if (fragment.flashOverlay != null)
            StartCoroutine(FlashRoutine(fragment.flashOverlay, 0.1f));

        if (_checkedIds.Contains(id))
            return;

        _checkedIds.Add(id);

        // 터치했으니 떨림 정지
        if (fragment.shakeTrigger != null)
            fragment.shakeTrigger.StopShake();

        if (fragment.checkMark != null) fragment.checkMark.SetActive(true);
        if (fragment.buttonText != null) fragment.buttonText.gameObject.SetActive(true);

        if (fragment.dimTarget != null)
        {
            var c = fragment.dimTarget.color;
            c.a = NormalAlpha;
            fragment.dimTarget.color = c;
        }

        var films = Films;
        if (films != null)
        {
            foreach (var f in films)
            {
                if (f != null && f.wiggle != null)
                    f.wiggle.SetRandomRotation();
            }
        }

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();

        TryHandleCompleted();
    }

    /// <summary>모든 필름이 체크되었는지 확인하고, 완료 시 completed 대사를 표시한다.</summary>
    private void TryHandleCompleted()
    {
        if (_completed) return;

        int total = (Films != null) ? Films.Length : 0;
        if (total <= 0) return;

        if (_checkedIds.Count < total) return;

        _completed = true;

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }

    /// <summary>
    /// 각 필름의 IntroElement.OnArrived 이벤트에 ShakeTrigger.StartShake를 바인딩한다.
    /// 인트로 등장 애니메이션 완료 시 해당 필름이 흔들리기 시작하도록 한다.
    /// </summary>
    private void BindShakeTriggers()
    {
        UnbindShakeTriggers();

        var films = Films;
        if (films == null) return;

        foreach (var f in films)
        {
            if (f == null || f.introElement == null || f.shakeTrigger == null) continue;

            var shake = f.shakeTrigger;
            System.Action handler = () => shake.StartShake();
            f.introElement.OnArrived += handler;
            _shakeBindings.Add((f.introElement, handler));
        }
    }

    /// <summary>바인딩된 ShakeTrigger 이벤트 핸들러를 모두 해제한다.</summary>
    private void UnbindShakeTriggers()
    {
        foreach (var (intro, handler) in _shakeBindings)
        {
            if (intro != null)
                intro.OnArrived -= handler;
        }
        _shakeBindings.Clear();
    }

    /// <summary>플래시 오버레이를 짧은 시간 동안 표시했다가 숨기는 코루틴.</summary>
    private IEnumerator FlashRoutine(GameObject overlay, float duration)
    {
        overlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        overlay.SetActive(false);
    }
}
