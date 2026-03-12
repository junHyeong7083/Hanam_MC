using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem10_Step2_Logic - 문제10 스텝2 장르 선택 로직 (추상 클래스)
///
/// 【역할】 4개의 영화 장르 카드 중 하나를 선택하는 메인 활동.
///          선택 후 딜레이를 거쳐 CompleteRoot에 선택한 장르의 이미지/텍스트를 표시하고,
///          Problem10SharedData에 선택 결과를 저장하여 Step3에서 참조할 수 있게 한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층.
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝2 (메인 활동 - 장르 선택)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem10_Step2 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, Problem10SharedData
/// 【흐름】 스텝 진입 → 대화 재생 → 장르 4개 중 1개 선택 → SelectIndicator 표시
///         → 딜레이 후 CompleteRoot로 전환 → SharedData에 저장 → DB 저장 + 완료
/// </summary>
public abstract class Director_Problem10_Step2_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    /// <summary>장르 카드 한 장의 데이터 (라벨 textId, 스프라이트)</summary>
    [Serializable]
    public class GenreCardData
    {
        public int labelTextId;       // 장르명 CSV textId (예: 101101001~004)
        public Sprite cardSprite;     // 장르 카드 스프라이트
    }

    /// <summary>장르 선택 결과 DTO (DB 저장용)</summary>
    [Serializable]
    private class GenreSelectionDto
    {
        public int selectedIndex;     // 선택된 장르 인덱스 (0~3)
        public string selectedGenre;  // 선택된 장르 이름
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>장르 카드 데이터 배열 (4개)</summary>
    protected abstract GenreCardData[] GenreCardsData { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서

    // ----- 선택 화면 -----
    /// <summary>장르 선택 UI 루트</summary>
    protected abstract GameObject SelectRoot { get; }
    /// <summary>장르 선택 버튼 배열 (4개)</summary>
    protected abstract Button[] GenreButtons { get; }
    /// <summary>선택 시 표시할 인디케이터 배열 (4개)</summary>
    protected abstract GameObject[] SelectIndicators { get; }
    /// <summary>장르 라벨 텍스트 배열 (4개)</summary>
    protected abstract Text[] GenreLabels { get; }

    // ----- 완료 화면 -----
    /// <summary>완료 UI 루트</summary>
    protected abstract GameObject CompleteRoot { get; }
    /// <summary>완료 화면에 표시할 선택된 장르 카드 이미지</summary>
    protected abstract Image CompleteCardImage { get; }
    /// <summary>완료 화면에 표시할 선택된 장르 라벨</summary>
    protected abstract Text CompleteCardLabel { get; }

    // ----- 공유 데이터 -----
    /// <summary>Step3에서 선택 결과를 참조하기 위한 ScriptableObject 공유 데이터</summary>
    protected abstract Problem10SharedData SharedData { get; }

    #endregion

    #region Virtual Config

    /// <summary>장르 선택 후 CompleteRoot 전환까지의 딜레이 (초)</summary>
    protected virtual float TransitionDelay => 1.0f;

    #endregion

    // ===== 내부 상태 =====
    private bool _selected;                        // 장르 선택 완료 여부 (중복 방지)
    private Coroutine _transitionRoutine;          // 전환 코루틴 핸들
    private bool _interactionLocked = true;        // 대화 재생 중 상호작용 잠금

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>스텝 진입. 장르 라벨 설정, 인디케이터 초기화, 리스너 등록, 대화 재생 대기.</summary>
    protected override void OnStepEnter()
    {
        _selected = false;
        _interactionLocked = true;

        // 장르 라벨 설정
        var data = GenreCardsData;
        var labels = GenreLabels;
        if (data != null && labels != null)
        {
            for (int i = 0; i < labels.Length && i < data.Length; i++)
            {
                if (labels[i] != null && data[i].labelTextId > 0)
                    labels[i].text = ProblemRuntime.L(data[i].labelTextId);
            }
        }

        // SelectIndicator 모두 숨김
        var indicators = SelectIndicators;
        if (indicators != null)
        {
            foreach (var ind in indicators)
                if (ind != null) ind.SetActive(false);
        }

        // 루트 설정
        if (SelectRoot != null) SelectRoot.SetActive(true);
        if (CompleteRoot != null) CompleteRoot.SetActive(false);
        RegisterListeners();

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    /// <summary>대화 진입 완료 시 상호작용 잠금 해제.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>스텝 퇴장. 전환 코루틴 정지, 리스너 정리.</summary>
    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        RemoveListeners();
    }

    // =========================
    // 리스너 등록/해제
    // =========================

    /// <summary>장르 버튼에 클릭 리스너를 등록한다.</summary>
    private void RegisterListeners()
    {
        var buttons = GenreButtons;
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                int idx = i;
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => OnGenreClicked(idx));
            }
        }
    }

    /// <summary>장르 버튼 리스너를 제거한다.</summary>
    private void RemoveListeners()
    {
        var buttons = GenreButtons;
        if (buttons != null)
        {
            foreach (var btn in buttons)
                if (btn != null) btn.onClick.RemoveAllListeners();
        }
    }

    // =========================
    // 장르 선택
    // =========================

    /// <summary>장르 카드 클릭 시 호출. 인디케이터 표시 후 딜레이를 거쳐 CompleteRoot로 전환한다.</summary>
    private void OnGenreClicked(int index)
    {
        if (_interactionLocked) return;
        if (_selected) return;
        _selected = true;

        // SelectIndicator 표시
        var indicators = SelectIndicators;
        if (indicators != null)
        {
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].SetActive(i == index);
        }

        // 딜레이 후 전환
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionToComplete(index));
    }

    // =========================
    // 완료 화면 전환
    // =========================

    /// <summary>
    /// 딜레이 후 CompleteRoot로 전환하는 코루틴.
    /// 선택한 장르를 CompleteRoot에 반영하고, SharedData에 저장하며, DB에 기록한다.
    /// </summary>
    private IEnumerator TransitionToComplete(int index)
    {
        yield return new WaitForSeconds(TransitionDelay);

        var data = GenreCardsData;
        if (data != null && index < data.Length)
        {
            // CompleteRoot 카드에 선택한 장르 반영
            if (CompleteCardImage != null && data[index].cardSprite != null)
                CompleteCardImage.sprite = data[index].cardSprite;

            if (CompleteCardLabel != null && data[index].labelTextId > 0)
                CompleteCardLabel.text = ProblemRuntime.L(data[index].labelTextId);
        }

        // 루트 전환
        if (SelectRoot != null) SelectRoot.SetActive(false);
        if (CompleteRoot != null) CompleteRoot.SetActive(true);

        // 완료 처리: completedTextIds 재생 → NextStepBtn 표시
        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();

        // SharedData에 선택 결과 저장 (Step3에서 사용)
        if (SharedData != null && data != null && index < data.Length)
            SharedData.SetSelection(index, data[index].cardSprite);

        // DB 저장
        string genreName = (data != null && index < data.Length && data[index].labelTextId > 0)
            ? ProblemRuntime.L(data[index].labelTextId)
            : "";
        SaveAttempt(new GenreSelectionDto
        {
            selectedIndex = index,
            selectedGenre = genreName
        });

        _transitionRoutine = null;
    }
}
