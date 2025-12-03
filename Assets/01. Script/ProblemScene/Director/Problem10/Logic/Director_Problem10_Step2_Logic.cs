using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step2 로직 베이스
/// - 4개 장르 카드 중 하나 선택
/// - 선택 후 확인 버튼 → Gate 완료
/// </summary>
public abstract class Director_Problem10_Step2_Logic : ProblemStepBase
{
    #region Data Classes

    [Serializable]
    public class GenreData
    {
        public string id;           // growth, warmth, contribution, family
        public string name;         // 성장 드라마, 따뜻한 휴먼 코미디, ...
        public string emoji;        // 🌱, 🌈, 🌍, 🏡
        [TextArea(1, 2)]
        public string description;  // 계속 배우고 발전하는 나, ...
    }

    [Serializable]
    public class GenreCardUI
    {
        public Button button;
        public GameObject selectedIndicator;  // 선택 시 표시되는 체크 표시
        public Image backgroundImage;         // 배경 색상 변경용 (선택적)
    }

    // DB 저장용 DTO
    [Serializable]
    public class GenreSelectionDto
    {
        public string stepKey;
        public string selectedGenreId;
        public string selectedGenreName;
        public DateTime selectedAt;
    }

    #endregion

    #region Abstract Properties

    [Header("===== 장르 데이터 =====")]
    protected abstract GenreData[] Genres { get; }

    [Header("===== 화면 루트 =====")]
    protected abstract GameObject SelectionRoot { get; }

    [Header("===== 장르 카드 UI (4개) =====")]
    protected abstract GenreCardUI[] GenreCards { get; }

    [Header("===== 확인 버튼 =====")]
    protected abstract Button ConfirmButton { get; }

    [Header("===== 완료 게이트 =====")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    /// <summary>선택된 카드 배경 색상</summary>
    protected virtual Color SelectedColor => new Color(1f, 0.54f, 0.24f, 0.3f); // Orange 30%

    /// <summary>기본 카드 배경 색상</summary>
    protected virtual Color NormalColor => new Color(1f, 1f, 1f, 0.1f); // White 10%

    #endregion

    // 내부 상태
    private int _selectedIndex = -1;

    #region Step Lifecycle

    protected override void OnStepEnter()
    {
        _selectedIndex = -1;

        // Gate 초기화
        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        // 초기 화면 설정
        if (SelectionRoot != null) SelectionRoot.SetActive(true);

        // 확인 버튼 비활성화
        if (ConfirmButton != null)
            ConfirmButton.interactable = false;

        // 모든 선택 표시 숨기기
        UpdateSelectionVisuals();

        RegisterListeners();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        RemoveListeners();
    }

    #endregion

    #region UI Control

    private void UpdateSelectionVisuals()
    {
        var cards = GenreCards;
        if (cards == null) return;

        for (int i = 0; i < cards.Length; i++)
        {
            var card = cards[i];
            if (card == null) continue;

            bool isSelected = i == _selectedIndex;

            // 선택 표시
            if (card.selectedIndicator != null)
                card.selectedIndicator.SetActive(isSelected);

            // 배경 색상 (선택적)
            if (card.backgroundImage != null)
                card.backgroundImage.color = isSelected ? SelectedColor : NormalColor;
        }
    }

    #endregion

    #region Listeners

    private void RegisterListeners()
    {
        var cards = GenreCards;
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (card?.button != null)
                {
                    int index = i; // 클로저용
                    card.button.onClick.RemoveAllListeners();
                    card.button.onClick.AddListener(() => OnGenreSelected(index));
                }
            }
        }

        if (ConfirmButton != null)
        {
            ConfirmButton.onClick.RemoveAllListeners();
            ConfirmButton.onClick.AddListener(OnConfirmClicked);
        }
    }

    private void RemoveListeners()
    {
        var cards = GenreCards;
        if (cards != null)
        {
            foreach (var card in cards)
                card?.button?.onClick.RemoveAllListeners();
        }

        ConfirmButton?.onClick.RemoveAllListeners();
    }

    #endregion

    #region Event Handlers

    private void OnGenreSelected(int index)
    {
        _selectedIndex = index;

        // 시각적 업데이트
        UpdateSelectionVisuals();

        // 확인 버튼 활성화
        if (ConfirmButton != null)
            ConfirmButton.interactable = true;

        // 선택 콜백
        OnGenreSelectedVisual(index);
    }

    private void OnConfirmClicked()
    {
        if (_selectedIndex < 0) return;

        var genres = Genres;
        if (genres == null || _selectedIndex >= genres.Length) return;

        var selected = genres[_selectedIndex];

        // DB 저장
        SaveAttempt(new GenreSelectionDto
        {
            stepKey = context != null ? context.CurrentStepKey : null,
            selectedGenreId = selected.id,
            selectedGenreName = selected.name,
            selectedAt = DateTime.UtcNow
        });

        // SelectionRoot 숨기기
        if (SelectionRoot != null) SelectionRoot.SetActive(false);

        // Gate 완료 → completeRoot 자동 표시
        var gate = CompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();
    }

    #endregion

    #region Virtual Callbacks

    /// <summary>장르 선택 시 호출 (파생 클래스에서 override 가능)</summary>
    protected virtual void OnGenreSelectedVisual(int index)
    {
        // 선택 애니메이션 등 추가 가능
    }

    #endregion

    #region Public Getters (다음 Step에서 사용 가능)

    /// <summary>선택된 장르 데이터 반환</summary>
    public GenreData GetSelectedGenre()
    {
        var genres = Genres;
        if (genres == null || _selectedIndex < 0 || _selectedIndex >= genres.Length)
            return null;

        return genres[_selectedIndex];
    }

    #endregion
}
