using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step2
/// - 인스펙터에서 장면 데이터 + UI 참조
/// - 실제 로직은 Director_Problem5_Step2_Logic(부모)에 있음
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
        public GameObject glowImage;        // 글로우 이미지 (revealed 시 비활성화)

        [Header("팝업 이미지들")]
        [Tooltip("클로즈업 팝업 (이미지 + 스케일 애니메이션)")]
        public PopupImageDisplay closeUpPopup;
        [Tooltip("풀씬 팝업 (이미지 + 스케일 애니메이션)")]
        public PopupImageDisplay fullScenePopup;

        // ==== IZoomOutSceneData 구현 ====
        public int Id => id;
        public Button IconButton => iconButton;
        public GameObject UnrevealedRoot => unrevealedRoot;
        public GameObject RevealedRoot => revealedRoot;
        public GameObject GlowImage => glowImage;
        public PopupImageDisplay CloseUpPopup => closeUpPopup;
        public PopupImageDisplay FullScenePopup => fullScenePopup;
    }

    [Header("장면 데이터들 (씬에서 아이콘 1:1 대응)")]
    [SerializeField] private SceneData[] scenes;

    [Header("완료 게이트 (다음 스텝 진행)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ==== 베이스에 값 주입용 override ====

    protected override IZoomOutSceneData[] Scenes => scenes;
    protected override StepCompletionGate CompletionGate => completionGate;
}
