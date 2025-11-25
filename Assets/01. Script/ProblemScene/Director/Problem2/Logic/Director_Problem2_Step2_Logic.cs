using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMPro는 사용하지 않지만, 원본 파일에 있었으므로 유지

/// <summary>
/// Director / Problem2 / Step2 공통 로직 베이스.
/// - ProblemStepBase 를 상속받고,
/// - 구체 Step(Director_Problem2_Step2)은 단순히 필드만 들고 프로퍼티로 매핑.
/// </summary>
public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    // === 자식에서 UI를 매핑해 줄 추상 프로퍼티들 ===

    [Serializable]
    protected class EmotionLightSlot
    {
        // 원본과 동일한 구조 유지 (UI 바인딩을 위한 필드들)
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

        // 로직 내부에서만 쓰는 필드는 그대로 유지
        [NonSerialized] public bool revealed;
        [NonSerialized] public Coroutine lineRoutine;
    }

    [Header("Emotion Light Slots (자식에서 주입)")]
    protected abstract EmotionLightSlot[] Slots { get; }

    [Header("Line Animation Settings (자식에서 주입)")]
    protected abstract float LineDrawDuration { get; }
    protected abstract float LineHoldDuration { get; }
    protected abstract float LineFadeDuration { get; }
    protected abstract AnimationCurve LineWidthCurve { get; }
    protected abstract float LineMaxThickness { get; }

    [Header("Light 등장 애니메이션 (자식에서 주입)")]
    protected abstract bool PlayLightAppearAnimation { get; }
    protected abstract float LightAppearDuration { get; }
    protected abstract float LightAppearScale { get; }

    [Header("완료 게이트 (자식에서 주입)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // 내부 로직용 필드 (자식에게 노출 불필요)
    private Canvas _canvas;
    private RectTransform _canvasRect;

    // === ProblemStepBase 구현 ===
    protected override void OnStepEnter()
    {
        // 로직 내부에서 필요한 설정 (캔버스 참조)
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.transform as RectTransform;

        InitSlots();

        // 진행도/완료 버튼은 StepCompletionGate가 관리
        if (CompletionGate != null)
        {
            int total = (Slots != null) ? Slots.Length : 0;
            CompletionGate.ResetGate(total);
        }
    }

    protected override void OnStepExit()
    {
        // 필요시 정리
    }

    // ------------------------------------------------------------------
    // 초기 세팅 (로직)
    // ------------------------------------------------------------------
    private void InitSlots()
    {
        if (Slots == null) return;

        foreach (var slot in Slots)
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
                // 원본 로직 유지: 인스펙터에 연결 안 되어 있으면 GetComponent 시도
                if (slot.lineImage == null)
                    slot.lineImage = slot.lineRect.GetComponent<Image>();

                if (slot.lineConnector == null)
                    slot.lineConnector = slot.lineRect.GetComponent<UILineConnector>();

                if (slot.lineConnector != null)
                {
                    // 라인 컴포넌트 기준으로 리셋
                    // 주의: UILineConnector의 PlayLineRoutine이 Duration/Thickness를 사용하도록 
                    // 외부에서 구현되어 있어야 함. (원본 코드에서 LineConnector의 상세는 알 수 없음)
                    // 현재는 원본의 로직 위임 구조를 최대한 유지함.
                    slot.lineConnector.ResetLine();

                    // Logic 클래스에서 LineConnector에 설정 값을 넘겨주는 코드가 필요하면 여기에 추가
                    // (ex: slot.lineConnector.SetAnimationSettings(LineDrawDuration, LineHoldDuration, ...);)
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

    // ------------------------------------------------------------------
    // 상태 제어 (로직)
    // ------------------------------------------------------------------
    private void SetRevealState(EmotionLightSlot slot, bool reveal, bool immediate = false)
    {
        slot.revealed = reveal;

        if (slot.lightLockedRoot != null)
            slot.lightLockedRoot.SetActive(!reveal);

        if (slot.lightRevealedRoot != null)
        {
            slot.lightRevealedRoot.SetActive(reveal);

            if (reveal && PlayLightAppearAnimation && !immediate)
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
    // 필름 클릭 → 조명 켜기 + 라인 애니메이션 (로직)
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
        if (CompletionGate != null)
            CompletionGate.MarkOneDone();

        // 3) 라인 애니메이션 시작
        if (slot.lineRoutine != null)
            StopCoroutine(slot.lineRoutine);
        slot.lineRoutine = StartCoroutine(PlayUILine(slot));
    }


    // ------------------------------------------------------------------
    // 슬롯별 UI Image 선 애니메이션 (로직)
    // ------------------------------------------------------------------
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
        // 주의: UILineConnector에 LineDrawDuration 등의 값을 넘겨주는 코드가 필요하면
        // 자식 클래스의 InitSlots 이후에 실행되거나, UILineConnector가 내부적으로 이 값을
        // 참조하도록 수정해야 할 수 있습니다. 현재는 원본 로직을 유지합니다.

        // 원본 로직:
        yield return slot.lineConnector.PlayLineRoutine(
            slot.filmLineAnchor,
            slot.lightLineAnchor
        );

        slot.lineRoutine = null;
    }


    // ------------------------------------------------------------------
    // 조명 등장 애니메이션 (로직)
    // ------------------------------------------------------------------
    private IEnumerator PlayLightAppear(Transform target)
    {
        if (target == null) yield break;

        Vector3 startScale = Vector3.one * 0.8f;
        Vector3 peakScale = Vector3.one * LightAppearScale; // 프로퍼티 사용
        Vector3 endScale = Vector3.one;

        float t = 0f;
        target.localScale = startScale;
        float duration = LightAppearDuration; // 프로퍼티 사용

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
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