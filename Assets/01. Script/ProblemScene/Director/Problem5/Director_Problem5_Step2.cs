using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem5_Step2 - 문제5 스텝2의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 장면 데이터(SceneData) 배열과 완료 게이트를 바인딩한다.
///         실제 장면 아이콘 탐색/리빌 로직은 부모(Director_Problem5_Step2_Logic)에 있다.
///         SceneData 내부 클래스가 IZoomOutSceneData를 구현하여 textId 기반으로 텍스트를 가져온다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제5 / 스텝2 (메인 활동 - 장면 아이콘 탐색)
/// 【부모 클래스】 Director_Problem5_Step2_Logic → ProblemStepBase
/// </summary>
public class Director_Problem5_Step2 : Director_Problem5_Step2_Logic
{
    [Serializable]
    public class SceneData : IZoomOutSceneData
    {
        [Tooltip("장면 ID (로그용)")]
        public int id = 1;

        [Header("아이콘 관련 UI")]
        public Button iconButton;           // 클릭용 버튼
        public GameObject unrevealedRoot;   // 아직 클릭 안 한 상태의 비주얼
        public GameObject revealedRoot;     // 클릭 완료 후 비주얼

        [Header("텍스트 (CSV textId)")]
        public int unrevealedTextId;        // 클릭 전 텍스트 ID
        public Text unrevealedText;         // 클릭 전 텍스트 UI
        public int revealedTextId;          // 클릭 후 텍스트 ID
        public Text revealedText;           // 클릭 후 텍스트 UI

        // ==== IZoomOutSceneData 구현 ====
        public int Id => id;
        public Button IconButton => iconButton;
        public GameObject UnrevealedRoot => unrevealedRoot;
        public GameObject RevealedRoot => revealedRoot;
        public int UnrevealedTextId => unrevealedTextId;
        public int RevealedTextId => revealedTextId;
        public Text UnrevealedText => unrevealedText;
        public Text RevealedText => revealedText;
    }

    [Header("장면 데이터들 (씬에서 아이콘 1:1 대응)")]
    [SerializeField] private SceneData[] scenes;

    [Header("완료 게이트 (다음 스텝 진행)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ==== 베이스에 값 주입용 override ====

    protected override IZoomOutSceneData[] Scenes => scenes;
    protected override StepCompletionGate CompletionGate => completionGate;
}
