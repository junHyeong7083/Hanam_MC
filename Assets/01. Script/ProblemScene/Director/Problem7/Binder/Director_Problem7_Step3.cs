using UnityEngine;

/// <summary>
/// Director_Problem7_Step3 - 문제7 스텝3 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 대사 선택 화면, 마이크 STT, 재시도 텍스트의 UI 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제7 > 스텝3 (마무리 - STT 명대사 말하기)
/// 【부모 클래스】 Director_Problem7_Step3_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem7 Step3 GameObject에 부착
/// 【참조되는 곳】 Director_Problem7_Step3_Logic (명대사 말하기 로직)
/// </summary>
public class Director_Problem7_Step3 : Director_Problem7_Step3_Logic
{
    [Header("===== 재시도 텍스트 =====")]
    [SerializeField] private int retryTextId;                      // STT 실패 시 재시도 안내 textId

    [Header("===== 대사 선택 화면 =====")]
    [SerializeField] private GameObject selectDialogueRoot;        // 대사 선택 UI 루트
    [Tooltip("3개 대사: id/textId/button/selectImg 설정")]
    [SerializeField] private DialogueItem[] dialogueChoices;       // 대사 선택지 3개

    [Header("===== 마이크 STT =====")]
    [SerializeField] private MicRecordingIndicator micIndicator;   // STT 녹음 인디케이터
    [SerializeField] private GameObject micButtonRoot;              // 마이크 버튼 루트 (선택 전 숨김)

    // ===== 부모 추상 프로퍼티를 인스펙터 필드로 연결 =====
    protected override int RetryTextId => retryTextId;

    protected override GameObject SelectDialogueRoot => selectDialogueRoot;
    protected override DialogueItem[] DialogueChoices => dialogueChoices;

    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject MicButtonRoot => micButtonRoot;
}
