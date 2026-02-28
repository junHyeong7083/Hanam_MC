using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step3
/// - 인스펙터에서 선택지/UI 참조 바인딩
/// - 실제 로직은 Director_Problem5_Step3_Logic(부모)에서 처리
/// </summary>
public class Director_Problem5_Step3 : Director_Problem5_Step3_Logic
{
    [Serializable]
    public class DialogueOptionData : IDialogueOptionData
    {
        [Tooltip("옵션 ID (로그용)")]
        public int id = 1;

        [Tooltip("CSV textId (대사 텍스트)")]
        public int textId;

        [Tooltip("옵션 타입 (회피형 / 건강한 / 도전적)")]
        public DialogueOptionType type = DialogueOptionType.Avoidant;

        [Tooltip("이 옵션이 정답(건강한 표현)인지 여부")]
        public bool isCorrect = false;

        [Tooltip("이 옵션의 카메라 이미지 스프라이트")]
        public Sprite optionSprite;

        // ==== 인터페이스 구현 ====
        public int Id => id;
        public int TextId => textId;
        public DialogueOptionType Type => type;
        public bool IsCorrect => isCorrect;
        public Sprite OptionSprite => optionSprite;
    }

    [Header("선택지")]
    [SerializeField] private DialogueOptionData[] options;

    [Header("NPC 응답 UI")]
    [SerializeField] private GameObject npcResponseRoot;
    [SerializeField] private Text npcResponseText;
    [SerializeField] private int npcResponseTextId;

    [Header("마이크 STT")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // ===== 베이스에 값 주입용 override =====

    protected override IDialogueOptionData[] Options => options;

    protected override GameObject NpcResponseRoot => npcResponseRoot;
    protected override Text NpcResponseText => npcResponseText;
    protected override int NpcResponseTextId => npcResponseTextId;

    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override StepCompletionGate CompletionGate => completionGate;
}
