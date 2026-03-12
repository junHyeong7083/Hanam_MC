using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem9_Step2_Logic - 문제9 스텝2 3라운드 대사 선택 로직 (추상 클래스)
///
/// 【역할】 3라운드에 걸쳐 상황별 올바른 대사를 선택하는 메인 활동.
///          각 라운드마다 상황 안내 + 3개 선택지가 표시되며, 정답 선택 시
///          결과 텍스트 + 말풍선이 표시되고 다음 라운드로 진행한다.
///          3라운드 모두 완료 시 DB 저장 및 완료 처리.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층.
/// 【문제/스텝】 Director 테마 > 문제9 > 스텝2 (메인 활동 - 대사 선택)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem9_Step2 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, SoundManager
/// 【흐름】 스텝 진입 → 라운드1 표시 → 정답 선택 → 결과+말풍선 → "다음" 버튼
///         → 라운드2 → ... → 라운드3 정답 → DB 저장 + 완료
/// </summary>
public abstract class Director_Problem9_Step2_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    /// <summary>한 라운드의 상황/선택지/정답/결과 데이터를 묶는 구조체</summary>
    [Serializable]
    public class RoundData
    {
        public int situationTextId;        // 상황 안내 텍스트의 CSV textId (하남박스에 표시)
        public int[] choiceTextIds;        // 선택지 textId 배열 (3개)
        public int correctChoiceIndex;     // 정답 인덱스 (0-based)
        public int resultTextId;           // 정답 선택 후 결과 텍스트의 CSV textId
        public int speechBubbleTextId;     // 정답 선택 후 말풍선 텍스트의 CSV textId
        public Sprite sceneSprite;         // 질문 상태의 씬 카드 이미지
        public Sprite answerSceneSprite;   // 정답 상태의 씬 카드 이미지
    }

    /// <summary>선택 기록 DTO (DB 저장용)</summary>
    [Serializable]
    private class ChoiceAttemptDto
    {
        public int roundIndex;    // 라운드 번호 (0-based)
        public int choiceIndex;   // 선택한 선택지 인덱스
        public bool isCorrect;    // 정답 여부
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>라운드 데이터 배열 (3라운드)</summary>
    protected abstract RoundData[] Rounds { get; }

    // ----- 하남박스 -----
    /// <summary>가이드 텍스트 UI (상황 안내 / 결과 표시)</summary>
    protected abstract Text GuideText { get; }
    /// <summary>오답 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Fail { get; }
    /// <summary>다음 라운드로 넘어가는 버튼</summary>
    protected abstract Button NextDialogueButton { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서

    // ----- 씬 이미지 -----
    /// <summary>씬 카드 이미지 (질문/정답 스프라이트 교체)</summary>
    protected abstract Image SceneCardImage { get; }

    // ----- 질문 영역 -----
    /// <summary>선택지 버튼들이 포함된 질문 루트 오브젝트</summary>
    protected abstract GameObject QuestionRoot { get; }
    /// <summary>선택지 버튼 배열 (3개)</summary>
    protected abstract Button[] QuestionButtons { get; }
    /// <summary>선택지 라벨 텍스트 배열 (3개)</summary>
    protected abstract Text[] QuestionLabels { get; }

    // ----- 답변 영역 -----
    /// <summary>정답 선택 후 표시되는 답변 루트 오브젝트</summary>
    protected abstract GameObject AnswerRoot { get; }
    /// <summary>정답 선택 후 표시되는 말풍선 텍스트</summary>
    protected abstract Text SpeechBubbleText { get; }

    // ----- 대화 이미지 (질문 시 / 정답 시 교체) -----
    /// <summary>질문 시 표시되는 내 캐릭터 대화 이미지</summary>
    protected abstract GameObject MyDialogueImage { get; }
    /// <summary>정답 시 표시되는 상대 캐릭터 대화 이미지</summary>
    protected abstract GameObject OtherDialogueImage { get; }

    #endregion

    // ===== 내부 상태 =====
    private int _currentRound;                     // 현재 라운드 인덱스 (0-based)
    private bool _answering;                       // 정답을 선택하여 답변 화면 표시 중인지
    private List<ChoiceAttemptDto> _attempts;       // 모든 라운드의 선택 기록 (DB 저장용)
    private bool _interactionLocked = true;        // 대화 재생 중 상호작용 잠금

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>스텝 진입. 상태 초기화, 리스너 등록, 첫 라운드 표시, 대화 재생 대기.</summary>
    protected override void OnStepEnter()
    {
        _currentRound = 0;
        _answering = false;
        _attempts = new List<ChoiceAttemptDto>();

        RegisterListeners();
        ShowRound(0);

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

    /// <summary>스텝 퇴장. 리스너 정리.</summary>
    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;
        RemoveListeners();
    }

    // =========================
    // 리스너 등록/해제
    // =========================

    /// <summary>선택지 버튼 및 "다음" 버튼에 클릭 리스너를 등록한다.</summary>
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

    /// <summary>모든 버튼 리스너를 제거한다.</summary>
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

    /// <summary>
    /// 지정된 라운드의 UI를 세팅한다. 상황 텍스트, 씬 이미지, 선택지 라벨,
    /// 질문/답변 영역 표시 상태를 초기화한다.
    /// </summary>
    private void ShowRound(int round)
    {
        var rounds = Rounds;
        if (rounds == null || round >= rounds.Length) return;

        var data = rounds[round];
        _answering = false;

        // 가이드 텍스트
        if (GuideText != null && data.situationTextId > 0)
            GuideText.text = ProblemRuntime.L(data.situationTextId);

        if (data.situationTextId > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(data.situationTextId);

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

        // 대화 이미지: 질문 시 my, other 숨김
        if (MyDialogueImage != null) MyDialogueImage.SetActive(true);
        if (OtherDialogueImage != null) OtherDialogueImage.SetActive(false);

        // 버튼 숨기기
        if (NextDialogueButton != null) NextDialogueButton.gameObject.SetActive(false);
    }

    // =========================
    // 선택지 클릭
    // =========================

    /// <summary>선택지 클릭 시 호출. 정답 여부를 판별하여 정답/오답 처리로 분기한다.</summary>
    private void OnChoiceClicked(int index)
    {
        if (_interactionLocked) return;
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

    /// <summary>
    /// 정답 선택 시 호출. 답변 영역 표시, 씬 이미지 교체, 결과 텍스트/말풍선 표시.
    /// 마지막 라운드면 DB 저장 + 완료, 아니면 "다음" 버튼 표시.
    /// </summary>
    private void OnCorrect(RoundData data)
    {
        _answering = true;

        // QuestionRoot 숨기고 AnswerRoot 표시
        if (QuestionRoot != null) QuestionRoot.SetActive(false);
        if (AnswerRoot != null) AnswerRoot.SetActive(true);

        // 대화 이미지: 정답 시 other 표시
        if (MyDialogueImage != null) MyDialogueImage.SetActive(false);
        if (OtherDialogueImage != null) OtherDialogueImage.SetActive(true);

        // 씬 이미지 → 답변용 스프라이트로 교체
        if (SceneCardImage != null && data.answerSceneSprite != null)
            SceneCardImage.sprite = data.answerSceneSprite;

        // 결과 텍스트 (하남박스)
        if (GuideText != null && data.resultTextId > 0)
            GuideText.text = ProblemRuntime.L(data.resultTextId);

        if (data.resultTextId > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(data.resultTextId);

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
            SaveAttempt(_attempts);

            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();
        }
    }

    /// <summary>오답 선택 시 호출. 오답 안내 텍스트를 표시한다 (다시 선택 가능).</summary>
    private void OnWrong()
    {
        if (GuideText != null && GuideTextId_Fail > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Fail);
    }

    // =========================
    // 다음 라운드
    // =========================

    /// <summary>"다음" 버튼 클릭 시 호출. 다음 라운드를 표시한다.</summary>
    private void OnNextDialogueClicked()
    {
        _currentRound++;
        ShowRound(_currentRound);
    }
}
