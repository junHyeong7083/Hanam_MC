using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step2 로직 베이스
/// - "3라운드 대사 선택" 한 화면에서 진행
/// - 흐름: 상황 + 3개 선택지 → 정답 선택 → 결과 + 말풍선 → 다음 라운드 (총 3회)
/// </summary>
public abstract class Director_Problem9_Step2_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class RoundData
    {
        public int situationTextId;        // 상황 안내 (하남박스)
        public int[] choiceTextIds;        // 선택지 textId (3개)
        public int correctChoiceIndex;     // 정답 인덱스 (0-based)
        public int resultTextId;           // 결과 텍스트 (하남박스)
        public int speechBubbleTextId;     // 말풍선 텍스트
        public Sprite sceneSprite;         // 씬 카드 이미지 (질문)
        public Sprite answerSceneSprite;   // 씬 카드 이미지 (답변)
    }

    [Serializable]
    private class ChoiceAttemptDto
    {
        public int roundIndex;
        public int choiceIndex;
        public bool isCorrect;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    protected abstract RoundData[] Rounds { get; }

    // 하남박스
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Fail { get; }
    protected abstract Button NextDialogueButton { get; }
    protected abstract GameObject NextStepButtonRoot { get; }

    // 씬 이미지
    protected abstract Image SceneCardImage { get; }

    // 질문 영역
    protected abstract GameObject QuestionRoot { get; }
    protected abstract Button[] QuestionButtons { get; }
    protected abstract Text[] QuestionLabels { get; }

    // 답변 영역
    protected abstract GameObject AnswerRoot { get; }
    protected abstract Text SpeechBubbleText { get; }

    #endregion

    // 내부 상태
    private int _currentRound;
    private bool _answering;
    private Coroutine _guideRevertRoutine;
    private List<ChoiceAttemptDto> _attempts;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentRound = 0;
        _answering = false;
        _attempts = new List<ChoiceAttemptDto>();

        RegisterListeners();
        ShowRound(0);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_guideRevertRoutine != null)
        {
            StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = null;
        }

        RemoveListeners();
    }

    // =========================
    // 리스너 등록/해제
    // =========================

    private void RegisterListeners()
    {
        var buttons = QuestionButtons;
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                buttons[i].onClick.RemoveAllListeners();
                int idx = i;
                buttons[i].onClick.AddListener(() => OnChoiceClicked(idx));
            }
        }

        var nextBtn = NextDialogueButton;
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(OnNextDialogueClicked);
        }
    }

    private void RemoveListeners()
    {
        var buttons = QuestionButtons;
        if (buttons != null)
        {
            foreach (var btn in buttons)
                if (btn != null) btn.onClick.RemoveAllListeners();
        }

        var nextBtn = NextDialogueButton;
        if (nextBtn != null)
            nextBtn.onClick.RemoveAllListeners();
    }

    // =========================
    // 라운드 표시
    // =========================

    private void ShowRound(int round)
    {
        var rounds = Rounds;
        if (rounds == null || round >= rounds.Length) return;

        var data = rounds[round];
        _answering = false;

        // 가이드 텍스트
        if (GuideText != null && data.situationTextId > 0)
            GuideText.text = ProblemRuntime.L(data.situationTextId);

        // 씬 이미지
        if (SceneCardImage != null && data.sceneSprite != null)
            SceneCardImage.sprite = data.sceneSprite;

        // 선택지 라벨
        var labels = QuestionLabels;
        if (labels != null && data.choiceTextIds != null)
        {
            for (int i = 0; i < labels.Length && i < data.choiceTextIds.Length; i++)
            {
                if (labels[i] != null && data.choiceTextIds[i] > 0)
                    labels[i].text = ProblemRuntime.L(data.choiceTextIds[i]);
            }
        }

        // 질문 버튼 활성화
        var buttons = QuestionButtons;
        if (buttons != null)
        {
            foreach (var btn in buttons)
                if (btn != null) btn.interactable = true;
        }

        // QuestionRoot 표시, AnswerRoot 숨김
        if (QuestionRoot != null) QuestionRoot.SetActive(true);
        if (AnswerRoot != null) AnswerRoot.SetActive(false);

        // 버튼 숨기기
        if (NextDialogueButton != null) NextDialogueButton.gameObject.SetActive(false);
        if (NextStepButtonRoot != null) NextStepButtonRoot.SetActive(false);
    }

    // =========================
    // 선택지 클릭
    // =========================

    private void OnChoiceClicked(int index)
    {
        if (_answering) return;

        var rounds = Rounds;
        if (rounds == null || _currentRound >= rounds.Length) return;

        var data = rounds[_currentRound];
        bool isCorrect = (index == data.correctChoiceIndex);

        _attempts.Add(new ChoiceAttemptDto
        {
            roundIndex = _currentRound,
            choiceIndex = index,
            isCorrect = isCorrect
        });

        if (isCorrect)
            OnCorrect(data);
        else
            OnWrong();
    }

    // =========================
    // 정답 / 오답
    // =========================

    private void OnCorrect(RoundData data)
    {
        _answering = true;

        // QuestionRoot 숨기고 AnswerRoot 표시
        if (QuestionRoot != null) QuestionRoot.SetActive(false);
        if (AnswerRoot != null) AnswerRoot.SetActive(true);

        // 씬 이미지 → 답변용 스프라이트로 교체
        if (SceneCardImage != null && data.answerSceneSprite != null)
            SceneCardImage.sprite = data.answerSceneSprite;

        // 결과 텍스트 (하남박스)
        if (GuideText != null && data.resultTextId > 0)
            GuideText.text = ProblemRuntime.L(data.resultTextId);

        // 말풍선 텍스트
        if (SpeechBubbleText != null && data.speechBubbleTextId > 0)
            SpeechBubbleText.text = ProblemRuntime.L(data.speechBubbleTextId);

        // 적절한 버튼 표시
        if (_currentRound < Rounds.Length - 1)
        {
            if (NextDialogueButton != null)
                NextDialogueButton.gameObject.SetActive(true);
        }
        else
        {
            if (NextStepButtonRoot != null)
                NextStepButtonRoot.SetActive(true);

            SaveAttempt(_attempts);
        }
    }

    private void OnWrong()
    {
        if (GuideText != null && GuideTextId_Fail > 0)
        {
            if (_guideRevertRoutine != null)
                StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = StartCoroutine(ShowFailGuideAndRevert());
        }
    }

    private IEnumerator ShowFailGuideAndRevert()
    {
        var rounds = Rounds;
        int situationTextId = (rounds != null && _currentRound < rounds.Length)
            ? rounds[_currentRound].situationTextId
            : 0;

        GuideText.text = ProblemRuntime.L(GuideTextId_Fail);
        yield return new WaitForSeconds(2f);

        if (GuideText != null && situationTextId > 0 && !_answering)
            GuideText.text = ProblemRuntime.L(situationTextId);

        _guideRevertRoutine = null;
    }

    // =========================
    // 다음 라운드
    // =========================

    private void OnNextDialogueClicked()
    {
        _currentRound++;
        ShowRound(_currentRound);
    }
}
