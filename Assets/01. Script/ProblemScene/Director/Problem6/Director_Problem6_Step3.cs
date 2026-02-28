using System;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem6_Step3 : Director_Problem6_Step3_Logic
{
    [Serializable]
    public class RelaxationStepData : IRelaxationStepData
    {
        public int id = 1;
        public string title;
        public int instructionTextId;
        public float durationSeconds = 3f;

        public int Id => id;
        public string Title => title;
        public int InstructionTextId => instructionTextId;
        public float DurationSeconds => durationSeconds;
    }

    [Header("이완 단계 목록")]
    [SerializeField] private RelaxationStepData[] steps;

    [Header("UI Root")]
    [SerializeField] private GameObject playingRoot;
    [SerializeField] private GameObject pausedRoot;

    [Header("텍스트 UI")]
    [SerializeField] private Text stepTitleLabel;
    [SerializeField] private Text stepInstructionLabel;

    [Header("컨트롤 버튼들")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem6_Step3_EffectController effectController;

    protected override IRelaxationStepData[] Steps => steps;

    protected override GameObject PlayingRoot => playingRoot;
    protected override GameObject PausedRoot => pausedRoot;

    protected override Text StepTitleLabel => stepTitleLabel;
    protected override Text StepInstructionLabel => stepInstructionLabel;

    protected override Button PauseButton => pauseButton;
    protected override Button ResumeButton => resumeButton;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override Problem6_Step3_EffectController EffectController => effectController;
}
