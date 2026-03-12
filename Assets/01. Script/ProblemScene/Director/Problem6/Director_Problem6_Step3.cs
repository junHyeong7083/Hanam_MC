using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem6_Step3 - 문제6 스텝3 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 이완 단계 데이터, UI 루트, 텍스트, 버튼, 게이트, 이펙트, 프로그레스 바의
///          참조를 SerializeField로 바인딩하고 부모 Logic의 추상 프로퍼티를 override한다.
///          IRelaxationStepData 인터페이스의 concrete 구현(RelaxationStepData)도 포함한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제6 > 스텝3 (마무리 - 이완 훈련)
/// 【부모 클래스】 Director_Problem6_Step3_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem6 Step3 GameObject에 부착
/// 【참조되는 곳】 Director_Problem6_Step3_Logic (이완 훈련 재생 로직)
/// </summary>
public class Director_Problem6_Step3 : Director_Problem6_Step3_Logic
{
    /// <summary>
    /// RelaxationStepData - IRelaxationStepData의 concrete 구현.
    /// 인스펙터에서 이완 단계별 ID, 제목, 안내 텍스트 ID, 지속 시간을 설정할 수 있다.
    /// </summary>
    [Serializable]
    public class RelaxationStepData : IRelaxationStepData
    {
        public int id = 1;                  // 단계 고유 ID
        public string title;                // 단계 제목 (예: "복식 호흡", "근육 이완")
        public int instructionTextId;       // CSV textId (단계 안내 문구)
        public float durationSeconds = 3f;  // 이 단계의 지속 시간 (초)

        // IRelaxationStepData 인터페이스 구현
        public int Id => id;
        public string Title => title;
        public int InstructionTextId => instructionTextId;
        public float DurationSeconds => durationSeconds;
    }

    [Header("이완 단계 목록")]
    [SerializeField] private RelaxationStepData[] steps;              // 이완 훈련 단계 배열

    [Header("UI Root")]
    [SerializeField] private GameObject playingRoot;                   // 재생 중 표시 UI 루트
    [SerializeField] private GameObject pausedRoot;                    // 일시정지 중 표시 UI 루트

    [Header("텍스트 UI")]
    [SerializeField] private Text stepTitleLabel;                      // 현재 단계 제목 텍스트
    [SerializeField] private Text stepInstructionLabel;                // 현재 단계 안내 텍스트

    [Header("컨트롤 버튼들")]
    [SerializeField] private Button pauseButton;                       // 일시정지 버튼
    [SerializeField] private Button resumeButton;                      // 재개 버튼

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;        // 스텝 완료 판정용 게이트

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem6_Step3_EffectController effectController; // 카드 팝인 등 이펙트

    [Header("프로그레스 바")]
    [SerializeField] private Image progressFillImage;                  // 단계 진행률 Fill 이미지

    // ===== 부모 추상 프로퍼티를 인스펙터 필드로 연결 =====
    protected override IRelaxationStepData[] Steps => steps;

    protected override GameObject PlayingRoot => playingRoot;
    protected override GameObject PausedRoot => pausedRoot;

    protected override Text StepTitleLabel => stepTitleLabel;
    protected override Text StepInstructionLabel => stepInstructionLabel;

    protected override Button PauseButton => pauseButton;
    protected override Button ResumeButton => resumeButton;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override Problem6_Step3_EffectController EffectController => effectController;

    protected override Image ProgressFillImage => progressFillImage;
}
