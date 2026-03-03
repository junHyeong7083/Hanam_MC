using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step2 로직 베이스
/// - 4개 장르 카드 중 하나 선택
/// - 선택 후 CompleteRoot에 선택한 장르 이미지/텍스트 표시
/// </summary>
public abstract class Director_Problem10_Step2_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class GenreCardData
    {
        public int labelTextId;       // 장르명 textId (101101001~004)
        public Sprite cardSprite;     // 장르 카드 스프라이트
    }

    [Serializable]
    private class GenreSelectionDto
    {
        public int selectedIndex;
        public string selectedGenre;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    protected abstract GenreCardData[] GenreCardsData { get; }

    // 하남박스
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId { get; }
    protected abstract int GuideTextId_Success { get; }
    protected abstract GameObject NextStepButtonRoot { get; }

    // 선택 화면
    protected abstract GameObject SelectRoot { get; }
    protected abstract Button[] GenreButtons { get; }
    protected abstract GameObject[] SelectIndicators { get; }
    protected abstract Text[] GenreLabels { get; }

    // 완료 화면
    protected abstract GameObject CompleteRoot { get; }
    protected abstract Image CompleteCardImage { get; }
    protected abstract Text CompleteCardLabel { get; }

    // 공유 데이터
    protected abstract Problem10SharedData SharedData { get; }

    #endregion

    #region Virtual Config

    protected virtual float TransitionDelay => 1.0f;

    #endregion

    // 내부 상태
    private bool _selected;
    private Coroutine _transitionRoutine;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _selected = false;

        // 가이드 텍스트
        if (GuideText != null && GuideTextId > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId);

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
        if (NextStepButtonRoot != null) NextStepButtonRoot.SetActive(false);

        RegisterListeners();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

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

    private void OnGenreClicked(int index)
    {
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

        // 성공 가이드
        if (GuideText != null && GuideTextId_Success > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Success);

        // NextStepBtn 활성화
        if (NextStepButtonRoot != null)
            NextStepButtonRoot.SetActive(true);

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
