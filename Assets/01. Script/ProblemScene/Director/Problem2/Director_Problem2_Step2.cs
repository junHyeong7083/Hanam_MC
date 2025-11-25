using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Director / Problem2 / Step2
/// - 아래 필름 카드를 클릭하면 위쪽 감정 조명이 켜짐.
/// - 클릭 시 필름 ↔ 조명 사이에 UI 라인(Image) 이펙트가 재생됨.
/// - 모두 켜지면 StepCompletionGate를 통해 완료 버튼이 활성화됨.
/// </summary>
public class Director_Problem2_Step2 : ProblemStepBase
{
    [Serializable]
    public class EmotionLightSlot
    {
        [TextArea]
        public string sceneText;
        public string emotionText;
        public Color color = Color.white;

        [Header("Top Light UI (조명 영역)")]
        public GameObject lightRoot;
        public GameObject lightLockedRoot;      // 잠긴 상태 (회색)
        public GameObject lightRevealedRoot;    // 켜진 상태 (컬러 렌즈 + 이모지)
        public Image lightCircleImage;
        public Image lightGlowImage;
        [Header("Light Label (Image + Text)")]
        public GameObject lightLabelRoot;  // 이미지 + 그 안의 텍스트 전체 루트

        [Header("Line Anchors")]
        public RectTransform filmLineAnchor;    // 필름 카드 쪽 라인 시작점
        public RectTransform lightLineAnchor;   // 조명 쪽 라인 끝점

        [Header("Bottom Film Card UI")]
        public Button filmButton;
        public GameObject filmTouchPromptRoot;
        public GameObject filmEmotionPopupRoot;

        [Header("Line UI (slot 전용)")]
        public RectTransform lineRect;  // 이 슬롯에서 쓸 선 오브젝트
        public Image lineImage;         // 해당 선의 Image
        public UILineConnector lineConnector;

        [NonSerialized] public bool revealed;
        [NonSerialized] public Coroutine lineRoutine;
    }

    [Header("Emotion Light Slots")]
    [SerializeField] private EmotionLightSlot[] slots;

    [Header("Line Animation Settings")]
    [SerializeField] private float lineDrawDuration = 0.35f;   // 선이 그려지는 시간
    [SerializeField] private float lineHoldDuration = 0.4f;    // 다 그려진 후 유지 시간
    [SerializeField] private float lineFadeDuration = 0.25f;   // 줄어들며 사라지는 시간
    [SerializeField] private AnimationCurve lineWidthCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private float lineMaxThickness = 4f;      // UI 픽셀 단위 두께 (sizeDelta.y)

    private Canvas _canvas;
    private RectTransform _canvasRect;

    [Header("Light 등장 애니메이션 (옵션)")]
    [SerializeField] private bool playLightAppearAnimation = true;
    [SerializeField] private float lightAppearDuration = 0.25f;
    [SerializeField] private float lightAppearScale = 1.15f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // === ProblemStepBase 구현 ===
    protected override void OnStepEnter()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.transform as RectTransform;

        InitSlots();

