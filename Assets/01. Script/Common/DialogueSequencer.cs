using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DialogueSequencer - 하남 캐릭터 대사 시퀀스 관리 컴포넌트
///
/// 【역할】 HanamBoxRoot 프리팹에 부착되어 대사 텍스트를 순서대로 표시하고,
///          TTS 음성을 재생하며, 다음 스텝 전환 버튼을 제어한다.
///          "진입 대사(enterTextIds)"와 "완료 대사(completedTextIds)" 두 종류의 시퀀스를 관리한다.
///
/// 【참조하는 곳】 Problem1~10의 Step2/Step3 Logic 파일 (Director_ProblemN_StepN_Logic.cs) — 이벤트 구독 및 ShowCompletedText() 호출,
///                InventoryDropTargetStepBase, RandomCardSequenceStepBase — 스텝 완료 시 ShowCompletedText() 호출,
///                CommonRewardStep — 보상 스텝에서 대사 제어,
///                TTSTrigger — TTS 재생 연동,
///                DebugShortcutController — 디버그 시 대사 건너뛰기
/// 【참조되는 곳】 ProblemRuntime.L(textId) — CSV에서 텍스트 로드,
///                SoundManager.PlayTTS(textId) — TTS 음성 재생,
///                StepFlowController — NextStepBtn 클릭 시 다음 스텝 전환
///
/// 【흐름】
///   1. 스텝 활성화 → OnEnable() → 1프레임 지연 후 enterTextIds 시퀀스 자동 시작
///   2. 유저가 NextDialogueBtn 클릭 → 다음 대사 표시 + TTS 재생
///   3. 마지막 enter 대사 표시 시 → OnEnterComplete 이벤트 발행 (Logic에서 문제 UI 표시 등)
///   4. enter 시퀀스 완전 종료 시 → OnEnterSequenceDone 이벤트 발행
///   5. 문제 풀이 완료 후 Logic에서 ShowCompletedText() 호출 → completedTextIds 시퀀스 시작
///   6. 마지막 completed 대사 표시 시 → NextStepBtn 자동 표시
///   7. NextStepBtn 클릭 → StepFlowController.NextStep()으로 다음 스텝 이동
/// </summary>
public class DialogueSequencer : MonoBehaviour
{
    /// <summary>대사 텍스트를 표시하는 UI Text 컴포넌트 (HanamBoxRoot 내부)</summary>
    [SerializeField] private Text dialogueText;

    /// <summary>다음 대사로 넘기는 버튼 (유저가 탭하면 다음 대사 표시)</summary>
    [SerializeField] private Button nextDialogueBtn;

    /// <summary>다음 스텝으로 넘어가는 버튼 (completed 시퀀스 완료 후 자동 표시)</summary>
    [SerializeField] private Button nextStepBtn;

    [Header("쪽수 표시")]
    /// <summary>현재 대사 페이지 번호를 표시하는 텍스트 (예: "(2/5)")</summary>
    [SerializeField] private Text pageText;

    [Header("진입 시 순차 대사")]
    /// <summary>스텝 진입 시 순서대로 재생할 대사 textId 배열 (CSV DataTable 기준)</summary>
    [SerializeField] private int[] enterTextIds;

    [Header("완료 시 순차 대사")]
    /// <summary>문제 풀이 완료 후 순서대로 재생할 대사 textId 배열</summary>
    [SerializeField] private int[] completedTextIds;

    [Header("Intro 모드")]
    [Tooltip("true: enter 끝나면 바로 nextStepBtn 표시 (Intro용)")]
    /// <summary>true이면 enter 시퀀스 완료 후 바로 NextStepBtn을 표시 (Intro 스텝에서 사용)</summary>
    [SerializeField] private bool showNextStepAfterEnter;

    /// <summary>마지막 enter 텍스트가 화면에 "표시"될 때 발행 (UI 표시 타이밍 — 아직 유저 클릭 전)</summary>
    public event Action OnEnterComplete;

