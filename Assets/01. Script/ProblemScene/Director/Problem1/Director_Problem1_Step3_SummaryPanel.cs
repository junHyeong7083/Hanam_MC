using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem1_Step3_SummaryPanel : MonoBehaviour
{
    [Serializable]
    public struct SummaryLineConfig
    {
        public RectTransform spawnPoint;
        public RectTransform targetPoint;
    }

    [Header("타이틀")]
    [SerializeField] private Text titleText;
    [SerializeField] private int titleTextId;

    [Header("요약 텍스트 ID 목록")]
    [SerializeField] private int[] summaryTextIds;

    [Header("라인 생성 설정")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Transform linesRoot;

    [Header("위치 설정")]
    [SerializeField] private SummaryLineConfig[] lineConfigs;

    [Header("타이밍")]
    [SerializeField] private float spawnInterval = 0.3f;
    [SerializeField] private float moveDuration = 0.5f;

    [Header("다음 스텝 처리")]
    [SerializeField] private StepCompletionGate completionGate;

    private Coroutine _sequenceRoutine;

    private void OnEnable()
    {
        if (completionGate != null)
            completionGate.ResetGate(1);

        StartSequence();
    }

    private void OnDisable()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    public void SetSummaryContent(int[] textIds)
    {
        summaryTextIds = textIds ?? Array.Empty<int>();
    }

    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

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
        // 타이틀 세팅
        if (titleText != null && titleTextId != 0)
            titleText.text = ProblemRuntime.L(titleTextId);

        if (summaryTextIds == null || summaryTextIds.Length == 0)
            yield break;

        if (linePrefab == null || linesRoot == null)
        {
            Debug.LogWarning("[SummaryPanel] linePrefab 또는 linesRoot가 null 입니다.");
            yield break;
        }

        int textCount = summaryTextIds.Length;
        int configCount = (lineConfigs != null) ? lineConfigs.Length : 0;
        int count = Mathf.Min(textCount, configCount);

        if (count <= 0)
        {
            Debug.LogWarning("[SummaryPanel] summaryTextIds 또는 lineConfigs가 비어있습니다.");
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            var cfg = lineConfigs[i];

            RectTransform spawn = cfg.spawnPoint;
            RectTransform target = cfg.targetPoint;

            if (spawn == null && lineConfigs.Length > 0)
            {
                spawn = lineConfigs[0].spawnPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} spawnPoint null -> element0으로 대체");
            }

            if (target == null && lineConfigs.Length > 0)
            {
                target = lineConfigs[0].targetPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} targetPoint null -> element0으로 대체");
            }

            if (spawn == null || target == null)
            {
                Debug.LogWarning($"[SummaryPanel] line {i} 생성 불가 - spawn/target null");
                continue;
            }

            var go = Instantiate(linePrefab, linesRoot);
            go.name = $"SummaryLine_{i}";

            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogWarning($"[SummaryPanel] line {i} RectTransform이 없습니다.");
                Destroy(go);
                continue;
            }

            // 프리팹 구조 가정:
            // linePrefab
            //  - Icon(0)
            //    - NumberText (Icon 내부 Text)
            //  - DescriptionText (linePrefab 하위 Text)
            Transform iconTr = go.transform.childCount > 0 ? go.transform.GetChild(0) : null;

            if (iconTr != null)
            {
                var numberText = iconTr.GetComponentInChildren<Text>();
                if (numberText != null)
                    numberText.text = (i + 1).ToString();
            }

            // 설명 텍스트 설정
            var descTexts = go.GetComponentsInChildren<Text>();
            for (int tIdx = 0; tIdx < descTexts.Length; tIdx++)
            {
                var t = descTexts[tIdx];
                if (iconTr != null && t.transform.IsChildOf(iconTr))
                    continue;

                t.text = ProblemRuntime.L(summaryTextIds[i]);
                break;
            }

            // 시작 위치에서 목표 위치로 이동
            rt.position = spawn.position;
            StartCoroutine(MoveLine(rt, target.position, moveDuration));

            yield return new WaitForSeconds(spawnInterval);
        }

        // 마지막 라인 이동 완료까지 대기
        yield return new WaitForSeconds(moveDuration);

        // 여기서 완료 처리 -> StepCompletionGate가 completeRoot(버튼 루트)를 켬
        if (completionGate != null)
            completionGate.MarkOneDone();
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
            lerp = Mathf.SmoothStep(0f, 1f, lerp);

            rt.position = Vector3.Lerp(startPos, targetPos, lerp);
            yield return null;
        }

        rt.position = targetPos;
    }
}