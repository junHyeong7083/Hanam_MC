using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CommonRewardStep - 공용 보상 연출 스텝 (모든 문제의 마지막 스텝)
///
/// 【역할】 문제 풀이 완료 후 보상 아이템 획득 연출을 담당한다.
///          SequenceItem 배열로 정의된 UI 요소들을 순차적으로 등장시키고(위치/스케일/알파 애니메이션),
///          보상 아이템 정보를 DB에 저장하며, DialogueSequencer로 보상 대사를 재생한다.
///          모든 연출 완료 후 "홈으로" 버튼을 표시한다.
/// 【참조하는 곳】 StepFlowController의 마지막 stepPanels에 배치
/// 【참조되는 곳】 ProblemStepBase (SaveReward), DataService (Progress, Reward),
///                DialogueSequencer (대사 재생), ProblemSession, GameManager
/// 【흐름】 OnStepEnter() → SaveRewardToDbOnce() → ApplyRewardText() → StartSequence()
///          → SequenceItem 순차 등장 → DialogueSequencer 대사 재생 → OnEnterComplete() → 홈 버튼 표시
///          → GoToHome() 또는 SaveAndNextStep() → MarkProblemSolved() → 홈 화면 전환
/// </summary>
public class CommonRewardStep : ProblemStepBase
{
    /// <summary>
    /// 보상 연출 시퀀스의 개별 아이템 정의.
    /// 각 항목은 순서대로 등장하며, delay/duration/offset/scale 등으로 연출을 제어한다.
    /// </summary>
    [Serializable]
    public class SequenceItem
    {
        [Header("디버그/설명용 이름 (선택)")]
        public string name; // 인스펙터에서 구분하기 쉽도록 설명용 이름

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
    [SerializeField] private SequenceItem[] sequenceItems; // 순차 등장할 UI 요소들의 배열

    [Header("아이템 이름 텍스트")]
    [SerializeField] private Text itemNameText;   // 보상 아이템 이름을 표시할 Text UI
    [SerializeField] private int itemNameTextId;   // 아이템 이름의 CSV textId

    [Header("효과 설명 텍스트")]
    [SerializeField] private Text effectDescText;  // 보상 아이템 효과 설명을 표시할 Text UI
    [SerializeField] private int effectDescTextId;  // 효과 설명의 CSV textId

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 보상 대사를 재생하는 시퀀서

    [Header("버튼 (enterTextIds 완료 후 표시)")]
    [SerializeField] private Button homeButton; // 대사 완료 후 표시되는 "홈으로" 버튼

    [Header("보상 메타 (DB 저장용)")]
    [SerializeField] private string rewardItemId = "mind_lens";    // DB에 저장할 보상 아이템 ID
    [SerializeField] private string rewardItemName = "마음 렌즈";   // DB에 저장할 보상 아이템 이름

    // 내부 상태
    private Coroutine _sequenceRoutine; // 연출 코루틴 참조 (중복 방지)
    private bool _rewardSaved;          // 보상이 이미 DB에 저장되었는지 (중복 저장 방지)

    /// <summary>DB에 저장할 보상 아이템 단건 데이터</summary>
    [Serializable]
    public class StepRewardItemDto
    {
        public string itemId;
        public string itemName;
        public bool unlocked;
    }

    /// <summary>DB에 저장할 보상 시도 데이터 (아이템 배열 포함)</summary>
    [Serializable]
    public class StepRewardAttemptDto
    {
        public StepRewardItemDto[] items;
    }

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>
    /// 스텝 진입 시: DB에 보상 저장 → 텍스트 표시 → 홈 버튼 숨김 → 대사 이벤트 등록 → 연출 시작
    /// </summary>
    protected override void OnStepEnter()
    {
        SaveRewardToDbOnce();   // DB에 보상 아이템 저장 (한 번만)
        ApplyRewardText();       // 아이템 이름/효과 텍스트 표시

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

    /// <summary>DialogueSequencer의 enterTextIds 재생이 모두 완료되면 호출. 홈 버튼을 표시한다.</summary>
    private void OnEnterComplete()
    {
        if (homeButton != null)
            homeButton.gameObject.SetActive(true);
    }

    // =========================
    // 리워드 텍스트 표시
    // =========================

    /// <summary>CSV DataTable에서 아이템 이름과 효과 설명 텍스트를 가져와 UI에 표시한다.</summary>
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

    /// <summary>보상 연출 시퀀스를 시작한다. 이미 실행 중이면 중지 후 재시작.</summary>
    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        InitState();
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    /// <summary>
    /// 모든 SequenceItem의 초기 상태를 설정한다.
    /// basePos를 캐싱하고, startOffset/startScale/alpha를 적용하여 등장 전 상태로 만든다.
    /// </summary>
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

    /// <summary>
    /// SequenceItem 배열을 순서대로 재생하는 메인 코루틴.
    /// 각 아이템의 delay 대기 → 등장 애니메이션 → SweepHighlightTrigger 실행 순서로 진행.
    /// </summary>
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

    /// <summary>
    /// 개별 SequenceItem의 등장 애니메이션을 재생하는 코루틴.
    /// 위치(startOffset→basePos), 스케일(startScale→baseScale), 알파(0→1) 보간.
    /// useOvershoot 활성 시 전반부에서 overshootScale까지 확대 후 후반부에서 원래 크기로 복귀.
    /// </summary>
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

    /// <summary>DB 저장(문제 완료) 후 다음 스텝으로 이동</summary>
    public void SaveAndNextStep()
    {
        MarkProblemSolved();

        var sfc = GetComponentInParent<StepFlowController>();
        if (sfc != null)
            sfc.NextStep();
        else
            Debug.LogWarning("[CommonRewardStep] StepFlowController를 찾을 수 없음");
    }

    /// <summary>홈 화면으로 나가기</summary>
    public void GoToHome()
    {
        MarkProblemSolved();

        // Director 테마: LevelSelectPanel 또는 EndingPanel로 복귀
        if (ProblemSession.CurrentTheme == ProblemTheme.Director)
        {
            ProblemSession.ReturnTarget = HomeReturnTarget.LevelSelect;
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

    /// <summary>
    /// 보상 아이템을 DB에 저장한다 (한 번만 실행됨).
    /// ProblemStepBase.SaveReward()를 호출하여 인벤토리에 아이템을 추가한다.
    /// </summary>
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
