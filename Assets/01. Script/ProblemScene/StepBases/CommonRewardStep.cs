using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 공용 보상 연출 Step
/// - ProblemStepBase 를 상속
/// - 여러 UI 요소를 배열(sequenceItems)로 받아 순차적으로 등장
/// - 보상 DB 저장 + 리워드 이름/설명 텍스트 표시
/// </summary>
public class CommonRewardStep : ProblemStepBase
{
    [Serializable]
    public class SequenceItem
    {
        [Header("디버그/설명용 이름 (선택)")]
        public string name;

        [Header("UI Root")]
        public RectTransform root;
        public CanvasGroup canvasGroup;

        [Header("타이밍")]
        [Tooltip("이전 아이템이 끝난 후 기다릴 시간")]
        public float delay = 0f;

        [Tooltip("이 아이템의 등장 애니메이션 시간")]
        public float duration = 0.4f;

        [Header("위치/스케일 연출")]
        [Tooltip("basePos + startOffset 위치에서 시작")]
        public Vector2 startOffset = Vector2.zero;

        [Tooltip("스케일 애니메이션을 사용할지 여부")]
        public bool useScale = false;
        public float startScale = 1f;

        [Tooltip("스케일 오버슈트(통통 튀는 효과) 사용 여부")]
        public bool useOvershoot = false;
        public float overshootScale = 1.1f;

        // --- 내부 캐시 ---
        [NonSerialized] public bool initialized;
        [NonSerialized] public Vector2 basePos;
        [NonSerialized] public Vector3 baseScale;
    }

    [Header("연출 시퀀스 (위에서 아래 순서대로 재생)")]
    [SerializeField] private SequenceItem[] sequenceItems;

    [Header("아이템 이름 텍스트")]
    [SerializeField] private Text itemNameText;
    [SerializeField] private int itemNameTextId;

