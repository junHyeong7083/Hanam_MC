using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StressCardSlot - 스트레스 반응 카드 한 장의 데이터 및 UI 참조를 묶는 직렬화 가능 구조체.
/// Problem6 Step2에서 8장의 스트레스 반응 카드를 구성하는 데 사용된다.
/// </summary>
[Serializable]
public class StressCardSlot
{
    [Header("논리 데이터")]
    public string id;              // DB 저장용 카드 식별자 (예: "headache", "heartbeat")
    public int labelTextId;        // CSV textId (ProblemRuntime.L로 읽어 카드 라벨에 표시)

    [Header("UI 참조")]
    public Button button;          // 카드 전체를 감싸는 버튼 (클릭 이벤트 수신)
    public Text labelText;         // 카드 안에 표시되는 텍스트 컴포넌트
    public Image backgroundImage;  // 카드 배경 이미지 (선택/미선택 시 활성화 토글)

    [Header("선택 시 표시")]
    public GameObject selectImage;     // 선택 시 활성화되는 시각적 표시 오브젝트
}

/// <summary>
/// StudioLightSlot - 스튜디오 조명 한 개의 기본/클릭 상태 이미지를 묶는 직렬화 가능 구조체.
/// Problem6 Step2에서 카드 선택 개수에 비례하여 조명이 점등되는 UI를 구현한다.
/// </summary>
[Serializable]
public class StudioLightSlot
{
    [Header("조명 이미지 (기본 상태)")]
    public GameObject defaultImage;    // 조명이 꺼진 상태의 이미지 오브젝트

    [Header("클릭 이미지 (선택 시 표시)")]
    public GameObject clickedImage;    // 조명이 켜진 상태의 이미지 오브젝트
}

/// <summary>
/// Director_Problem6_Step2_Logic - 문제6 스텝2 스트레스 반응 카드 선택 로직 (추상 클래스)
///
/// 【역할】 8장의 스트레스 반응 카드 중 3장을 선택하는 메인 활동 스텝.
///          카드 선택 개수에 비례하여 상단 조명(3개)이 왼쪽부터 하나씩 켜지며,
///          3개 모두 선택 시 StepCompletionGate가 열린다.
///          DB 저장은 completeRoot 안 "다음" 버튼의 OnClick에서 SaveSelectionToDB()로 한 번만 호출.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. SerializeField는 Binder(Director_Problem6_Step2)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제6 > 스텝2 (메인 활동 - 카드 선택)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem6_Step2 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, StepCompletionGate, ProblemRuntime
/// 【흐름】 스텝 진입 → 대화 재생 완료 대기 → 카드 선택(토글) → 조명 업데이트
///         → 3개 선택 시 게이트 열림 + 완료 텍스트 표시 → "다음" 버튼으로 DB 저장
/// </summary>
public abstract class Director_Problem6_Step2_Logic : ProblemStepBase
{
    // ===== 자식(Binder)에서 넘겨줄 추상 프로퍼티들 =====

    /// <summary>스트레스 반응 카드 슬롯 배열 (인스펙터에서 8개 설정)</summary>
    protected abstract StressCardSlot[] Cards { get; }

    /// <summary>상단 스튜디오 조명 슬롯 배열 (인스펙터에서 3개 설정)</summary>
    protected abstract StudioLightSlot[] Lights { get; }

    /// <summary>스텝 완료 판정용 게이트 (completeRoot 안에 "다음" 버튼이 있음)</summary>
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    // ===== 설정값 (가상 프로퍼티로 파생 클래스에서 재정의 가능) =====

    /// <summary>게이트가 열리기 위한 최소 선택 카드 수 (기본 3)</summary>
    protected virtual int MinSelectCount => 3;

    /// <summary>선택 가능한 최대 카드 수 (기본 3)</summary>
    protected virtual int MaxSelectCount => 3;

    /// <summary>카드 미선택 시 배경 색상 (기본 흰색)</summary>
    protected virtual Color CardNormalColor =>
        Color.white;

    /// <summary>카드 선택 시 배경 색상 (주황색 #FF8A3D 계열)</summary>
    protected virtual Color CardSelectedColor =>
        new Color(1f, 0.54f, 0.24f, 1f); // #FF8A3D 느낌

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서 (진입/완료 대사 재생)

    // ===== 내부 상태 =====

    private bool _interactionLocked = true;   // 대화 재생 중 카드 클릭 잠금 플래그
    private bool[] _selectedFlags;            // 각 카드의 선택 여부 플래그 배열
    private int _selectedCount;               // 현재 선택된 카드 총 개수
    private bool _initialized;                // 초기화 완료 여부
    private bool _gateCompleted;              // 게이트 열림 상태 추적 (중복 호출 방지)
    private bool _savedToDB;                  // DB 저장 완료 여부 (중복 저장 방지)

