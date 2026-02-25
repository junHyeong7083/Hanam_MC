using System;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem1_Step3 : Director_Problem1_Step3_Logic
{
    [Serializable]
    public class FilmItem
    {
        public int id;

        [TextArea]
        public string filmText;

        public Sprite filmSprite;

        public bool isThought;
    }

    [Header("문항 설정")]
    [SerializeField] private FilmItem[] films;

    [Header("상단 하남이 대사 (textId 배열)")]
    [SerializeField] private Text hanamiDialogueText;
    [SerializeField] private int[] hanamiDialogueTextIds; // [0]=기본, [1]=오답

    [Header("하남이 대사 연출")]
    [SerializeField] private float wrongHanamiMessageDuration = 2f;

    [Header("현재 필름 UI")]
    [SerializeField] private RectTransform currentFilmRoot;
    [SerializeField] private GameObject currentFilmPrefab;

    [Header("분류 후 배치 슬롯 (하이라키 pos들 순서대로)")]
    [Tooltip("생각 필름통 슬롯들 (예: pos0, pos1, pos2, pos3 ...)")]
    [SerializeField] private Transform[] thoughtSlots;

    [Tooltip("사실 필름통 슬롯들 (예: pos4, pos5, pos6, pos7 ...)")]
    [SerializeField] private Transform[] factSlots;

    [Header("정답 버튼 / 다음촬영 버튼 루트")]
    [SerializeField] private GameObject answerButtonsRoot;
    [SerializeField] private GameObject summaryButtonRoot;

    [Header("분류 연출 딜레이")]
    [SerializeField] private float sortAdvanceDelay = 0.6f;

    [Header("마이크")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("패널 전환")]
    [SerializeField] private GameObject stepRoot;
    [SerializeField] private GameObject summaryPanelRoot;

    protected override int FilmCount => films != null ? films.Length : 0;

    protected override int GetFilmId(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return -1;
        return films[index].id;
    }

    protected override string GetFilmText(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return null;
        return films[index].filmText;
    }

    protected override Sprite GetFilmSprite(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return null;
        return films[index].filmSprite;
    }

    protected override bool IsFilmThought(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return false;
        return films[index].isThought;
    }

    protected override int GetHanamiDialogueTextId(int index)
    {
        if (hanamiDialogueTextIds == null || hanamiDialogueTextIds.Length == 0) return -1;
        if (index < 0 || index >= hanamiDialogueTextIds.Length) return -1;
        return hanamiDialogueTextIds[index];
    }

    protected override float WrongHanamiMessageDuration => wrongHanamiMessageDuration;

    protected override RectTransform CurrentFilmRoot => currentFilmRoot;
    protected override GameObject CurrentFilmPrefab => currentFilmPrefab;

    protected override Transform[] ThoughtSlots => thoughtSlots;
    protected override Transform[] FactSlots => factSlots;

    protected override GameObject AnswerButtonsRoot => answerButtonsRoot;
    protected override GameObject SummaryButtonRoot => summaryButtonRoot;
    protected override float SortAdvanceDelay => sortAdvanceDelay;

    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject StepRoot => stepRoot;
    protected override GameObject SummaryPanelRoot => summaryPanelRoot;

    protected override Text HanamiDialogueText => hanamiDialogueText;
}