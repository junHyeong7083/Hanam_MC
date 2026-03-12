using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Director_Problem1_Step3_Logic - 문제1 스텝3의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 필름 카드를 "생각"과 "사실"로 분류하는 핵심 게임 로직을 담당한다.
///         랜덤 순서로 카드를 보여주고, 사용자가 버튼 또는 STT(음성인식)로 분류를 선택한다.
///         정답이면 해당 슬롯으로 카드를 이동시키고, 오답이면 피드백 메시지를 표시한다.
///         모든 카드 분류 완료 시 요약 패널을 보여주고, 분류 결과를 DB에 저장한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측. 모든 UI 참조는 abstract property/method.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝3 (마무리 - 필름 분류 활동)
/// 【부모 클래스】 RandomCardSequenceStepBase → 카드 순서 랜덤화 + 순차 진행 기능 제공
/// 【참조하는 곳】 Director_Problem1_Step3 (Binder 자식 클래스)
/// 【참조되는 곳】 Director_Problem1_Step3_FilmCardAnimator (카드 등장/퇴장 애니메이션),
///               Director_Problem1_Step3_SummaryPanel (요약 패널), DialogueSequencer (대사),
///               MicRecordingIndicator (STT 음성 입력), StepCompletionGate (다음 스텝 진행)
/// 【흐름】 스텝 진입 → enter 대사 → 첫 카드 표시 → 사용자 분류(버튼/STT) →
///         정답: 슬롯 이동 애니메이션 → 다음 카드 / 오답: 피드백 표시 → 재시도 →
///         모든 카드 완료 → 요약 버튼 표시 → 요약 패널 → DB 저장 → 다음 스텝
/// </summary>
public abstract class Director_Problem1_Step3_Logic : RandomCardSequenceStepBase
{
    /// <summary>분류 결과 로그 엔트리 (DB 저장용)</summary>
    [Serializable]
    protected class SortLogEntry
    {
        public int filmId;         // 필름 고유 ID
        public string text;        // 필름 텍스트 내용
        public string correctType; // 정답 유형 ("생각" 또는 "사실")
        public string chosenType;  // 사용자가 선택한 유형
    }

    /// <summary>DB에 저장할 분류 결과 전체 페이로드</summary>
    [Serializable]
    private class SortLogPayload
    {
        public SortLogEntry[] items;
    }

    // ===== 자식에서 구현할 추상 프로퍼티/메서드 =====

    /// <summary>총 필름 카드 수</summary>
    protected abstract int FilmCount { get; }

    /// <summary>index번째 필름의 고유 ID 반환</summary>
    protected abstract int GetFilmId(int index);

    /// <summary>index번째 필름의 표시 텍스트 반환 (CSV textId 기반)</summary>
    protected abstract string GetFilmText(int index);

    /// <summary>index번째 필름의 스프라이트(이미지) 반환</summary>
    protected abstract Sprite GetFilmSprite(int index);

    /// <summary>index번째 필름이 "생각"이면 true, "사실"이면 false</summary>
    protected abstract bool IsFilmThought(int index);

    /// <summary>index번째 필름에 대응하는 하남이 대사 textId</summary>
    protected abstract int GetFilmHanamiTextId(int index);

    /// <summary>현재 카드가 표시될 UI 루트 RectTransform</summary>
    protected abstract RectTransform CurrentFilmRoot { get; }

    /// <summary>필름 카드 프리팹 (인스턴스화하여 사용)</summary>
    protected abstract GameObject CurrentFilmPrefab { get; }

    /// <summary>"생각" 카드 배치 슬롯 배열 (분류 후 이동 목표)</summary>
    protected abstract Transform[] ThoughtSlots { get; }

    /// <summary>"사실" 카드 배치 슬롯 배열 (분류 후 이동 목표)</summary>
    protected abstract Transform[] FactSlots { get; }

    /// <summary>"생각"/"사실" 분류 버튼이 포함된 루트 오브젝트</summary>
    protected abstract GameObject AnswerButtonsRoot { get; }

    /// <summary>모든 카드 분류 완료 후 표시되는 요약 버튼 루트</summary>
    protected abstract GameObject SummaryButtonRoot { get; }

