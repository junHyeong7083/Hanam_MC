using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem1_Step3_SummaryPanel - 문제1 스텝3 요약 패널 컨트롤러.
///
/// 【역할】 필름 분류 완료 후 활성화되는 요약 패널을 관리한다.
///         타이틀 텍스트를 설정하고, summaryTextIds 배열의 각 텍스트를 순차적으로
///         라인 프리팹으로 생성하여 애니메이션과 함께 표시한다.
///         사용자가 화면을 클릭하면 다음 라인이 등장하고, 마지막 라인까지 보면
///         StepCompletionGate로 스텝을 완료시킨다.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝3의 요약 화면
/// 【부모 클래스】 MonoBehaviour (독립 컴포넌트, OnEnable에서 시퀀스 시작)
/// 【참조하는 곳】 Director_Problem1_Step3_Logic (SummaryPanelRoot로 활성화)
/// 【참조되는 곳】 ProblemRuntime.L() (CSV 텍스트), StepCompletionGate (완료 처리)
/// 【흐름】 패널 활성화(OnEnable) → 타이틀 설정 → 첫 라인 표시 → 클릭마다 다음 라인 →
///         마지막 라인 → 완료 게이트 처리 → 다음 스텝
/// </summary>
public class Director_Problem1_Step3_SummaryPanel : MonoBehaviour
{
    /// <summary>각 라인의 시작 위치(spawnPoint)와 목표 위치(targetPoint) 설정</summary>
    [Serializable]
    public struct SummaryLineConfig
    {
        public RectTransform spawnPoint;    // 라인이 생성되는 시작 위치
        public RectTransform targetPoint;   // 라인이 이동할 최종 위치
    }

    [Header("타이틀")]
    [SerializeField] private Text titleText;       // 요약 패널 상단 타이틀 텍스트
    [SerializeField] private int titleTextId;      // 타이틀 CSV textId

    [Header("요약 텍스트 ID 목록")]
    [SerializeField] private int[] summaryTextIds; // 각 라인에 표시할 CSV textId 배열

    [Header("라인 생성 설정")]
    [SerializeField] private GameObject linePrefab; // 요약 라인 프리팹 (Icon + DescriptionText 구조)
    [SerializeField] private Transform linesRoot;   // 라인이 생성될 부모 Transform

    [Header("위치 설정")]
    [SerializeField] private SummaryLineConfig[] lineConfigs;  // 각 라인별 시작/목표 위치 설정

    [Header("타이밍")]
    [SerializeField] private float moveDuration = 0.5f;  // 라인 이동 애니메이션 시간 (초)

    [Header("다음 클릭 버튼 (투명 1920x1080)")]
    [SerializeField] private Button nextLineButton;      // 화면 전체를 덮는 투명 버튼 (다음 라인 트리거)

    [Header("다음 스텝 처리")]
    [SerializeField] private StepCompletionGate completionGate;  // 모든 라인 표시 후 스텝 완료

    private int _currentLineIndex;   // 현재 표시 중인 라인 인덱스
    private int _totalLineCount;     // 총 표시할 라인 수 (textIds와 configs의 최소값)
    private bool _isAnimating;       // 라인 이동 애니메이션 진행 중 여부

    /// <summary>패널 활성화 시 호출. 완료 게이트 리셋, 버튼 리스너 설정, 시퀀스 시작.</summary>
    private void OnEnable()
    {
        if (completionGate != null)
            completionGate.ResetGate(1);

        if (nextLineButton != null)
        {
            nextLineButton.onClick.RemoveAllListeners();
            nextLineButton.onClick.AddListener(OnNextLineClicked);
        }

        StartSequence();
    }

    private void OnDisable()
    {
        if (nextLineButton != null)
            nextLineButton.onClick.RemoveAllListeners();
    }

    /// <summary>외부에서 요약 텍스트 ID 배열을 동적으로 설정할 때 사용한다.</summary>
    public void SetSummaryContent(int[] textIds)
    {
        summaryTextIds = textIds ?? Array.Empty<int>();
    }

    /// <summary>요약 시퀀스를 시작한다. 기존 라인을 정리하고 첫 라인을 표시한다.</summary>
    public void StartSequence()
    {
        ClearLines();
        PrepareSequence();
    }

    /// <summary>linesRoot 아래 기존 라인 오브젝트를 모두 파괴한다.</summary>
    private void ClearLines()
    {
        if (linesRoot == null) return;

        for (int i = linesRoot.childCount - 1; i >= 0; i--)
            Destroy(linesRoot.GetChild(i).gameObject);
    }

    /// <summary>시퀀스 초기화: 타이틀 설정, 총 라인 수 계산, 첫 라인 즉시 표시.</summary>
    private void PrepareSequence()
    {
        // 타이틀 세팅
        if (titleText != null && titleTextId != 0)
            titleText.text = ProblemRuntime.L(titleTextId);

        int textCount = (summaryTextIds != null) ? summaryTextIds.Length : 0;
        int configCount = (lineConfigs != null) ? lineConfigs.Length : 0;
        _totalLineCount = Mathf.Min(textCount, configCount);
        _currentLineIndex = 0;
        _isAnimating = false;

        if (_totalLineCount <= 0)
        {
            Debug.LogWarning("[SummaryPanel] summaryTextIds 또는 lineConfigs가 비어있습니다.");
            return;
        }

        // 첫 번째 라인 즉시 표시
        SpawnLine(_currentLineIndex);
    }

    /// <summary>
    /// 화면 클릭 시 다음 라인을 표시한다.
    /// 마지막 라인이면 버튼을 숨기고 완료 게이트를 처리한다.
    /// </summary>
    private void OnNextLineClicked()
    {
        if (_isAnimating) return;

        _currentLineIndex++;

        if (_currentLineIndex >= _totalLineCount) return;

        SpawnLine(_currentLineIndex);

        // 마지막 라인이면 즉시 버튼 비활성화 + 완료 처리
        if (_currentLineIndex >= _totalLineCount - 1)
        {
            if (nextLineButton != null)
                nextLineButton.gameObject.SetActive(false);

            if (completionGate != null)
                completionGate.MarkOneDone();
        }
    }

    /// <summary>
    /// i번째 라인을 생성하고, spawnPoint에서 targetPoint로 이동 애니메이션을 재생한다.
    /// 프리팹 구조: linePrefab > Icon(자식0) > NumberText + DescriptionText(직계 자식)
    /// </summary>
    private void SpawnLine(int i)
    {
        if (linePrefab == null || linesRoot == null)
        {
            Debug.LogWarning("[SummaryPanel] linePrefab 또는 linesRoot가 null 입니다.");
            return;
        }

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
            return;
        }

        var go = Instantiate(linePrefab, linesRoot);
        go.name = $"SummaryLine_{i}";

        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogWarning($"[SummaryPanel] line {i} RectTransform이 없습니다.");
            Destroy(go);
            return;
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
        StartCoroutine(MoveLineWithLock(rt, target.position, moveDuration));
    }

    /// <summary>라인 이동 애니메이션 + 잠금 제어 래퍼 코루틴.</summary>
    private IEnumerator MoveLineWithLock(RectTransform rt, Vector3 targetPos, float duration)
    {
        _isAnimating = true;
        yield return StartCoroutine(MoveLine(rt, targetPos, duration));
        _isAnimating = false;
    }

    /// <summary>SmoothStep 보간으로 RectTransform을 목표 위치로 이동시키는 코루틴.</summary>
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