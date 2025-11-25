using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StressCardSlot
{
    [Header("논리 데이터")]
    public string id;          // headache, heartbeat, ...
    public string label;       // 두통, 심장 두근거림 ...
    public string category;    // 신체적 / 정서적 / 행동적

    [Header("UI 참조")]
    public Button button;          // 카드 전체 버튼
    public Text labelText;         // 카드 안 텍스트
    public Text categoryText;      // 카테고리 텍스트
    public Image backgroundImage;  // 카드 배경 Image (색 바꿀 대상)
}

[Serializable]
public class StudioLightSlot
{
    [Header("조명 이미지 (한 장으로 처리)")]
    public Image image;

    [Header("불안정(깜빡) 상태 색상")]
    public Color unstableColor = new Color(1f, 0.54f, 0.24f, 0.7f);   // 대충 #FF8A3D

    [Header("안정(고정) 상태 색상")]
    public Color stableColor = new Color(1f, 0.84f, 0f, 1f);         // 대충 #FFD700

    [Header("깜빡임 설정")]
    public float flickerSpeed = 2f;
    [Range(0f, 1f)] public float minAlpha = 0.2f;
    [Range(0f, 1f)] public float maxAlpha = 1f;

    [HideInInspector] public float phaseOffset;   // 각 조명 별 위상 랜덤
}

/// <summary>
/// Director / Part6 / Step2 (스트레스 반응 카드 선택) 로직 베이스.
/// - 카드 2~4개 선택.
/// - 선택 개수에 비례해서 위쪽 조명들이 하나씩 안정화.
/// - 카드 선택으로 Gate만 열고,
///   실제 DB 저장은 completeRoot 안 "다음" 버튼에서 SaveSelectionToDB()로 한 번만 호출.
/// </summary>
public abstract class Director_Problem6_Step2_Logic : ProblemStepBase
{
    // ===== 자식에서 넘겨줄 추상 프로퍼티들 =====

    /// <summary>스트레스 카드 슬롯들 (8개)</summary>
    protected abstract StressCardSlot[] Cards { get; }

    /// <summary>위쪽 조명들 (4개)</summary>
    protected abstract StudioLightSlot[] Lights { get; }

    /// <summary> "2~4개의 카드를 선택하세요" 같은 안내 텍스트 (옵션)</summary>
    protected abstract Text ProgressLabelUI { get; }

    /// <summary>완료 게이트 (completeRoot 안에 버튼 있음)</summary>
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    // ===== 설정값 =====

    protected virtual int MinSelectCount => 2;
    protected virtual int MaxSelectCount => 4;

    protected virtual Color CardNormalColor =>
        Color.white;

    protected virtual Color CardSelectedColor =>
        new Color(1f, 0.54f, 0.24f, 1f); // #FF8A3D 느낌

    // ===== 내부 상태 =====

    private bool[] _selectedFlags;
    private int _selectedCount;
    private bool _initialized;
    private bool _gateCompleted;   // MarkOneDone 한 번만 호출하기 위한 플래그
    private bool _savedToDB;       // SaveSelectionToDB 한 번만 호출하기 위한 플래그

    // =========================================================
    // Step Lifecycle (ProblemStepBase)
    // =========================================================

    /// <summary>
    /// ProblemStepBase.OnEnable → 여기 OnStepEnter 호출됨.
    /// </summary>
    protected override void OnStepEnter()
    {
        ResetGate();
        InitializeLightPhases();
        InitializeIfNeeded();
    }

    protected override void OnStepExit()
    {
        RemoveCardListeners();
    }

    // 매 프레임 조명 깜빡임/안정화
    private void Update()
    {
        UpdateLightsVisual();
    }

    // =========================================================
    // Gate 초기화
    // =========================================================

    private void ResetGate()
    {
        var gate = StepCompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);   // 이 스텝에서 필요한 완료 카운트 = 1

        _gateCompleted = false;
        _savedToDB = false;
    }

    private void InitializeLightPhases()
    {
        var lights = Lights;
        if (lights == null) return;

        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].phaseOffset = UnityEngine.Random.Range(0f, 10f);
        }
    }

    // =========================================================
    // 초기화 & 카드 UI 세팅
    // =========================================================

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
        UpdateProgressLabel();

        _initialized = true;
    }

    private void SetupCardUI()
    {
        RemoveCardListeners();

        var cards = Cards;
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            var slot = cards[i];

            // 텍스트 세팅
            if (slot.labelText != null)
                slot.labelText.text = slot.label;

            if (slot.categoryText != null)
                slot.categoryText.text = slot.category;

            // 색 초기화
            if (slot.backgroundImage != null)
                slot.backgroundImage.color = CardNormalColor;

            // 버튼 리스너
            if (slot.button != null)
                slot.button.onClick.AddListener(() => OnClickCard(index));
        }
    }

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

    private void OnClickCard(int index)
    {
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
        UpdateProgressLabel();
        TryOpenGateOnce();   // 여기서는 Gate만 열고, DB는 안 건드림
    }

    private void UpdateCardVisuals()
    {
        var cards = Cards;
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            var slot = cards[i];
            bool isSelected = _selectedFlags != null && _selectedFlags[i];

            if (slot.backgroundImage != null)
                slot.backgroundImage.color = isSelected ? CardSelectedColor : CardNormalColor;
        }
    }

    private void UpdateProgressLabel()
    {
        var label = ProgressLabelUI;
        if (label == null) return;

        label.text = $"{MinSelectCount}~{MaxSelectCount}개의 카드를 선택하세요 ({_selectedCount}/{MaxSelectCount} 선택됨)";
    }

    // =========================================================
    // 조명: Image 한 장으로 깜빡임/안정화
    // =========================================================

    private void UpdateLightsVisual()
    {
        var lights = Lights;
        if (lights == null || lights.Length == 0)
            return;

        int stabilizedLights = Mathf.Clamp(_selectedCount, 0, lights.Length);
        float time = Time.time;

        for (int i = 0; i < lights.Length; i++)
        {
            var slot = lights[i];
            var img = slot.image;
            if (img == null) continue;

            if (i < stabilizedLights)
            {
                // 안정 상태: stableColor + alpha=1
                Color c = slot.stableColor;
                c.a = 1f;
                img.color = c;
            }
            else
            {
                // 불안정 상태: sin 기반으로 alpha 깜빡
                float phase = time * slot.flickerSpeed + slot.phaseOffset;
                float sin = (Mathf.Sin(phase) + 1f) * 0.5f;   // 0~1
                float alpha = Mathf.Lerp(slot.minAlpha, slot.maxAlpha, sin);

                Color c = slot.unstableColor;
                c.a = alpha;
                img.color = c;
            }
        }
    }

    // =========================================================
    // Gate 완료 (한 번만, DB는 여기서 X)
    // =========================================================

    private void TryOpenGateOnce()
    {
        if (_gateCompleted) return;
        if (_selectedCount < MinSelectCount) return;

        var gate = StepCompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();   // completeRoot ON

        _gateCompleted = true;
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
                        label = slot.label,
                        category = slot.category
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