    // =========================================================
    // Step Lifecycle (ProblemStepBase)
    // =========================================================

    /// <summary>
    /// 스텝 진입 시 호출. ProblemStepBase.OnEnable → OnStepEnter 순서.
    /// 게이트 리셋, 조명 초기화, 카드 UI 세팅 후 대화 재생이 끝날 때까지 상호작용을 잠근다.
    /// </summary>
    protected override void OnStepEnter()
    {
        ResetGate();
        InitializeLights();
        InitializeIfNeeded();

        // 대화 재생이 끝날 때까지 카드 클릭을 잠금
        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;

    }

    /// <summary>DialogueSequencer의 진입 대사 재생이 완료되면 상호작용 잠금을 해제한다.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>
    /// 스텝 퇴장 시 호출. 이벤트 구독 해제 및 버튼 리스너 정리.
    /// </summary>
    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

        RemoveCardListeners();
    }

    // =========================================================
    // Gate 초기화
    // =========================================================

    /// <summary>완료 게이트를 리셋하고 내부 상태 플래그를 초기화한다.</summary>
    private void ResetGate()
    {
        var gate = StepCompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);   // 이 스텝에서 필요한 완료 카운트 = 1

        _gateCompleted = false;
        _savedToDB = false;
    }

    /// <summary>모든 조명을 기본(꺼진) 상태로 초기화한다.</summary>
    private void InitializeLights()
    {
        var lights = Lights;
        if (lights == null) return;

        for (int i = 0; i < lights.Length; i++)
        {
            var slot = lights[i];
            if (slot.defaultImage != null)
                slot.defaultImage.SetActive(true);
            if (slot.clickedImage != null)
                slot.clickedImage.SetActive(false);
        }
    }

    // =========================================================
    // 초기화 & 카드 UI 세팅
    // =========================================================

    /// <summary>
    /// 카드 배열이 유효하면 선택 플래그를 초기화하고 카드 UI를 세팅한다.
    /// 이미 초기화된 경우에도 매번 선택 상태를 리셋한다.
    /// </summary>
    private void InitializeIfNeeded()
    {
        var cards = Cards;
        if (cards == null || cards.Length == 0)
        {
            Debug.LogWarning($"{name}: Cards 비어 있음");
            return;
        }

        if (_selectedFlags == null || _selectedFlags.Length != cards.Length)
            _selectedFlags = new bool[cards.Length];

        _selectedCount = 0;
        for (int i = 0; i < _selectedFlags.Length; i++)
            _selectedFlags[i] = false;

        SetupCardUI();

        _initialized = true;
    }

    /// <summary>
    /// 각 카드의 텍스트(CSV), 배경/선택 이미지 초기 상태, 버튼 리스너를 설정한다.
    /// </summary>
private void SetupCardUI()
    {
        RemoveCardListeners();

        var cards = Cards;
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;   // 클로저 캡처용 로컬 변수
            var slot = cards[i];

            // CSV에서 텍스트를 읽어 카드 라벨에 설정
            if (slot.labelText != null)
            {
                slot.labelText.text = slot.labelTextId > 0
                    ? ProblemRuntime.L(slot.labelTextId)
                    : "";
                slot.labelText.color = Color.black;
            }

            // 선택 전 초기 상태: backgroundImage 표시, selectImage 숨김
            if (slot.backgroundImage != null)
                slot.backgroundImage.gameObject.SetActive(true);
            if (slot.selectImage != null)
                slot.selectImage.SetActive(false);

            // 각 카드 버튼에 클릭 리스너 등록
            if (slot.button != null)
                slot.button.onClick.AddListener(() => OnClickCard(index));
        }
    }

    /// <summary>모든 카드 버튼의 클릭 리스너를 제거한다.</summary>
    private void RemoveCardListeners()
    {
        var cards = Cards;
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            var slot = cards[i];
            if (slot.button != null)
                slot.button.onClick.RemoveAllListeners();
        }
    }

    // =========================================================
    // 카드 클릭 처리
    // =========================================================

    /// <summary>
    /// 카드 클릭 시 호출. 선택/해제를 토글하고 UI와 게이트 상태를 갱신한다.
    /// 최대 선택 개수(MaxSelectCount)를 초과하면 새 선택을 무시한다.
    /// </summary>
    private void OnClickCard(int index)
    {
        if (_interactionLocked) return;
        var cards = Cards;
        if (!_initialized || cards == null) return;
        if (index < 0 || index >= cards.Length) return;

        bool currentlySelected = _selectedFlags[index];

        if (currentlySelected)
        {
            // 선택 해제
            _selectedFlags[index] = false;
            _selectedCount = Mathf.Max(0, _selectedCount - 1);
        }
        else
        {
            // 새로 선택 → 최대 개수 제한
            if (_selectedCount >= MaxSelectCount)
                return;

            _selectedFlags[index] = true;
            _selectedCount++;
        }

        UpdateCardVisuals();
        UpdateLightsVisual();
        UpdateGateState();   // 선택 개수에 따라 Gate 열기/닫기
    }

    /// <summary>
    /// 모든 카드의 시각 상태를 현재 선택 플래그에 맞게 갱신한다.
    /// 선택된 카드: backgroundImage OFF, selectImage ON / 미선택: 반대.
    /// </summary>
