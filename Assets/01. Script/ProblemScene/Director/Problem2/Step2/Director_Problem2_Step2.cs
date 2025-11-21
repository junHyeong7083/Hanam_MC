using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Director / Problem2 / Step2
/// - 아래 필름 카드를 클릭하면 위쪽 감정 조명이 켜짐.
/// - 클릭 시 필름 ↔ 조명 사이에 UI 라인(Image) 이펙트가 재생됨.
/// - 모두 켜지면 완료 버튼이 활성화되고, 눌렀을 때 onComplete 호출.
/// </summary>
public class Director_Problem2_Step2 : MonoBehaviour
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

    [Header("완료 UI")]
    [SerializeField] private GameObject completeButtonRoot;

    private void OnEnable()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.transform as RectTransform;

        InitSlots();
        Debug.Log("sibal");

        if (completeButtonRoot != null)
            completeButtonRoot.SetActive(false);
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
            {
                slot.lightLabelRoot.SetActive(false);
            }

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

                slot.lineRect.gameObject.SetActive(false);
                slot.lineRect.pivot = new Vector2(0f, 0.5f); // 왼쪽에서 오른쪽으로 자라게

                var sd = slot.lineRect.sizeDelta;
                sd.y = lineMaxThickness;
                sd.x = 0f;
                slot.lineRect.sizeDelta = sd;
            }

            // 잠긴 상태로 시작
            SetRevealState(slot, false, immediate: true);

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

        // 2) 모든 슬롯이 열린 상태라면 완료 버튼 On
        if (completeButtonRoot != null && CheckAllRevealed())
        {
            Debug.Log("[OnFilmClicked] all revealed → show completeButtonRoot");
            completeButtonRoot.SetActive(true);
        }

        // 어떤게 null인지 다 찍어보기
        if (slot.lineRect == null) Debug.LogWarning("[OnFilmClicked] lineRect is NULL");
        if (slot.lineImage == null) Debug.LogWarning("[OnFilmClicked] lineImage is NULL");
        if (_canvasRect == null) Debug.LogWarning("[OnFilmClicked] _canvasRect is NULL");
        if (slot.filmLineAnchor == null) Debug.LogWarning("[OnFilmClicked] filmLineAnchor is NULL");
        if (slot.lightLineAnchor == null) Debug.LogWarning("[OnFilmClicked] lightLineAnchor is NULL");

        if (slot.lineRect != null && slot.lineImage != null &&
            _canvasRect != null &&
            slot.filmLineAnchor != null &&
            slot.lightLineAnchor != null)
        {
            if (slot.lineRoutine != null)
                StopCoroutine(slot.lineRoutine);

            Debug.Log("[OnFilmClicked] StartCoroutine(PlayUILine)");
            slot.lineRoutine = StartCoroutine(PlayUILine(slot));
        }
    }

    private bool CheckAllRevealed()
    {
        if (slots == null || slots.Length == 0) return false;

        foreach (var s in slots)
        {
            if (s == null) continue;
            if (!s.revealed) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------
    // 슬롯별 UI Image 선 애니메이션
    // ------------------------------------------------------------------
    private IEnumerator PlayUILine(EmotionLightSlot slot)
    {
        var lineRect = slot.lineRect;
        var lineImage = slot.lineImage;

        // ★ 여기서 라인 오브젝트랑 이미지 무조건 켜기
        if (!lineRect.gameObject.activeSelf)
            lineRect.gameObject.SetActive(true);
        if (!lineImage.enabled)
            lineImage.enabled = true;

        Debug.Log($"[PlayUILine] start slot={slot.emotionText}");

        // Label은 한 번만 등장 애니메이션
        if (slot.lightLabelRoot != null && !slot.lightLabelRoot.activeSelf)
        {
            slot.lightLabelRoot.SetActive(true);
            StartCoroutine(PlayLightAppear(slot.lightLabelRoot.transform));
        }

        Vector2 startLocal;
        Vector2 endLocal;

        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, slot.filmLineAnchor.position),
            cam,
            out startLocal);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            RectTransformUtility.WorldToScreenPoint(cam, slot.lightLineAnchor.position),
            cam,
            out endLocal);

        Vector2 dir = endLocal - startLocal;
        float length = dir.magnitude;

        if (length <= 0.0001f)
        {
            Debug.LogWarning("[PlayUILine] length too small, abort");
            lineRect.gameObject.SetActive(false);
            slot.lineRoutine = null;
            yield break;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 라인 RectTransform 세팅
        lineRect.SetParent(_canvasRect, worldPositionStays: false);
        lineRect.SetAsLastSibling(); // ★ 제일 위로
        lineRect.anchoredPosition = startLocal;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        var sd = lineRect.sizeDelta;
        sd.y = lineMaxThickness;
        sd.x = 0f;
        lineRect.sizeDelta = sd;

        var col = slot.color;
        col.a = 1f;
        lineImage.color = col;

        // 1) 시작점 → 끝점까지 그려지는 구간
        float t = 0f;
        while (t < lineDrawDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / lineDrawDuration);

            float widthFactor = lineWidthCurve.Evaluate(x);
            sd.y = lineMaxThickness * widthFactor;
            sd.x = length * x;
            lineRect.sizeDelta = sd;

            yield return null;
        }

        sd.x = length;
        lineRect.sizeDelta = sd;
        lineRect.anchoredPosition = startLocal;

        if (lineHoldDuration > 0f)
            yield return new WaitForSeconds(lineHoldDuration);

        // 3) 시작 → 끝 방향으로 말려들어가며 + 알파 페이드 아웃
        float fadeT = 0f;
        while (fadeT < lineFadeDuration)
        {
            fadeT += Time.deltaTime;
            float x = Mathf.Clamp01(fadeT / lineFadeDuration);
            float inv = 1f - x;

            Vector2 currentStart = Vector2.Lerp(startLocal, endLocal, x);
            float currentLength = length * inv;

            lineRect.anchoredPosition = currentStart;
            sd.x = currentLength;
            lineRect.sizeDelta = sd;

            var c = lineImage.color;
            c.a = inv;
            lineImage.color = c;

            yield return null;
        }

        lineRect.gameObject.SetActive(false);
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