    /// <summary>분류 후 다음 카드로 넘어가기 전 대기 시간 (초)</summary>
    protected abstract float SortAdvanceDelay { get; }

    /// <summary>STT 마이크 인디케이터 (음성으로 "생각"/"사실" 선택 가능)</summary>
    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;  // 대사 시퀀서 (enter/completed + 카드별 대사)

    [Header("피드백 TextId")]
    [SerializeField] private int correctThoughtTextId = 101010019;  // "생각" 정답 피드백 텍스트 ID
    [SerializeField] private int correctFactTextId = 101010018;     // "사실" 정답 피드백 텍스트 ID
    [SerializeField] private int wrongFeedbackTextId;               // 오답 피드백 텍스트 ID
    [SerializeField] private float wrongFeedbackDuration = 2f;      // 오답 피드백 표시 시간 (초)

    /// <summary>분류 UI 전체를 감싸는 루트 오브젝트 (요약 패널 전환 시 숨김)</summary>
    protected abstract GameObject StepRoot { get; }

    /// <summary>요약 패널 루트 (분류 완료 후 활성화)</summary>
    protected abstract GameObject SummaryPanelRoot { get; }

    // ===== 내부 상태 필드 =====

    /// <summary>대사 재생 중 상호작용 잠금 플래그</summary>
    private bool _interactionLocked = true;

    /// <summary>첫 대사 텍스트가 표시되기 전까지 카드 생성을 지연하는 플래그</summary>
    private bool _deferFirstCard;

    /// <summary>현재 화면에 표시 중인 필름 카드 인스턴스</summary>
    private GameObject _currentFilmInstance;

    /// <summary>현재 필름 카드의 애니메이터 컴포넌트</summary>
    private Director_Problem1_Step3_FilmCardAnimator _currentFilmAnimator;

    /// <summary>다음 카드로 진행 중인지 여부 (중복 입력 방지)</summary>
    private bool _isAdvancing;

    /// <summary>이미 분류된 필름 ID 집합 (중복 분류 방지)</summary>
    private readonly HashSet<int> _sortedFilmIds = new HashSet<int>();

    /// <summary>분류 결과 로그 리스트 (DB 저장용)</summary>
    private readonly List<SortLogEntry> _logs = new List<SortLogEntry>();

    /// <summary>"생각" 슬롯에 배치된 필름 인스턴스 목록</summary>
    private readonly List<GameObject> _placedThoughtFilmInstances = new List<GameObject>();

    /// <summary>"사실" 슬롯에 배치된 필름 인스턴스 목록</summary>
    private readonly List<GameObject> _placedFactFilmInstances = new List<GameObject>();

    /// <summary>오답 피드백 임시 메시지 코루틴 참조 (중복 실행 방지/정리용)</summary>
    private Coroutine _hanamiTempMessageCoroutine;

    /// <summary>현재 오답 피드백 메시지를 표시 중인지 여부</summary>
    private bool _isShowingHanamiWrongMessage;

    /// <summary>활성 DOTween 트윈 목록 (정리용)</summary>
    private readonly List<Tween> _activeTweens = new List<Tween>();

    // ===== 연출 파라미터 상수 =====
    private const float MoveAnimDuration = 0.28f;   // 카드 슬롯 이동 애니메이션 시간
    private const float MovePunchScale = 0.92f;      // 카드 이동 시 스케일 펀치 값

    /// <summary>RandomCardSequenceStepBase에서 요구하는 총 카드 수</summary>
    protected override int CardCount => FilmCount;

    /// <summary>
    /// 카드 시퀀스 리셋 시 호출. 모든 내부 상태 초기화, STT 이벤트 구독,
    /// 배치된 필름 인스턴스 정리, 대사 시퀀서 이벤트 바인딩 등을 수행한다.
    /// </summary>
    protected override void OnSequenceReset()
    {
        _isAdvancing = false;
        _sortedFilmIds.Clear();
        _logs.Clear();

        StopWrongFeedbackCoroutineIfRunning();
        _isShowingHanamiWrongMessage = false;

        KillAllTweens();

        if (MicIndicator != null)
        {
            MicIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
            MicIndicator.OnKeywordMatched += OnSTTKeywordMatched;
            MicIndicator.SetRecording(false);
        }

        ClearPlacedFilmInstances();

        if (SummaryPanelRoot != null)
            SummaryPanelRoot.SetActive(false);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(false);

        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(true);

        DestroyCurrentFilmCard();

        _interactionLocked = true;
        if (dialogueSequencer != null)
        {
            // enter 시퀀스의 쪽수에 film 수 반영
            dialogueSequencer.SetExtraPageCount(FilmCount);

            // 첫 대사가 표시될 때까지 카드 생성을 지연
            _deferFirstCard = true;
            dialogueSequencer.OnFirstTextShown += OnDialogueFirstTextShown;
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        }
        else
        {
            _deferFirstCard = false;
            _interactionLocked = false;
        }
    }

