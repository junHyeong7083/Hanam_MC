using UnityEngine;
using System.Collections;

/// <summary>
/// Director / Problem_3 / Step1
/// - DB에서 시나리오 펜 보유 확인 후 자동 활성화
/// </summary>
public class Director_Problem3_Step1 : InventoryDropTargetStepBase
{
    [Header("Book Activation")]
    [SerializeField] private RectTransform bookVisualRoot;
    [SerializeField] private float activateScale = 1.05f;
    [SerializeField] private float activateDuration = 0.6f;
    [SerializeField] private float delayBeforeComplete = 1.5f;

    [Header("Camera Shutter Effect")]
    [SerializeField] private GameObject cameraShutterPrefab;

    [Header("Sparkle Effect")]
    [SerializeField] private GameObject sparkleEffectPrefab;
    [SerializeField] private int sparkleCount = 6;
    [SerializeField] private float sparkleSpreadMin = 30f;
    [SerializeField] private float sparkleSpreadMax = 100f;

    [Header("Flicker")]
    [SerializeField] private bool enableFlicker = true;
    [SerializeField] private int flickerCount = 4;
    [SerializeField] private float flickerDuration = 0.6f;
    [SerializeField] private float flickerMinAlpha = 0.5f;

    [Header("Instruction")]
    [SerializeField] private GameObject instructionRoot;

    [Header("Completion Gate (Optional)")]
    [SerializeField] private StepCompletionGate completionGate;

    // === Base Properties ===
    protected override RectTransform TargetVisualRoot => bookVisualRoot;
    protected override GameObject InstructionRoot => instructionRoot;
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float ActivateScale => activateScale;
    protected override float ActivateDuration => activateDuration;
    protected override float DelayBeforeComplete => delayBeforeComplete;

    // Internal
    private CanvasGroup _targetCanvasGroup;

    protected override IEnumerator PlayActivateAnimation()
    {
        var visual = TargetVisualRoot;
        if (visual == null || ActivateDuration <= 0f)
            yield break;

        if (_targetCanvasGroup == null)
        {
            _targetCanvasGroup = visual.GetComponent<CanvasGroup>();
            if (_targetCanvasGroup == null)
                _targetCanvasGroup = visual.gameObject.AddComponent<CanvasGroup>();
        }

        GameObject shutterInstance = null;
        if (cameraShutterPrefab != null)
        {
            shutterInstance = Instantiate(cameraShutterPrefab, visual);
            var rt = shutterInstance.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = Vector2.zero;
            shutterInstance.SetActive(false);
        }

        SpawnSparkleEffects(visual);

        float totalDuration = Mathf.Max(activateDuration, enableFlicker ? flickerDuration : 0f);
        float t = 0f;
        int flickerIndex = 0;
        float flickerInterval = flickerCount > 0 ? flickerDuration / (flickerCount * 2) : 0f;
        float nextFlickerTime = 0f;
        bool flickerOn = true;

        while (t < totalDuration)
        {
            t += Time.deltaTime;

            if (t < activateDuration)
            {
                float scaleT = Mathf.Clamp01(t / 0.3f);
                float scale = Mathf.Lerp(1f, activateScale, scaleT);
                visual.localScale = Vector3.one * scale;
            }

            if (enableFlicker && _targetCanvasGroup != null && t < flickerDuration)
            {
                if (t >= nextFlickerTime && flickerIndex < flickerCount * 2)
                {
                    flickerOn = !flickerOn;
                    _targetCanvasGroup.alpha = flickerOn ? 1f : flickerMinAlpha;

                    if (shutterInstance != null)
                        shutterInstance.SetActive(!flickerOn);

                    nextFlickerTime += flickerInterval;
                    flickerIndex++;
                }
            }

            yield return null;
        }

        visual.localScale = Vector3.one * activateScale;
        if (_targetCanvasGroup != null)
            _targetCanvasGroup.alpha = 1f;

        if (shutterInstance != null)
            Destroy(shutterInstance);
    }

    private void SpawnSparkleEffects(RectTransform parent)
    {
        if (sparkleEffectPrefab == null || sparkleCount <= 0)
            return;

        for (int i = 0; i < sparkleCount; i++)
        {
            var sparkle = Instantiate(sparkleEffectPrefab, parent);
            var rt = sparkle.GetComponent<RectTransform>();
            if (rt != null)
            {
                float angle = (360f / sparkleCount) * i + Random.Range(-15f, 15f);
                float distance = Random.Range(sparkleSpreadMin, sparkleSpreadMax);
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * distance;
                rt.anchoredPosition = offset;
            }
        }
    }
}
