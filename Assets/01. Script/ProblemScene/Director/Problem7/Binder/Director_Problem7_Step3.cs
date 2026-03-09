using UnityEngine;

/// <summary>
/// Director / Problem7 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step3 : Director_Problem7_Step3_Logic
{
    [Header("===== 재시도 텍스트 =====")]
    [SerializeField] private int retryTextId;

    [Header("===== 대사 선택 화면 =====")]
    [SerializeField] private GameObject selectDialogueRoot;
    [Tooltip("3개 대사: id/textId/button/selectImg 설정")]
    [SerializeField] private DialogueItem[] dialogueChoices;

    [Header("===== 마이크 STT =====")]
    [SerializeField] private MicRecordingIndicator micIndicator;
    [SerializeField] private GameObject micButtonRoot;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override int RetryTextId => retryTextId;

    protected override GameObject SelectDialogueRoot => selectDialogueRoot;
    protected override DialogueItem[] DialogueChoices => dialogueChoices;

    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject MicButtonRoot => micButtonRoot;
}
