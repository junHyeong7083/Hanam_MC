using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public abstract class Director_Problem2_Step2_Logic : ProblemStepBase
{
    [Serializable]
    protected class EmotionLightSlot
    {
        [TextArea] public string sceneText;
        public string emotionText;
        public Color color = Color.white;

        [Header("Top Light UI (조명 영역)")]
        public GameObject lightRoot;
        public GameObject lightLockedRoot;
        public GameObject lightRevealedRoot;
        public Image lightCircleImage;
        public Image lightGlowImage;

        [Header("Light Label (Image + Text)")]
        public GameObject lightLabelRoot;

        [Header("Line Anchors")]
        public RectTransform filmLineAnchor;
        public RectTransform lightLineAnchor;

        [Header("Bottom Film Card UI")]
        public Button filmButton;
        public GameObject filmTouchPromptRoot;
        public GameObject filmEmotionPopupRoot;

        [Header("Line UI (slot 전용)")]
        public RectTransform lineRect;
        public Image lineImage;
        public UILineConnector lineConnector;

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

    // =======================
    // 추가: Problem1 Step2 스타일(guide text + next button)
    // =======================
    [Header("Guide Text (Localized)")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdOnEnter = 0;
    [SerializeField] private int guideTextIdOnCompleted = 0;

    [Header("Next Shoot Button")]
    [SerializeField] private GameObject nextShootButtonRoot;
    [SerializeField] private Button nextShootButton;

    [Header("NextStep Auto Call")]
    [SerializeField] private bool autoCallNextStep = true;
    [SerializeField] private string stepFlowControllerTypeName = "StepFlowController";
    [SerializeField] private string nextStepMethodName = "NextStep";

    [Header("Fallback Callback")]
    [SerializeField] private UnityEvent onClickNextShootFallback;

    private bool _completed = false;
    private Component _flowController;
    private MethodInfo _nextStepMethod;

    // 내부 로직용 필드
    private Canvas _canvas;
    private RectTransform _canvasRect;

    protected override void OnStepEnter()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.transform as RectTransform;

        _completed = false;

        InitSlots();

        if (CompletionGate != null)
        {
            int total = (Slots != null) ? Slots.Length : 0;
            CompletionGate.ResetGate(total);
        }

        ApplyGuideText(guideTextIdOnEnter);
        SetNextShootButtonVisible(false);

        CacheStepFlowController();
        BindNextShootButton();
    }

    protected override void OnStepExit()
    {
        UnbindNextShootButton();
        _completed = false;
    }

    private void InitSlots()
    {
        if (Slots == null) return;

        foreach (var slot in Slots)
        {
            if (slot == null) continue;

            slot.revealed = false;

            if (slot.lightLabelRoot != null)
                slot.lightLabelRoot.SetActive(false);

            if (slot.lightCircleImage != null)
                slot.lightCircleImage.color = slot.color;

            if (slot.lightGlowImage != null)
            {
                var c = slot.color;
                c.a = slot.lightGlowImage.color.a;
                slot.lightGlowImage.color = c;
            }

            if (slot.lineRect != null)
            {
                if (slot.lineImage == null)
                    slot.lineImage = slot.lineRect.GetComponent<Image>();

                if (slot.lineConnector == null)
                    slot.lineConnector = slot.lineRect.GetComponent<UILineConnector>();

                if (slot.lineConnector != null)
                    slot.lineConnector.ResetLine();

                SetRevealState(slot, reveal: false, immediate: true);
            }

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

    private void OnFilmClicked(EmotionLightSlot slot)
    {
        if (slot.revealed)
            return;

        SetRevealState(slot, true);

        if (CompletionGate != null)
            CompletionGate.MarkOneDone();

        if (slot.lineRoutine != null)
            StopCoroutine(slot.lineRoutine);
        slot.lineRoutine = StartCoroutine(PlayUILine(slot));

        TryHandleCompleted();
    }

    private void TryHandleCompleted()
    {
        if (_completed) return;

        var slots = Slots;
        int total = (slots != null) ? slots.Length : 0;
        if (total <= 0) return;

        int revealedCount = 0;
        for (int i = 0; i < total; i++)
        {
            if (slots[i] != null && slots[i].revealed)
                revealedCount++;
        }

        if (revealedCount < total) return;

        _completed = true;

        ApplyGuideText(guideTextIdOnCompleted);
        SetNextShootButtonVisible(true);

        // 중요: 게이트가 자동으로 다음 스텝을 넘기는 구조면
        // StepCompletionGate에서 auto-next를 꺼주거나,
        // 게이트의 완료 버튼/자동 연결을 빼줘야 함.
    }

    private void ApplyGuideText(int textId)
    {
        if (guideText == null) return;
        guideText.text = ProblemRuntime.L(textId);
    }

    private void SetNextShootButtonVisible(bool visible)
    {
        if (nextShootButtonRoot != null)
            nextShootButtonRoot.SetActive(visible);

        if (nextShootButton != null)
            nextShootButton.interactable = visible;
    }

    private void BindNextShootButton()
    {
        if (nextShootButton == null) return;
        nextShootButton.onClick.RemoveListener(OnClickNextShoot);
        nextShootButton.onClick.AddListener(OnClickNextShoot);
    }

    private void UnbindNextShootButton()
    {
        if (nextShootButton == null) return;
        nextShootButton.onClick.RemoveListener(OnClickNextShoot);
    }

    private void OnClickNextShoot()
    {
        if (!_completed) return;

        if (autoCallNextStep && TryCallNextStep())
            return;

        onClickNextShootFallback?.Invoke();
    }

    private void CacheStepFlowController()
    {
        _flowController = null;
        _nextStepMethod = null;

        if (!autoCallNextStep) return;

        var monos = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < monos.Length; i++)
        {
            var mb = monos[i];
            if (mb == null) continue;

            var t = mb.GetType();
            if (t.Name != stepFlowControllerTypeName) continue;

            var mi = t.GetMethod(nextStepMethodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (mi == null) continue;
            if (mi.GetParameters().Length != 0) continue;

            _flowController = mb;
            _nextStepMethod = mi;
            break;
        }
    }

    private bool TryCallNextStep()
    {
        if (_flowController == null || _nextStepMethod == null)
        {
            CacheStepFlowController();
            if (_flowController == null || _nextStepMethod == null)
                return false;
        }

        _nextStepMethod.Invoke(_flowController, null);
        return true;
    }

    private IEnumerator PlayUILine(EmotionLightSlot slot)
    {
        if (slot.lineConnector == null && slot.lineRect != null)
            slot.lineConnector = slot.lineRect.GetComponent<UILineConnector>();

        if (slot.lineConnector == null ||
            slot.filmLineAnchor == null ||
            slot.lightLineAnchor == null)
        {
            slot.lineRoutine = null;
            yield break;
        }

        if (slot.lightLabelRoot != null && !slot.lightLabelRoot.activeSelf)
        {
            slot.lightLabelRoot.SetActive(true);
            StartCoroutine(PlayLightAppear(slot.lightLabelRoot.transform));
        }

        yield return slot.lineConnector.PlayLineRoutine(
            slot.filmLineAnchor,
            slot.lightLineAnchor
        );

        slot.lineRoutine = null;
    }

    private IEnumerator PlayLightAppear(Transform target)
    {
        if (target == null) yield break;

        Vector3 startScale = Vector3.one * 0.8f;
        Vector3 peakScale = Vector3.one * LightAppearScale;
        Vector3 endScale = Vector3.one;

        float t = 0f;
        target.localScale = startScale;
        float duration = LightAppearDuration;

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