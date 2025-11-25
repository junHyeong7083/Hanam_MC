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
        [TextArea] public string instruction;
        public float durationSeconds = 3f;

        public int Id => id;
        public string Title => title;
        public string Instruction => instruction;
        public float DurationSeconds => durationSeconds;
    }

    [Header("이완 단계 목록")]
    [SerializeField] private RelaxationStepData[] steps;

    [Header("UI Root")]
    [SerializeField] private GameObject introRoot;
    [SerializeField] private GameObject playingRoot;
    [SerializeField] private GameObject pausedRoot;

    [Header("텍스트 / 진행도 UI")]
    [SerializeField] private Text stepCounterLabel;
    [SerializeField] private Text stepTitleLabel;
    [SerializeField] private Text stepInstructionLabel;
    [SerializeField] private Image progressFillImage;

    [Header("호흡 원 애니메이션 루트 (옵션)")]
    [SerializeField] private GameObject breathingCircleRoot;

    [Header("컨트롤 버튼들")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override IRelaxationStepData[] Steps => steps;

    protected override GameObject IntroRoot => introRoot;
    protected override GameObject PlayingRoot => playingRoot;
    protected override GameObject PausedRoot => pausedRoot;

    protected override Text StepCounterLabel => stepCounterLabel;
    protected override Text StepTitleLabel => stepTitleLabel;
    protected override Text StepInstructionLabel => stepInstructionLabel;
    protected override Image ProgressFillImage => progressFillImage;

    protected override GameObject BreathingCircleRoot => breathingCircleRoot;

    protected override Button StartButton => startButton;
    protected override Button PauseButton => pauseButton;
    protected override Button ResumeButton => resumeButton;

    protected override StepCompletionGate CompletionGate => completionGate;
}
