using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HanamBoxRoot에 붙여서 대사 시퀀스 + NextStepBtn을 관리
/// - OnEnable 시 enterTextIds 시퀀스 시작
/// - NextDialogueBtn 탭으로 다음 대사 진행
/// - 마지막 enter 텍스트 표시 시 OnEnterComplete 이벤트
/// - ShowCompletedText() 호출 시 completedTextIds 시퀀스
/// - 마지막 completed 텍스트 표시 시 NextStepBtn 자동 표시
/// - NextStepBtn 클릭 시 StepFlowController.NextStep() 호출
/// </summary>
public class DialogueSequencer : MonoBehaviour
{
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button nextDialogueBtn;
    [SerializeField] private Button nextStepBtn;

    [Header("쪽수 표시")]
    [SerializeField] private Text pageText;

    [Header("진입 시 순차 대사")]
    [SerializeField] private int[] enterTextIds;

    [Header("완료 시 순차 대사")]
    [SerializeField] private int[] completedTextIds;

    [Header("Intro 모드")]
    [Tooltip("true: enter 끝나면 바로 nextStepBtn 표시 (Intro용)")]
    [SerializeField] private bool showNextStepAfterEnter;

    public event Action OnEnterComplete;
    public event Action OnEnterSequenceDone;

    /// <summary>enterTextIds의 첫 텍스트가 화면에 표시될 때 1회 발행</summary>
    public event Action OnFirstTextShown;

    /// <summary>
    /// OnEnable 전에 enterTextIds를 동적으로 교체
    /// </summary>
    public void SetEnterTextIds(int[] textIds)
    {
        enterTextIds = textIds;
    }

    /// <summary>enter + completed 합산 길이</summary>
    public int EnterTextCount => (enterTextIds != null) ? enterTextIds.Length : 0;
    public int CompletedTextCount => (completedTextIds != null) ? completedTextIds.Length : 0;

    /// <summary>외부에서 추가 페이지 수 설정 (카드 수 등)</summary>
    private int _extraPageCount;
    public void SetExtraPageCount(int count) { _extraPageCount = count; }

    private int[] _activeTextIds;
    private int _currentIndex;
    private Action _onLastShown;
    private Action _onSequenceDone;
    private Coroutine _enterRoutine;

    private void OnEnable()
    {
        if (nextStepBtn != null)
        {
            nextStepBtn.gameObject.SetActive(false);
        }

        // 1프레임 지연: 다른 컴포넌트의 OnEnable에서 이벤트 구독할 시간 확보
        _enterRoutine = StartCoroutine(StartEnterSequenceDelayed());
    }

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

        var sm = SoundManager.Instance;
        if (sm != null) sm.StopTTS();

        _onLastShown = null;
        _onSequenceDone = null;
    }

    public void ShowCompletedText()
    {
        if (completedTextIds == null || completedTextIds.Length == 0)
            return;

        PlaySequence(completedTextIds,
            onLastShown: ShowNextStepBtn);
    }

    private void PlaySequence(int[] textIds, Action onLastShown, Action onDone = null)
    {
        _activeTextIds = textIds;
        _currentIndex = 0;
        _onLastShown = onLastShown;
        _onSequenceDone = onDone;

        if (nextDialogueBtn != null)
        {
            nextDialogueBtn.onClick.RemoveListener(OnClickNext);
            nextDialogueBtn.onClick.AddListener(OnClickNext);
        }

        if (_activeTextIds != null && _activeTextIds.Length > 0)
            ShowCurrent();
        else
        {
            // 텍스트 없으면 즉시 onLastShown 호출
            var cb = _onLastShown;
            _onLastShown = null;
            cb?.Invoke();
        }
    }

    private void ShowCurrent()
    {
        if (_activeTextIds == null || _currentIndex >= _activeTextIds.Length)
        {
            Complete();
            return;
        }

        int textId = _activeTextIds[_currentIndex];

        if (dialogueText != null)
            dialogueText.text = ProblemRuntime.L(textId);

        // 쪽수 표시: (현재/전체) — enter + completed 합산
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

        var sm = SoundManager.Instance;
        if (sm != null) sm.PlayTTS(textId);

        // 첫 텍스트가 표시되는 순간 이벤트 발행
        if (_currentIndex == 0 && _activeTextIds == enterTextIds)
            OnFirstTextShown?.Invoke();

        // 마지막 텍스트
        if (_currentIndex == _activeTextIds.Length - 1)
        {
            if (_onLastShown != null)
            {
                var cb = _onLastShown;
                _onLastShown = null;
                cb.Invoke();
            }

            // onSequenceDone이 있으면 클릭 후 완료 (enter 시퀀스)
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

        if (nextDialogueBtn != null)
            nextDialogueBtn.gameObject.SetActive(true);
    }

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