        // 진행도/완료 버튼은 StepCompletionGate가 관리
        if (completionGate != null)
        {
            int total = (slots != null) ? slots.Length : 0;
            completionGate.ResetGate(total);
        }
    }

    protected override void OnStepExit()
    {
        // 필요시 정리
    }

    // ------------------------------------------------------------------
    // 초기 세팅
    // ------------------------------------------------------------------
    private void InitSlots()
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            slot.revealed = false;

            if (slot.lightLabelRoot != null)
                slot.lightLabelRoot.SetActive(false);

            // 색상 반영
            if (slot.lightCircleImage != null)
                slot.lightCircleImage.color = slot.color;

            if (slot.lightGlowImage != null)
            {
                var c = slot.color;
                c.a = slot.lightGlowImage.color.a; // 기존 알파 유지
                slot.lightGlowImage.color = c;
            }

            // 슬롯 전용 라인 초기화
            if (slot.lineRect != null)
            {
                if (slot.lineImage == null)
                    slot.lineImage = slot.lineRect.GetComponent<Image>();

                if (slot.lineConnector == null)
                    slot.lineConnector = slot.lineRect.GetComponent<UILineConnector>();

                if (slot.lineConnector != null)
                {
                    // 라인 컴포넌트 기준으로 리셋
                    slot.lineConnector.ResetLine();
                }

                // 잠긴 상태로 시작
                SetRevealState(slot, reveal: false, immediate: true);
            }
            // 버튼 연결
            if (slot.filmButton != null)
            {
                var captured = slot;
                slot.filmButton.onClick.RemoveAllListeners();
                slot.filmButton.onClick.AddListener(() => OnFilmClicked(captured));
            }
        }
    }

    private void SetRevealState(EmotionLightSlot slot, bool reveal, bool immediate = false)
    {
        slot.revealed = reveal;

        if (slot.lightLockedRoot != null)
            slot.lightLockedRoot.SetActive(!reveal);

        if (slot.lightRevealedRoot != null)
        {
            slot.lightRevealedRoot.SetActive(reveal);

            if (reveal && playLightAppearAnimation && !immediate)
            {
                StartCoroutine(PlayLightAppear(slot.lightRevealedRoot.transform));
            }
            else if (reveal && immediate)
            {
                slot.lightRevealedRoot.transform.localScale = Vector3.one;
            }
        }

        if (slot.filmTouchPromptRoot != null)
            slot.filmTouchPromptRoot.SetActive(!reveal);
        if (slot.filmEmotionPopupRoot != null)
            slot.filmEmotionPopupRoot.SetActive(reveal);
    }

    // ------------------------------------------------------------------
    // 필름 클릭 → 조명 켜기 + 라인 애니메이션
    // ------------------------------------------------------------------
    private void OnFilmClicked(EmotionLightSlot slot)
    {
        Debug.Log($"[OnFilmClicked] clicked slot={slot.emotionText}");

        if (slot.revealed)
        {
            Debug.Log("[OnFilmClicked] already revealed, ignore");
            return;
        }

        // 1) 조명/필름 UI 상태 갱신
        SetRevealState(slot, true);

        // 2) StepCompletionGate에 진행도 1 증가 보고
        if (completionGate != null)
            completionGate.MarkOneDone();
        if (slot.lineRoutine != null)
            StopCoroutine(slot.lineRoutine);
        slot.lineRoutine = StartCoroutine(PlayUILine(slot));
    }
    

    // ------------------------------------------------------------------
    // 슬롯별 UI Image 선 애니메이션
    // ------------------------------------------------------------------
    // 슬롯별 UI Image 선 애니메이션
    private IEnumerator PlayUILine(EmotionLightSlot slot)
    {
        // 혹시 인스펙터에서 안 넣었으면 한 번 더 시도
        if (slot.lineConnector == null && slot.lineRect != null)
            slot.lineConnector = slot.lineRect.GetComponent<UILineConnector>();

        // Anchor나 컴포넌트가 없으면 그냥 종료
        if (slot.lineConnector == null ||
            slot.filmLineAnchor == null ||
            slot.lightLineAnchor == null)
        {
            slot.lineRoutine = null;
            yield break;
        }

        // Label은 한 번만 등장 애니메이션
        if (slot.lightLabelRoot != null && !slot.lightLabelRoot.activeSelf)
        {
            slot.lightLabelRoot.SetActive(true);
            StartCoroutine(PlayLightAppear(slot.lightLabelRoot.transform));
        }

        // 실제 라인 애니메이션은 컴포넌트에 위임
        yield return slot.lineConnector.PlayLineRoutine(
            slot.filmLineAnchor,
            slot.lightLineAnchor
        );

        slot.lineRoutine = null;
    }


    // ------------------------------------------------------------------
    // 조명 등장 애니메이션
    // ------------------------------------------------------------------
    private IEnumerator PlayLightAppear(Transform target)
    {
        if (target == null) yield break;

        Vector3 startScale = Vector3.one * 0.8f;
        Vector3 peakScale = Vector3.one * lightAppearScale;
        Vector3 endScale = Vector3.one;

        float t = 0f;
        target.localScale = startScale;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, lightAppearDuration);
            float lerpT = Mathf.Clamp01(t);

            if (lerpT < 0.5f)
            {
                float u = lerpT / 0.5f;
                target.localScale = Vector3.Lerp(startScale, peakScale, u);
            }
            else
            {
                float u = (lerpT - 0.5f) / 0.5f;
                target.localScale = Vector3.Lerp(peakScale, endScale, u);
            }

            yield return null;
        }

        target.localScale = endScale;
    }
}