private void UpdateCardVisuals()
    {
        var cards = Cards;
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            var slot = cards[i];
            bool isSelected = _selectedFlags != null && _selectedFlags[i];

            // 선택 시: backgroundImage OFF, selectImage ON
            // 미선택 시: backgroundImage ON, selectImage OFF
            if (slot.backgroundImage != null)
                slot.backgroundImage.gameObject.SetActive(!isSelected);
            if (slot.selectImage != null)
                slot.selectImage.SetActive(isSelected);

            // 텍스트 색상: 항상 검정 유지
            if (slot.labelText != null)
                slot.labelText.color = Color.black;
        }
    }

    // =========================================================
    // 조명: 선택 개수만큼 왼쪽부터 이미지 교체
    // =========================================================

    /// <summary>
    /// 선택된 카드 개수만큼 왼쪽부터 조명을 켠다.
    /// 예: 2개 선택 시 조명[0], 조명[1]이 켜지고 조명[2]는 꺼짐.
    /// </summary>
    private void UpdateLightsVisual()
    {
        var lights = Lights;
        if (lights == null || lights.Length == 0)
            return;

        int litCount = Mathf.Clamp(_selectedCount, 0, lights.Length);

        for (int i = 0; i < lights.Length; i++)
        {
            var slot = lights[i];
            bool isLit = i < litCount;

            if (slot.defaultImage != null)
                slot.defaultImage.SetActive(!isLit);
            if (slot.clickedImage != null)
                slot.clickedImage.SetActive(isLit);
        }
    }

    // =========================================================
    // Gate 상태 업데이트 (선택 개수에 따라 열기/닫기)
    // =========================================================

    /// <summary>
    /// 현재 선택 카드 수에 따라 게이트를 열거나 닫는다.
    /// 조명이 모두 켜지면 completedText도 표시한다.
    /// 게이트 상태가 변경되면 MarkOneDone/MarkOneUndone을 호출한다.
    /// </summary>
    private void UpdateGateState()
    {
        var lights = Lights;
        bool allLightsLit = lights != null && _selectedCount >= lights.Length;

        // 조명 다 켜지면 완료 텍스트 표시
        if (allLightsLit && dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();

        // Gate: 최소 선택 수 이상이면 열기
        var gate = StepCompletionGateRef;
        if (gate == null) return;

        bool shouldBeCompleted = _selectedCount >= MinSelectCount;

        if (shouldBeCompleted && !_gateCompleted)
        {
            gate.MarkOneDone();
            _gateCompleted = true;
        }
        else if (!shouldBeCompleted && _gateCompleted)
        {
            gate.MarkOneUndone();
            _gateCompleted = false;
        }
    }

    // =========================================================
    // DB 저장: completeRoot 안 "다음" 버튼에서 OnClick으로 호출
    // =========================================================

    /// <summary>
    /// completeRoot의 다음 버튼에서 OnClick 이벤트로 호출.
    /// 현재 선택된 카드 목록을 DB에 한 번만 저장한다.
    /// </summary>
    public void SaveSelectionToDB()
    {
        if (_savedToDB) return;
        if (!_initialized) return;
        if (_selectedCount < MinSelectCount) return;

        var cards = Cards;
        var selectedList = new List<object>();

        if (cards != null && _selectedFlags != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (_selectedFlags[i])
                {
                    var slot = cards[i];
                    selectedList.Add(new
                    {
                        id = slot.id,
                        label = slot.labelTextId > 0 ? ProblemRuntime.L(slot.labelTextId) : ""
                    });
                }
            }
        }

        var body = new
        {
            selectedCount = _selectedCount,
            selectedResponses = selectedList.ToArray()
        };

        SaveAttempt(body);

        _savedToDB = true;
    }
}
