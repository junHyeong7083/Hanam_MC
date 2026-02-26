using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem1_Step3_SummaryPanel : MonoBehaviour
{
    [Serializable]
    public struct SummaryLineConfig
    {
        public RectTransform spawnPoint;  // ���� ��ġ
        public RectTransform targetPoint; // ���� ��ġ
    }

    [Serializable]
    public struct SummaryDescription
    {
        public Sprite icon;
        public int descriptionTextId;
    }

    [Header("��� (������ + ���� ��Ʈ)")]
    [SerializeField] private SummaryDescription[] summaryDescriptions;

    [Header("���� ���� ����")]
    [SerializeField] private GameObject linePrefab;   // Image + Text ���Ե� ������
    [SerializeField] private Transform linesRoot;     // ������ ���ε��� ���� �θ�

    [Header("��ġ ����")]
    [SerializeField] private SummaryLineConfig[] lineConfigs;

    [Header("Ÿ�̹�")]
    [SerializeField] private float spawnInterval = 0.3f;  // �ٸ��� ���� ����
    [SerializeField] private float moveDuration = 0.5f;   // spawn �� target �̵� �ð�

    [Header("�ϳ� ������")]
    [SerializeField] private RectTransform hanamIcon;     // HanamIcon Image�� RectTransform
    [SerializeField] private float iconDelay = 0.3f;      // ������ �� ���� ������ ������� ������
    [SerializeField] private float iconBobAmplitude = 5f; // ��/�Ʒ� ��鸲 ũ�� (px)
    [SerializeField] private float iconBobSpeed = 2f;     // ��鸲 �ӵ�

    private Coroutine _sequenceRoutine;
    private Coroutine _iconBobRoutine;

    private void OnEnable()
    {
        // �г� ���� �� �ڵ����� ������ ����
        StartSequence();
    }

    private void OnDisable()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        if (_iconBobRoutine != null)
        {
            StopCoroutine(_iconBobRoutine);
            _iconBobRoutine = null;
        }

        // �ٽ� ���� ���� ���� �������� ����
        if (hanamIcon != null)
            hanamIcon.gameObject.SetActive(false);
    }

    public void SetSummaryContent(Sprite[] icons, int[] textIds)
    {
        if (icons == null || textIds == null)
        {
            summaryDescriptions = Array.Empty<SummaryDescription>();
            return;
        }

        int count = Mathf.Min(icons.Length, textIds.Length);
        summaryDescriptions = new SummaryDescription[count];

        for (int i = 0; i < count; i++)
        {
            summaryDescriptions[i] = new SummaryDescription
            {
                icon = icons[i],
                descriptionTextId = textIds[i]
            };
        }
    }

    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        if (_iconBobRoutine != null)
        {
            StopCoroutine(_iconBobRoutine);
            _iconBobRoutine = null;
        }

        // ������ �� �������� ���α�
        if (hanamIcon != null)
            hanamIcon.gameObject.SetActive(false);

        ClearLines();
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    private void ClearLines()
    {
        if (linesRoot == null) return;

        for (int i = linesRoot.childCount - 1; i >= 0; i--)
            Destroy(linesRoot.GetChild(i).gameObject);
    }

    private IEnumerator SequenceRoutine()
    {
        if (summaryDescriptions == null || summaryDescriptions.Length == 0)
            yield break;

        int descCount = summaryDescriptions.Length;
        int configCount = (lineConfigs != null) ? lineConfigs.Length : 0;
        int count = Mathf.Min(descCount, configCount);

        //Debug.Log($"[SummaryPanel] descriptions={descCount}, configs={configCount}, loopCount={count}");

        for (int i = 0; i < count; i++)
        {
            var data = summaryDescriptions[i];
            var cfg = lineConfigs[i];

            // --- fallback �غ� ---
            RectTransform spawn = cfg.spawnPoint;
            RectTransform target = cfg.targetPoint;

            // spawn/target �� ��� ������ 0�� ������ ��� ���
            if (spawn == null && lineConfigs.Length > 0)
            {
                spawn = lineConfigs[0].spawnPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} spawnPoint null �� element0 �� ��ü");
            }

            if (target == null && lineConfigs.Length > 0)
            {
                target = lineConfigs[0].targetPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} targetPoint null �� element0 �� ��ü");
            }

            // �׷��� ���� ���� �ɰ��ϰ� null �̸� �׳� �α׸� ����� ��� ����
            if (spawn == null || target == null || linePrefab == null || linesRoot == null)
            {
                Debug.LogWarning($"[SummaryPanel] line {i} ���� ���� - ������ null ����");
                continue;
            }

            // 1) ���� ������ ����
            var go = Instantiate(linePrefab, linesRoot);
            go.name = $"SummaryLine_{i}";
            var rt = go.GetComponent<RectTransform>();

            var iconImage = go.GetComponentInChildren<Image>();
            var textComp = go.GetComponentInChildren<Text>();

            if (iconImage != null)
                iconImage.sprite = data.icon;
            if (textComp != null)
                textComp.text = ProblemRuntime.L(data.descriptionTextId);

           // Debug.Log($"[SummaryPanel] line {i} ���� - \"{data.description}\"");

            // 2) ����/��ǥ ��ġ
            rt.position = spawn.position;
            StartCoroutine(MoveLine(rt, target.position, moveDuration));

            // 3) ���� ���� interval �Ŀ�
            yield return new WaitForSeconds(spawnInterval);
        }

        // �ϳ� ������
        if (hanamIcon != null)
        {
            yield return new WaitForSeconds(iconDelay);

            hanamIcon.gameObject.SetActive(true);
            _iconBobRoutine = StartCoroutine(BobHanamIcon(hanamIcon));
        }
    }


    private IEnumerator MoveLine(RectTransform rt, Vector3 targetPos, float duration)
    {
        if (rt == null) yield break;

        Vector3 startPos = rt.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            lerp = Mathf.SmoothStep(0f, 1f, lerp); // �ε巯�� �

            rt.position = Vector3.Lerp(startPos, targetPos, lerp);
            yield return null;
        }

        rt.position = targetPos;
    }

    /// <summary>
    /// �ϳ� ������ ��/�Ʒ��� ��¦��¦ ��鸮�� �ִϸ��̼�
    /// </summary>
    private IEnumerator BobHanamIcon(RectTransform icon)
    {
        if (icon == null) yield break;

        Vector2 basePos = icon.anchoredPosition;
        float time = 0f;

        while (icon != null && icon.gameObject.activeInHierarchy)
        {
            time += Time.deltaTime * iconBobSpeed;
            float offsetY = Mathf.Sin(time) * iconBobAmplitude;
            icon.anchoredPosition = basePos + new Vector2(0f, offsetY);
            yield return null;
        }

        // ���� �� ��ġ�� ������� ����
        if (icon != null)
            icon.anchoredPosition = basePos;
    }
}