    /// <summary>enter 시퀀스가 완전히 "종료"될 때 발행 (마지막 대사에서 유저가 클릭한 후)</summary>
    public event Action OnEnterSequenceDone;

    /// <summary>enterTextIds의 첫 텍스트가 화면에 표시될 때 1회 발행 (초기 UI 설정용)</summary>
    public event Action OnFirstTextShown;

    /// <summary>
    /// OnEnable 전에 enterTextIds를 동적으로 교체
    /// </summary>
    public void SetEnterTextIds(int[] textIds)
    {
        enterTextIds = textIds;
    }

    /// <summary>enter 대사 배열의 길이 (쪽수 계산용)</summary>
    public int EnterTextCount => (enterTextIds != null) ? enterTextIds.Length : 0;

    /// <summary>completed 대사 배열의 길이 (쪽수 계산용)</summary>
    public int CompletedTextCount => (completedTextIds != null) ? completedTextIds.Length : 0;

    /// <summary>외부에서 추가 페이지 수 설정 (RandomCardSequence 등에서 카드 수만큼 추가)</summary>
    private int _extraPageCount;

    /// <summary>
    /// 추가 페이지 수를 설정한다. 쪽수 표시(pageText)에서 전체 페이지에 합산된다.
    /// RandomCardSequenceStepBase에서 카드 개수를 추가 페이지로 설정할 때 사용.
    /// </summary>
    /// <param name="count">추가할 페이지 수</param>
    public void SetExtraPageCount(int count) { _extraPageCount = count; }

    /// <summary>현재 재생 중인 시퀀스의 textId 배열 (enter 또는 completed)</summary>
    private int[] _activeTextIds;

    /// <summary>현재 시퀀스에서 표시 중인 대사의 인덱스</summary>
    private int _currentIndex;

    /// <summary>마지막 대사가 "표시"될 때 호출될 콜백 (표시 직후, 유저 클릭 전)</summary>
    private Action _onLastShown;

    /// <summary>시퀀스가 완전히 "종료"될 때 호출될 콜백 (마지막 대사 클릭 후)</summary>
    private Action _onSequenceDone;

    /// <summary>enter 시퀀스 코루틴 참조 (OnDisable에서 정리용)</summary>
    private Coroutine _enterRoutine;

    /// <summary>
    /// 스텝 활성화 시 호출. NextStepBtn을 숨기고, 1프레임 지연 후 enter 시퀀스를 시작한다.
    /// 1프레임 지연하는 이유: 같은 프레임에서 다른 컴포넌트(Logic 등)가 OnEnable에서 이벤트를 구독할 시간을 확보하기 위함.
    /// </summary>
    private void OnEnable()
    {
        if (nextStepBtn != null)
        {
            nextStepBtn.gameObject.SetActive(false);
        }

        // 1프레임 지연: 다른 컴포넌트의 OnEnable에서 이벤트 구독할 시간 확보
        _enterRoutine = StartCoroutine(StartEnterSequenceDelayed());
    }

    /// <summary>
    /// 1프레임 대기 후 enter 시퀀스를 시작하는 코루틴.
    /// onLastShown: 마지막 enter 대사가 표시될 때 OnEnterComplete 이벤트 발행
    /// onDone: enter 시퀀스 완전 종료 시 OnEnterSequenceDone 이벤트 발행
    /// </summary>
    private IEnumerator StartEnterSequenceDelayed()
    {
        yield return null;

        PlaySequence(enterTextIds,
            onLastShown: () => OnEnterComplete?.Invoke(),
            onDone: () =>
            {
                OnEnterSequenceDone?.Invoke();

                // enter 끝난 후 nextStepBtn 표시: Intro용 플래그가 켜진 경우만
                if (showNextStepAfterEnter)
                    ShowNextStepBtn();
            });

        _enterRoutine = null;
    }

