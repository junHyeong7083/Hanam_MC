using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem1_Step3_SummaryPanel : MonoBehaviour
{
    [Serializable]
    public struct SummaryLineConfig
    {
        public RectTransform spawnPoint;  // 시작 위치
        public RectTransform targetPoint; // 도착 위치
    }

    [Serializable]
    public struct SummaryDescription
    {
        public Sprite icon;          // 아이콘 스프라이트
        [TextArea]
        public string description;   // 설명 텍스트
    }

    [Header("요약 (아이콘 + 문장 세트)")]
    [SerializeField] private SummaryDescription[] summaryDescriptions;

    [Header("라인 생성 설정")]
    [SerializeField] private GameObject linePrefab;   // Image + Text 포함된 프리팹
    [SerializeField] private Transform linesRoot;     // 생성된 라인들이 붙을 부모

    [Header("위치 설정")]
    [SerializeField] private SummaryLineConfig[] lineConfigs;

    [Header("타이밍")]
    [SerializeField] private float spawnInterval = 0.3f;  // 줄마다 등장 간격
    [SerializeField] private float moveDuration = 0.5f;   // spawn → target 이동 시간

    [Header("하남 아이콘")]
    [SerializeField] private RectTransform hanamIcon;     // HanamIcon Image의 RectTransform
    [SerializeField] private float iconDelay = 0.3f;      // 마지막 줄 이후 아이콘 등장까지 딜레이
    [SerializeField] private float iconBobAmplitude = 5f; // 위/아래 흔들림 크기 (px)
    [SerializeField] private float iconBobSpeed = 2f;     // 흔들림 속도

    private Coroutine _sequenceRoutine;
    private Coroutine _iconBobRoutine;

    private void OnEnable()
    {
        // 패널 켜질 때 자동으로 시퀀스 시작
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

        // 다시 켜질 때를 위해 아이콘은 꺼둠
        if (hanamIcon != null)
            hanamIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 외부에서 요약 내용을 세팅하고 싶을 때 사용 (선택)
    /// </summary>
    public void SetSummaryContent(Sprite[] icons, string[] lines)
    {
        if (icons == null || lines == null)
        {
            summaryDescriptions = Array.Empty<SummaryDescription>();
            return;
        }

        int count = Mathf.Min(icons.Length, lines.Length);
        summaryDescriptions = new SummaryDescription[count];

        for (int i = 0; i < count; i++)
        {
            summaryDescriptions[i] = new SummaryDescription
            {
                icon = icons[i],
                description = lines[i]
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

        // 시작할 때 아이콘은 꺼두기
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

        Debug.Log($"[SummaryPanel] descriptions={descCount}, configs={configCount}, loopCount={count}");

        for (int i = 0; i < count; i++)
        {
            var data = summaryDescriptions[i];
            var cfg = lineConfigs[i];

            // --- fallback 준비 ---
            RectTransform spawn = cfg.spawnPoint;
            RectTransform target = cfg.targetPoint;

            // spawn/target 이 비어 있으면 0번 설정을 대신 사용
            if (spawn == null && lineConfigs.Length > 0)
            {
                spawn = lineConfigs[0].spawnPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} spawnPoint null → element0 로 대체");
            }

            if (target == null && lineConfigs.Length > 0)
            {
                target = lineConfigs[0].targetPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} targetPoint null → element0 로 대체");
            }

            // 그래도 아직 뭔가 심각하게 null 이면 그냥 로그만 남기고 계속 진행
            if (spawn == null || target == null || linePrefab == null || linesRoot == null)
            {
                Debug.LogWarning($"[SummaryPanel] line {i} 생성 실패 - 여전히 null 있음");
                continue;
            }

            // 1) 라인 프리팹 생성
            var go = Instantiate(linePrefab, linesRoot);
            go.name = $"SummaryLine_{i}";
            var rt = go.GetComponent<RectTransform>();

            var iconImage = go.GetComponentInChildren<Image>();
            var textComp = go.GetComponentInChildren<Text>();

            if (iconImage != null)
                iconImage.sprite = data.icon;
            if (textComp != null)
                textComp.text = data.description;

            Debug.Log($"[SummaryPanel] line {i} 생성 - \"{data.description}\"");

            // 2) 시작/목표 위치
            rt.position = spawn.position;
            StartCoroutine(MoveLine(rt, target.position, moveDuration));

            // 3) 다음 줄은 interval 후에
            yield return new WaitForSeconds(spawnInterval);
        }

        // 하남 아이콘
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
            lerp = Mathf.SmoothStep(0f, 1f, lerp); // 부드러운 곡선

            rt.position = Vector3.Lerp(startPos, targetPos, lerp);
            yield return null;
        }

        rt.position = targetPos;
    }

    /// <summary>
    /// 하남 아이콘 위/아래로 살짝살짝 흔들리는 애니메이션
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

        // 꺼질 때 위치를 원래대로 복구
        if (icon != null)
            icon.anchoredPosition = basePos;
    }
}
