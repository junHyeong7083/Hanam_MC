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

    [Header("요약 텍스트 ID 목록")]
    [SerializeField] private int[] summaryTextIds;

    [Header("라인 생성 설정")]
    [SerializeField] private GameObject linePrefab;   // Image + Text 포함된 프리팹
    [SerializeField] private Transform linesRoot;     // 생성된 라인들의 부모 Transform

    [Header("위치 설정")]
    [SerializeField] private SummaryLineConfig[] lineConfigs;

    [Header("타이밍")]
    [SerializeField] private float spawnInterval = 0.3f;  // 라인 간 생성 간격
    [SerializeField] private float moveDuration = 0.5f;   // spawn에서 target 이동 시간

    [Header("자동 다음 스텝")]
    [SerializeField] private StepCompletionGate completionGate;
    [SerializeField] private float autoAdvanceDelay = 5f;  // 마지막 라인 출력 후 대기 시간

    [Header("수동 넘기기 버튼 (선택)")]
    [SerializeField] private Button nextButton;

    private Coroutine _sequenceRoutine;
    private bool _advanced;

    private void OnEnable()
    {
        _advanced = false;
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnClickNext);
            nextButton.onClick.AddListener(OnClickNext);
        }
        StartSequence();
    }

    private void OnDisable()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnClickNext);

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    private void OnClickNext()
    {
        if (_advanced) return;
        _advanced = true;

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }

        if (completionGate != null)
        {
            completionGate.ResetGate(1);
            completionGate.MarkOneDone();
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
        if (summaryTextIds == null || summaryTextIds.Length == 0)
            yield break;

        int textCount = summaryTextIds.Length;
        int configCount = (lineConfigs != null) ? lineConfigs.Length : 0;
        int count = Mathf.Min(textCount, configCount);

        for (int i = 0; i < count; i++)
        {
            var cfg = lineConfigs[i];

            // --- fallback 처리 ---
            RectTransform spawn = cfg.spawnPoint;
            RectTransform target = cfg.targetPoint;

            if (spawn == null && lineConfigs.Length > 0)
            {
                spawn = lineConfigs[0].spawnPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} spawnPoint null → element0으로 대체");
            }

            if (target == null && lineConfigs.Length > 0)
            {
                target = lineConfigs[0].targetPoint;
                Debug.LogWarning($"[SummaryPanel] line {i} targetPoint null → element0으로 대체");
            }

            if (spawn == null || target == null || linePrefab == null || linesRoot == null)
            {
                Debug.LogWarning($"[SummaryPanel] line {i} 생성 불가 - 필수값 null 존재");
                continue;
            }

            // 1) 라인 프리팹 생성
            var go = Instantiate(linePrefab, linesRoot);
            go.name = $"SummaryLine_{i}";
            var rt = go.GetComponent<RectTransform>();

            // 프리팹 구조: linePrefab > Icon(첫번째 자식) > NumberText(Icon의 자식)
            //              linePrefab > DescriptionText
            Transform iconTr = go.transform.childCount > 0 ? go.transform.GetChild(0) : null;

            // 아이콘의 자식 텍스트에 번호 설정 (0번→"1", 1번→"2", ...)
            if (iconTr != null)
            {
                var numberText = iconTr.GetComponentInChildren<Text>();
                if (numberText != null)
                    numberText.text = (i + 1).ToString();
            }

            // 설명 텍스트 설정 (linePrefab 직속 Text 컴포넌트)
            var descTexts = go.GetComponentsInChildren<Text>();
            foreach (var t in descTexts)
            {
                // 아이콘 하위 텍스트는 건너뜀
                if (iconTr != null && t.transform.IsChildOf(iconTr))
                    continue;
                t.text = ProblemRuntime.L(summaryTextIds[i]);
                break;
            }

            // 2) 시작/목표 위치 설정 및 이동
            rt.position = spawn.position;
            StartCoroutine(MoveLine(rt, target.position, moveDuration));

            // 3) 다음 라인까지 interval 대기
            yield return new WaitForSeconds(spawnInterval);
        }

        // 마지막 라인 이동 완료 대기
        yield return new WaitForSeconds(moveDuration);

        // 자동 다음 스텝 진행
        if (completionGate != null && !_advanced)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
            if (_advanced) yield break;
            _advanced = true;
            completionGate.ResetGate(1);
            completionGate.MarkOneDone();
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
            lerp = Mathf.SmoothStep(0f, 1f, lerp); // 부드러운 이징

            rt.position = Vector3.Lerp(startPos, targetPos, lerp);
            yield return null;
        }

        rt.position = targetPos;
    }
}