    /// <summary>
    /// 스텝 비활성화 시 호출. 모든 상태를 정리한다.
    /// - 진행 중인 코루틴 중지
    /// - 버튼 리스너 해제 및 버튼 숨김
    /// - TTS 음성 정지
    /// - 콜백 참조 해제 (메모리 누수 방지)
    /// </summary>
    private void OnDisable()
    {
        _extraPageCount = 0;

        if (_enterRoutine != null)
        {
            StopCoroutine(_enterRoutine);
            _enterRoutine = null;
        }

        if (nextDialogueBtn != null)
        {
            nextDialogueBtn.onClick.RemoveListener(OnClickNext);
            nextDialogueBtn.gameObject.SetActive(false);
        }

        if (nextStepBtn != null)
        {
            nextStepBtn.gameObject.SetActive(false);
        }

        // 스텝 전환 시 이전 TTS가 계속 재생되지 않도록 강제 중지
        var sm = SoundManager.Instance;
        if (sm != null) sm.StopTTS();

        _onLastShown = null;
        _onSequenceDone = null;
    }

    /// <summary>
    /// 완료 대사 시퀀스를 시작한다.
    /// 문제 풀이가 끝난 후 Logic 클래스에서 호출한다.
    /// 마지막 completed 대사가 표시되면 NextStepBtn이 자동으로 나타난다.
    /// </summary>
    public void ShowCompletedText()
    {
        if (completedTextIds == null || completedTextIds.Length == 0)
            return;

        PlaySequence(completedTextIds,
            onLastShown: ShowNextStepBtn);
    }

    /// <summary>
    /// 대사 시퀀스 재생의 핵심 메서드.
    /// textIds 배열을 순서대로 표시하며, 각 대사마다 NextDialogueBtn 클릭을 기다린다.
    /// </summary>
    /// <param name="textIds">재생할 textId 배열</param>
    /// <param name="onLastShown">마지막 대사가 화면에 표시될 때 호출될 콜백</param>
    /// <param name="onDone">시퀀스 완전 종료 시 호출될 콜백 (null이면 마지막 대사 표시 후 즉시 Complete)</param>
    private void PlaySequence(int[] textIds, Action onLastShown, Action onDone = null)
    {
        _activeTextIds = textIds;
        _currentIndex = 0;
        _onLastShown = onLastShown;
        _onSequenceDone = onDone;

        // 버튼 리스너 중복 방지: 먼저 제거 후 재등록
        if (nextDialogueBtn != null)
        {
            nextDialogueBtn.onClick.RemoveListener(OnClickNext);
            nextDialogueBtn.onClick.AddListener(OnClickNext);
        }

        if (_activeTextIds != null && _activeTextIds.Length > 0)
            ShowCurrent();
        else
        {
            // 텍스트 없으면 즉시 onLastShown 호출 (빈 시퀀스 대응)
            var cb = _onLastShown;
            _onLastShown = null;
            cb?.Invoke();
        }
    }