    /// <summary>첫 대사 텍스트 표시 콜백 → 지연되었던 첫 카드를 생성한다.</summary>
    private void OnDialogueFirstTextShown()
    {
        _deferFirstCard = false;
        UpdateCurrentCardUI();
    }

    /// <summary>
    /// enter 대사 시퀀스 완료 콜백 → 상호작용 잠금 해제 + 현재 카드의 하남이 대사 표시.
    /// </summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;

        // enter 시퀀스 완료 후 현재 카드의 하남이 대사 표시
        if (dialogueSequencer != null)
        {
            int logicalIndex = GetCurrentLogicalIndex();
            if (logicalIndex >= 0)
            {
                int hanamiTextId = GetFilmHanamiTextId(logicalIndex);
                if (hanamiTextId > 0)
                    SetTextWithFilmPage(hanamiTextId, logicalIndex);
            }
        }
    }

    /// <summary>전체 페이지 수 (enter 대사 수 + 필름 카드 수)</summary>
    private int TotalPages => dialogueSequencer != null
        ? dialogueSequencer.EnterTextCount + FilmCount
        : FilmCount;

    /// <summary>필름 인덱스를 페이지 번호로 변환하여 대사 시퀀서에 텍스트를 설정한다.</summary>
    private void SetTextWithFilmPage(int textId, int filmIndex)
    {
        int enterCount = (dialogueSequencer != null) ? dialogueSequencer.EnterTextCount : 0;
        int currentPage = enterCount + filmIndex + 1;
        dialogueSequencer.SetText(textId, currentPage, TotalPages);
    }

    /// <summary>스텝 퇴장 시 호출. 이벤트 구독 해제, 상호작용 잠금.</summary>
    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
        {
            dialogueSequencer.OnFirstTextShown -= OnDialogueFirstTextShown;
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;
        }

        _interactionLocked = true;

        if (MicIndicator != null)
            MicIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
    }

    /// <summary>
    /// STT 키워드 매칭 콜백. index=0이면 "생각", index=1이면 "사실"로 분류 시도.
    /// </summary>
    private void OnSTTKeywordMatched(int index)
    {
        // 0=생각, 1=사실
        bool userChoseThought = (index == 0);
        HandleSort(userChoseThought);
    }

    /// <summary>
    /// RandomCardSequenceStepBase에서 카드 표시 요청 시 호출.
    /// 해당 인덱스의 필름 카드를 생성/갱신하고, 등장 애니메이션 재생 + 하남이 대사 표시.
    /// </summary>
    protected override void OnApplyCardToUI(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= FilmCount)
        {
            DestroyCurrentFilmCard();

            if (AnswerButtonsRoot != null)
                AnswerButtonsRoot.SetActive(false);

            return;
        }

        // 첫 대사 표시 전이면 카드 생성 스킵 (OnFirstTextShown에서 호출됨)
        if (_deferFirstCard)
            return;

        string text = GetFilmText(logicalIndex);
        Sprite sprite = GetFilmSprite(logicalIndex);

        SpawnOrUpdateCurrentFilmCard(text, sprite);

        // 카드별 하남이 대사 (enter 시퀀스 완료 후에만)
        if (!_interactionLocked && !_isShowingHanamiWrongMessage && dialogueSequencer != null)
        {
            int hanamiTextId = GetFilmHanamiTextId(logicalIndex);
            if (hanamiTextId > 0)
                SetTextWithFilmPage(hanamiTextId, logicalIndex);
        }

        if (_currentFilmAnimator != null)
            _currentFilmAnimator.PlayEnter();

        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(true);
    }

    /// <summary>현재 카드 UI 정리. 분류 시 슬롯으로 이동시키므로 여기서는 버튼만 숨긴다.</summary>
    protected override void OnClearCurrentCardUI()
    {
        // 현재 카드는 분류 시 슬롯으로 이동시키므로 여기서 삭제하지 않음
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);
    }

    protected override void OnCardProcessed(int logicalIndex)
    {
    }

    /// <summary>
    /// 모든 카드 처리 완료 시 호출. 버튼 숨기고, 요약 버튼 표시, completed 대사 재생.
    /// </summary>
    protected override void OnAllCardsProcessed()
    {
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);

        bool allSorted = (_logs.Count >= FilmCount);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(allSorted);

        if (allSorted)
        {
            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();

            Debug.Log("[Director_Problem1_Step3_Logic] 모든 필름 분류 완료.");
        }
        else
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] OnAllCardsProcessed 호출됨, 하지만 분류 로그가 부족함. logs="
                             + _logs.Count + ", films=" + FilmCount);
        }
    }

    /// <summary>
    /// 현재 필름 카드를 생성하거나 텍스트/이미지를 갱신한다.
    /// 최초 호출 시 프리팹 인스턴스화, 이후에는 기존 인스턴스의 내용만 업데이트.
    /// </summary>
    private void SpawnOrUpdateCurrentFilmCard(string text, Sprite sprite)
    {
        if (CurrentFilmRoot == null || CurrentFilmPrefab == null)
            return;

        if (_currentFilmInstance == null)
        {
            // text 자식 찾아서 idx 기반 넣어주면됨
            _currentFilmInstance = UnityEngine.Object.Instantiate(CurrentFilmPrefab, CurrentFilmRoot, false);
            _currentFilmAnimator = _currentFilmInstance.GetComponent<Director_Problem1_Step3_FilmCardAnimator>();
        }
        else
        {
            if (_currentFilmInstance.transform.parent != CurrentFilmRoot)
            {
                _currentFilmInstance.transform.SetParent(CurrentFilmRoot, false);
                ResetToLocalIdentity(_currentFilmInstance.transform);
            }
        }

        // GetComponentInChildren은 root GO도 포함하므로, 자식 전용으로 탐색
        Text uiText = null;
        foreach (var t in _currentFilmInstance.GetComponentsInChildren<Text>(true))
        {
            if (t.gameObject != _currentFilmInstance) { uiText = t; break; }
        }
        if (uiText != null)
            uiText.text = text ?? string.Empty;

        // Image는 root에 있으므로 root에서 직접 가져옴
        Image uiImage = _currentFilmInstance.GetComponent<Image>();
        if (uiImage != null && sprite != null)
            uiImage.sprite = sprite;
    }

    /// <summary>현재 필름 카드 인스턴스를 파괴하고 참조를 null로 정리한다.</summary>
    private void DestroyCurrentFilmCard()
    {
        if (_currentFilmInstance != null)
        {
            UnityEngine.Object.Destroy(_currentFilmInstance);
            _currentFilmInstance = null;
            _currentFilmAnimator = null;
        }
    }

    /// <summary>생각/사실 슬롯에 배치된 모든 필름 인스턴스를 파괴하고 리스트를 비운다.</summary>
    private void ClearPlacedFilmInstances()
    {
        for (int i = 0; i < _placedThoughtFilmInstances.Count; i++)
        {
            if (_placedThoughtFilmInstances[i] != null)
                UnityEngine.Object.Destroy(_placedThoughtFilmInstances[i]);
        }
        _placedThoughtFilmInstances.Clear();

        for (int i = 0; i < _placedFactFilmInstances.Count; i++)
        {
            if (_placedFactFilmInstances[i] != null)
                UnityEngine.Object.Destroy(_placedFactFilmInstances[i]);
        }
        _placedFactFilmInstances.Clear();
    }

    /// <summary>오답 피드백 메시지를 일시적으로 표시하는 코루틴을 시작한다.</summary>
    private void ShowWrongFeedbackTemp(int logicalIndex)
    {
        if (dialogueSequencer == null || wrongFeedbackTextId <= 0) return;

        StopWrongFeedbackCoroutineIfRunning();
        _hanamiTempMessageCoroutine = StartCoroutine(CoWrongFeedbackTemp(logicalIndex));
    }

    private IEnumerator CoWrongFeedbackTemp(int logicalIndex)
    {
        _isShowingHanamiWrongMessage = true;

        SetTextWithFilmPage(wrongFeedbackTextId, logicalIndex);

        float wait = Mathf.Max(0f, wrongFeedbackDuration);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        _isShowingHanamiWrongMessage = false;

        // 오답 후 다시 현재 카드의 하남이 대사로 복귀
        int hanamiTextId = GetFilmHanamiTextId(logicalIndex);
        if (hanamiTextId > 0)
            SetTextWithFilmPage(hanamiTextId, logicalIndex);

        _hanamiTempMessageCoroutine = null;
    }

    private void StopWrongFeedbackCoroutineIfRunning()
    {
        if (_hanamiTempMessageCoroutine != null)
        {
            StopCoroutine(_hanamiTempMessageCoroutine);
            _hanamiTempMessageCoroutine = null;
        }
    }

    /// <summary>활성 DOTween 트윈을 모두 중지하고 리스트를 비운다.</summary>
    private void KillAllTweens()
    {
        for (int i = 0; i < _activeTweens.Count; i++)
        {
            if (_activeTweens[i] != null && _activeTweens[i].active)
                _activeTweens[i].Kill();
        }
        _activeTweens.Clear();
    }

    /// <summary>"생각" 버튼 클릭 핸들러 (인스펙터에서 Button.onClick에 연결)</summary>
    public void OnClickSortThought()
    {
        HandleSort(true);
    }

    /// <summary>"사실" 버튼 클릭 핸들러 (인스펙터에서 Button.onClick에 연결)</summary>
    public void OnClickSortFact()
    {
        HandleSort(false);
    }

    /// <summary>
    /// 분류 처리 핵심 메서드. 사용자가 "생각" 또는 "사실"을 선택했을 때 호출된다.
    /// 오답이면 피드백 표시, 정답이면 카드를 해당 슬롯으로 이동시키고 다음 카드로 진행한다.
    /// </summary>
    /// <param name="userChoseThought">사용자가 "생각"을 선택했으면 true</param>
    private void HandleSort(bool userChoseThought)
    {
        if (_interactionLocked) return;
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0) return;
        if (_isAdvancing) return;
        if (_isShowingHanamiWrongMessage) return; // 오답 메시지 표시 중 입력 차단
        if (logicalIndex >= FilmCount) return;
        if (_currentFilmInstance == null) return;

        bool correctIsThought = IsFilmThought(logicalIndex);
        bool userCorrect = (userChoseThought == correctIsThought);

        // 오답: 메시지만 표시하고 다시 시도하게 함
        if (!userCorrect)
        {
            ShowWrongFeedbackTemp(logicalIndex);
            return;
        }

        // 정답 처리
        int filmId = GetFilmId(logicalIndex);
        string text = GetFilmText(logicalIndex);

        if (_sortedFilmIds.Contains(filmId))
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 이미 분류된 필름 재처리 시도: filmId=" + filmId);
            return;
        }

        _sortedFilmIds.Add(filmId);

        string correctType = correctIsThought ? "생각" : "사실";

        var entry = new SortLogEntry
        {
            filmId = filmId,
            text = text,
            correctType = correctType,
            chosenType = correctType
        };
        _logs.Add(entry);

        PlaceCurrentFilmIntoCorrectSlot(correctIsThought);

        // 정답 피드백 표시
        if (dialogueSequencer != null)
        {
            int feedbackId = correctIsThought ? correctThoughtTextId : correctFactTextId;
            if (feedbackId > 0)
                SetTextWithFilmPage(feedbackId, logicalIndex);
        }

        _isAdvancing = true;
        StartCoroutine(AdvanceAfterDelayWithAnimation(true));
    }

    /// <summary>
    /// 정답 분류된 현재 필름 카드를 해당 슬롯(생각/사실)으로 DOTween 애니메이션으로 이동시킨다.
    /// 이동 후 카드 참조를 끊어 다음 카드 생성이 가능하도록 한다.
    /// </summary>
    private void PlaceCurrentFilmIntoCorrectSlot(bool correctIsThought)
    {
        Transform[] slots = correctIsThought ? ThoughtSlots : FactSlots;
        List<GameObject> placedList = correctIsThought ? _placedThoughtFilmInstances : _placedFactFilmInstances;

        if (_currentFilmInstance == null)
            return;

        int nextSlotIndex = placedList.Count;

        if (slots == null || nextSlotIndex < 0 || nextSlotIndex >= slots.Length || slots[nextSlotIndex] == null)
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 배치 슬롯 부족/누락. "
                             + (correctIsThought ? "Thought" : "Fact")
                             + " slotIndex=" + nextSlotIndex + " / slots=" + (slots == null ? 0 : slots.Length));

            _currentFilmInstance.SetActive(false);
            placedList.Add(_currentFilmInstance);
            _currentFilmInstance = null;
            _currentFilmAnimator = null;
            return;
        }

        Transform slot = slots[nextSlotIndex];
        GameObject placedGo = _currentFilmInstance;
        Transform placedTr = placedGo.transform;

        // 현재 카드용 참조 끊기 (다음 카드 생성될 수 있도록)
        _currentFilmInstance = null;
        _currentFilmAnimator = null;

        // 월드 좌표 유지한 상태로 슬롯으로 이동 후, DOTween으로 슬롯 로컬 원점까지 이동
        placedTr.SetParent(slot, true);

        // 시작값 약간 강조 (선택 직후 연출)
        placedTr.DOComplete();
        Sequence seq = DOTween.Sequence();

        // scale punch 느낌
        seq.Join(placedTr.DOScale(MovePunchScale, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(placedTr.DOLocalMove(Vector3.zero, MoveAnimDuration).SetEase(Ease.OutCubic));
        seq.Join(placedTr.DOLocalRotate(Vector3.zero, MoveAnimDuration).SetEase(Ease.OutCubic));
        seq.Join(placedTr.DOScale(Vector3.one, MoveAnimDuration).SetEase(Ease.OutBack));

        _activeTweens.Add(seq);

        placedList.Add(placedGo);
    }

    /// <summary>Transform의 로컬 위치/회전/스케일을 기본값으로 리셋한다.</summary>
    private void ResetToLocalIdentity(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    /// <summary>
    /// 분류 후 딜레이를 두고, 카드 퇴장 애니메이션 재생 후 다음 카드로 진행하는 코루틴.
    /// </summary>
    private IEnumerator AdvanceAfterDelayWithAnimation(bool userCorrect)
    {
        float delay = Mathf.Max(0f, SortAdvanceDelay);

        // 오답이면 오답 문구 유지 보장
        if (!userCorrect)
            delay = Mathf.Max(delay, Mathf.Max(0f, wrongFeedbackDuration));

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 현재 카드는 이미 슬롯으로 이동되어 _currentFilmAnimator == null 일 수 있음
        if (_currentFilmAnimator != null)
            yield return StartCoroutine(_currentFilmAnimator.PlayExit());

        _isAdvancing = false;
        CompleteCurrentCard();
    }

    /// <summary>마이크 버튼 클릭 핸들러 → STT 녹음 토글</summary>
    public void OnClickMic()
    {
        if (MicIndicator != null)
            MicIndicator.ToggleRecording();
    }

    /// <summary>
    /// 요약 버튼 클릭 핸들러. 분류 결과를 DB에 저장하고,
    /// StepRoot를 숨기고 SummaryPanelRoot를 표시한다.
    /// </summary>
    public void OnClickSummaryButton()
    {
        if (_logs.Count < FilmCount)
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 아직 분류 미완료. logs=" + _logs.Count + ", films=" + FilmCount);
            return;
        }

        SaveSortLogToDb();

        if (StepRoot != null)
            StepRoot.SetActive(false);

        if (SummaryPanelRoot != null)
            SummaryPanelRoot.SetActive(true);
    }

    /// <summary>분류 로그를 SaveAttempt를 통해 DB에 저장한다.</summary>
    private void SaveSortLogToDb()
    {
        if (_logs.Count == 0)
        {
            Debug.Log("[Director_Problem1_Step3_Logic] 저장할 로그가 없어 DB 저장 스킵");
            return;
        }

        var body = new SortLogPayload { items = _logs.ToArray() };
        SaveAttempt(body);
    }
}