    [Header("효과 설명 텍스트")]
    [SerializeField] private Text effectDescText;
    [SerializeField] private int effectDescTextId;

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("버튼 (enterTextIds 완료 후 표시)")]
    [SerializeField] private Button homeButton;

    [Header("보상 메타 (DB 저장용)")]
    [SerializeField] private string rewardItemId = "mind_lens";
    [SerializeField] private string rewardItemName = "마음 렌즈";

    // 내부 상태
    private Coroutine _sequenceRoutine;
    private bool _rewardSaved;

    [Serializable]
    public class StepRewardItemDto
    {
        public string itemId;
        public string itemName;
        public bool unlocked;
    }

    [Serializable]
    public class StepRewardAttemptDto
    {
        public StepRewardItemDto[] items;
    }

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        SaveRewardToDbOnce();
        ApplyRewardText();

        // 버튼 초기 숨김
        if (homeButton != null)
            homeButton.gameObject.SetActive(false);

        // DialogueSequencer 마지막 enterText 표시 시 버튼 표시
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnEnterComplete;
        else if (homeButton != null)
            homeButton.gameObject.SetActive(true);

        StartSequence();
    }

    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnEnterComplete;

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    private void OnEnterComplete()
    {
        if (homeButton != null)
            homeButton.gameObject.SetActive(true);
    }

    // =========================
    // 리워드 텍스트 표시
    // =========================

    private void ApplyRewardText()
    {
        if (itemNameText != null && itemNameTextId > 0)
            itemNameText.text = ProblemRuntime.L(itemNameTextId);

        if (effectDescText != null && effectDescTextId > 0)
            effectDescText.text = ProblemRuntime.L(effectDescTextId);
    }

    // =========================
    // 시퀀스 제어
    // =========================

    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        InitState();
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    private void InitState()
    {
        if (sequenceItems == null) return;

        foreach (var item in sequenceItems)
        {
            if (item == null || item.root == null)
                continue;

            if (!item.initialized)
            {
                item.basePos = item.root.anchoredPosition;
                item.baseScale = item.root.localScale;
                item.initialized = true;
            }

            item.root.anchoredPosition = item.basePos + item.startOffset;

            if (item.useScale)
                item.root.localScale = Vector3.one * item.startScale;
            else
                item.root.localScale = item.baseScale;

            if (item.canvasGroup != null)
                item.canvasGroup.alpha = 0f;
        }
    }

    private IEnumerator SequenceRoutine()
    {
        if (sequenceItems == null || sequenceItems.Length == 0)
            yield break;

        for (int i = 0; i < sequenceItems.Length; i++)
        {
            var item = sequenceItems[i];
            if (item == null || item.root == null)
                continue;

            if (item.delay > 0f)
                yield return new WaitForSeconds(item.delay);

            yield return PlayItemRoutine(item);

            // 시퀀스 아이템 등장 완료 후 SweepHighlightTrigger 실행
            if (item.root != null)
            {
                var sweep = item.root.GetComponentInChildren<SweepHighlightTrigger>(true);
                if (sweep != null)
                    sweep.PlaySweep();
            }
        }
    }

    private IEnumerator PlayItemRoutine(SequenceItem item)
    {
        if (item == null || item.root == null)
            yield break;

        float duration = Mathf.Max(0.001f, item.duration);
        float t = 0f;

        Vector2 startPos = item.root.anchoredPosition;
        Vector2 endPos = item.basePos;

        Vector3 baseScale = item.baseScale;
        Vector3 startScale = item.useScale ? Vector3.one * item.startScale : baseScale;
        Vector3 overScale = item.useOvershoot
            ? Vector3.one * item.overshootScale
            : startScale;
        Vector3 endScale = baseScale;

        if (item.canvasGroup != null)
            item.canvasGroup.alpha = 0f;

        while (t < duration)
        {
            if (item.root == null)
                yield break;

            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / duration);
            float ease = Mathf.SmoothStep(0f, 1f, x);

            item.root.anchoredPosition = Vector2.Lerp(startPos, endPos, ease);

            if (item.useScale)
            {
                if (item.useOvershoot)
                {
                    if (x < 0.5f)
                    {
                        float inner = x / 0.5f;
                        float lerp = Mathf.SmoothStep(0f, 1f, inner);
                        item.root.localScale = Vector3.Lerp(startScale, overScale, lerp);
                    }
                    else
                    {
                        float inner = (x - 0.5f) / 0.5f;
                        float lerp = Mathf.SmoothStep(0f, 1f, inner);
                        item.root.localScale = Vector3.Lerp(overScale, endScale, lerp);
                    }
                }
                else
                {
                    item.root.localScale = Vector3.Lerp(startScale, endScale, ease);
                }
            }

            if (item.canvasGroup != null)
                item.canvasGroup.alpha = x;

            yield return null;
        }

        if (item.root != null)
        {
            item.root.anchoredPosition = endPos;
            if (item.useScale)
                item.root.localScale = endScale;
        }
        if (item.canvasGroup != null)
            item.canvasGroup.alpha = 1f;
    }

    // =========================
    // 버튼 Public 메서드
    // =========================

    /// <summary>홈 화면으로 나가기</summary>
    public void GoToHome()
    {
        MarkProblemSolved();

        // Director 테마: LevelSelectPanel 또는 EndingPanel로 복귀
        if (ProblemSession.CurrentTheme == ProblemTheme.Director)
        {
            ProblemSession.ReturnTarget = ProblemSession.CurrentProblemIndex >= 10
                ? HomeReturnTarget.Ending
                : HomeReturnTarget.LevelSelect;
        }
        else
        {
            ProblemSession.ReturnTarget = HomeReturnTarget.None;
        }

        Debug.Log($"[CommonRewardStep] GoToHome - Theme={ProblemSession.CurrentTheme}, Index={ProblemSession.CurrentProblemIndex}, ReturnTarget={ProblemSession.ReturnTarget}");
        GameManager.Instance.GoToHome();
    }

    /// <summary>현재 문제를 완료 처리 (DB 저장)</summary>
    private void MarkProblemSolved()
    {
        var ds = DataService.Instance;
        var user = SessionManager.Instance?.CurrentUser;

        if (ds != null && ds.Progress != null && user != null)
        {
            var theme = ProblemSession.CurrentTheme;
            var index = ProblemSession.CurrentProblemIndex;

            var res = ds.Progress.MarkProblemSolvedForCurrentUser(theme, index);
            if (!res.Ok)
                Debug.LogWarning($"[CommonRewardStep] MarkProblemSolved 실패: {res.Error}");
        }
        else
        {
            Debug.LogWarning("[CommonRewardStep] 문제 완료 저장 실패 - 세션 또는 DataService 없음");
        }
    }

    // =========================
    // 보상 DB 저장
    // =========================

    private void SaveRewardToDbOnce()
    {
        if (_rewardSaved) return;
        _rewardSaved = true;

        if (context == null)
        {
            Debug.LogWarning("[CommonRewardStep] ProblemContext가 설정되지 않아 보상 저장 스킵");
            return;
        }

        var body = new StepRewardAttemptDto
        {
            items = new[]
            {
                new StepRewardItemDto
                {
                    itemId = rewardItemId,
                    itemName = rewardItemName,
                    unlocked = true
                }
            }
        };

        SaveReward(body, rewardItemId, rewardItemName);
    }
}