    /// <summary>
    /// 현재 인덱스(_currentIndex)에 해당하는 대사를 화면에 표시하고 TTS를 재생한다.
    /// 마지막 대사인 경우 onLastShown 콜백 호출 후, onSequenceDone 유무에 따라 동작이 분기된다:
    ///   - onSequenceDone이 있으면 (enter 시퀀스): NextDialogueBtn을 표시하여 클릭 대기 → 클릭 시 Complete()
    ///   - onSequenceDone이 없으면 (completed 시퀀스): 즉시 Complete() (NextStepBtn이 이미 표시된 상태)
    /// </summary>
    private void ShowCurrent()
    {
        if (_activeTextIds == null || _currentIndex >= _activeTextIds.Length)
        {
            Complete();
            return;
        }

        int textId = _activeTextIds[_currentIndex];

        // CSV DataTable에서 textId에 해당하는 한글 텍스트를 가져와 UI에 표시
        if (dialogueText != null)
            dialogueText.text = ProblemRuntime.L(textId);

        // 쪽수 표시: (현재/전체) — enter + extraPage(카드 등) + completed 합산
        // completed 시퀀스 재생 시에는 enter 길이 + extraPage만큼 오프셋 추가
        if (pageText != null)
        {
            int enterLen = (enterTextIds != null) ? enterTextIds.Length : 0;
            int completedLen = (completedTextIds != null) ? completedTextIds.Length : 0;
            int totalPages = enterLen + completedLen + _extraPageCount;

            int currentPage = _currentIndex + 1;
            if (_activeTextIds == completedTextIds)
                currentPage += enterLen + _extraPageCount;

            pageText.text = $"({currentPage}/{totalPages})";
        }

        // SoundManager를 통해 해당 textId의 TTS 음성 파일 재생
        var sm = SoundManager.Instance;
        if (sm != null) sm.PlayTTS(textId);

        // 첫 텍스트가 표시되는 순간 이벤트 발행 (Logic에서 초기 UI 세팅에 활용)
        if (_currentIndex == 0 && _activeTextIds == enterTextIds)
            OnFirstTextShown?.Invoke();

        // 마지막 텍스트 처리
        if (_currentIndex == _activeTextIds.Length - 1)
        {
            if (_onLastShown != null)
            {
                var cb = _onLastShown;
                _onLastShown = null;
                cb.Invoke();
            }

            // onSequenceDone이 있으면 클릭 후 완료 (enter 시퀀스 — 유저가 마지막 대사를 확인한 뒤 클릭해야 진행)
            // 없으면 즉시 완료 (completed 시퀀스 — nextStepBtn이 이미 표시됨)
            if (_onSequenceDone != null)
            {
                if (nextDialogueBtn != null)
                    nextDialogueBtn.gameObject.SetActive(true);
                return;
            }

            Complete();
            return;
        }

        // 마지막이 아닌 경우: 다음 대사 버튼 표시
        if (nextDialogueBtn != null)
            nextDialogueBtn.gameObject.SetActive(true);
    }

    /// <summary>NextDialogueBtn 클릭 시 호출. 인덱스를 1 증가시키고 다음 대사를 표시한다.</summary>
    private void OnClickNext()
    {
        _currentIndex++;
        ShowCurrent();
    }

    /// <summary>
    /// 외부에서 다음 대사로 넘김 (단축키 등)
    /// </summary>
    public void AdvanceNext()
    {
        if (_activeTextIds == null || _currentIndex >= _activeTextIds.Length)
            return;

        OnClickNext();
    }

    /// <summary>
    /// 시퀀스 완료 처리. NextDialogueBtn을 숨기고 리스너를 해제한 뒤 onSequenceDone 콜백을 호출한다.
    /// enter 시퀀스 완료 시: OnEnterSequenceDone 이벤트가 발행됨
    /// completed 시퀀스 완료 시: onSequenceDone이 null이므로 아무 콜백도 호출되지 않음
    /// </summary>
    private void Complete()
    {
        if (nextDialogueBtn != null)
        {
            nextDialogueBtn.onClick.RemoveListener(OnClickNext);
            nextDialogueBtn.gameObject.SetActive(false);
        }

        var cb = _onSequenceDone;
        _onSequenceDone = null;
        cb?.Invoke();
    }

    /// <summary>NextStepBtn을 화면에 표시한다. 유저가 클릭하면 StepFlowController.NextStep()이 호출된다.</summary>
    private void ShowNextStepBtn()
    {
        if (nextStepBtn != null)
            nextStepBtn.gameObject.SetActive(true);
    }

    /// <summary>
    /// 시퀀스 외부에서 직접 텍스트를 설정 (per-card 대사 등)
    /// nextDialogueBtn은 숨김 처리
    /// </summary>
    public void SetText(int textId)
    {
        if (dialogueText != null)
            dialogueText.text = ProblemRuntime.L(textId);

        if (nextDialogueBtn != null)
            nextDialogueBtn.gameObject.SetActive(false);
    }

    /// <summary>
    /// 시퀀스 외부에서 텍스트 + 쪽수 직접 설정
    /// </summary>
    public void SetText(int textId, int currentPage, int totalPages)
    {
        SetText(textId);

        if (pageText != null)
            pageText.text = $"({currentPage}/{totalPages})";
    }
}
