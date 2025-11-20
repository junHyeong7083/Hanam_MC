using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director 테마 / Problem1 / Step4 (보상 연출 + 보상 DB 기록)
/// </summary>
public class Director_Problem1_Step4 : MonoBehaviour
{
    // ===== Reward Item =====
    [Header("Reward Item")]
    [SerializeField] private RectTransform rewardItemRoot;
    [SerializeField] private CanvasGroup rewardItemCanvasGroup;
    [SerializeField] private float rewardEnterDuration = 1f;
    [SerializeField] private float rewardStartScale = 0.5f;
    [SerializeField] private float rewardStartOffsetY = -100f;
    [SerializeField] private float rewardOvershootScale = 1.1f;

    // ===== 텍스트(제목 + 서브텍스트) =====
    [Header("Text Group (제목 + 설명)")]
    [SerializeField] private RectTransform textGroupRoot;
    [SerializeField] private CanvasGroup textGroupCanvasGroup;
    [SerializeField] private float textDelay = 0.5f;
    [SerializeField] private float textEnterDuration = 0.4f;
    [SerializeField] private float textStartOffsetY = -30f;

    // ===== Next 버튼 =====
    [Header("Next Button")]
    [SerializeField] private CanvasGroup nextButtonCanvasGroup;
    [SerializeField] private float nextButtonDelay = 1.5f;
    [SerializeField] private float nextButtonFadeDuration = 0.3f;

    // ===== Part 완료 뱃지 =====
    [Header("Part Complete Badge")]
    [SerializeField] private CanvasGroup partCompleteBadgeCanvasGroup;
    [SerializeField] private float badgeDelay = 1.8f;
    [SerializeField] private float badgeFadeDuration = 0.3f;

    // ===== 보상 메타 =====
    [Header("Reward Meta")]
    [SerializeField] private string rewardItemId = "mind_lens";
    [SerializeField] private string rewardItemName = "마음 렌즈";

    // ===== 인벤토리 패널 (선택) =====
    [Header("Inventory Panel (선택)")]
    [SerializeField] private RewardInventoryPanel inventoryPanel;

    // ===== DB 메타 정보 =====
    private ProblemTheme _theme = ProblemTheme.Director;
    private int _problemIndex = 1;
    private string _problemId;
    private string _sessionId;
    private string _userEmail;

    // Step3과 동일 패턴: ProblemScene 쪽에서 한 번만 세팅
    public void ConfigureMeta(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        string sessionId,
        string userEmail)
    {
        _theme = theme;
        _problemIndex = problemIndex;
        _problemId = problemId;
        _sessionId = sessionId;
        _userEmail = userEmail;
    }

    // ===== 보상 로그용 내부 DTO =====
    [Serializable]
    private class RewardItemLog
    {
        public string itemId;
        public string itemName;
        public bool unlocked;
    }

    [Serializable]
    private class RewardLogPayload
    {
        public string stepKey;      // 예: "Director_Problem1_Step4"
        public string theme;        // "Director" / "Gardener"
        public int problemIndex;    // 1..10
        public RewardItemLog[] items;
    }

    // ===== 내부 상태 =====
    private Coroutine _sequenceRoutine;

    private bool _rewardInit;
    private Vector2 _rewardBasePos;
    private Vector3 _rewardBaseScale;

    private bool _textInit;
    private Vector2 _textBasePos;

    private bool _rewardSaved;

    private void OnEnable()
    {
        StartSequence();
        SaveRewardToDbOnce();
    }

    private void OnDisable()
    {
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = null;
        }
    }

    public void StartSequence()
    {
        if (_sequenceRoutine != null)
            StopCoroutine(_sequenceRoutine);

        InitState();
        _sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    private void InitState()
    {
        // Reward 기본값 캐시
        if (rewardItemRoot != null && !_rewardInit)
        {
            _rewardBasePos = rewardItemRoot.anchoredPosition;
            _rewardBaseScale = rewardItemRoot.localScale;
            _rewardInit = true;
        }

        // Text 기본값 캐시
        if (textGroupRoot != null && !_textInit)
        {
            _textBasePos = textGroupRoot.anchoredPosition;
            _textInit = true;
        }

        // Reward 초기 상태
        if (rewardItemRoot != null)
        {
            rewardItemRoot.anchoredPosition = _rewardBasePos + new Vector2(0f, rewardStartOffsetY);
            rewardItemRoot.localScale = new Vector3(rewardStartScale, rewardStartScale, 1f);
        }
        if (rewardItemCanvasGroup != null)
            rewardItemCanvasGroup.alpha = 0f;

        // Text 초기 상태
        if (textGroupRoot != null)
            textGroupRoot.anchoredPosition = _textBasePos + new Vector2(0f, textStartOffsetY);
        if (textGroupCanvasGroup != null)
            textGroupCanvasGroup.alpha = 0f;

        // 버튼/뱃지 초기 상태
        if (nextButtonCanvasGroup != null)
            nextButtonCanvasGroup.alpha = 0f;
        if (partCompleteBadgeCanvasGroup != null)
            partCompleteBadgeCanvasGroup.alpha = 0f;
    }

    private IEnumerator SequenceRoutine()
    {
        // 1) 보상 아이템 등장
        yield return RewardEnterRoutine();

        // 2) 텍스트 등장
        if (textGroupRoot != null && textGroupCanvasGroup != null)
            StartCoroutine(TextEnterRoutine());

        // 3) 인벤토리 연출 (선택)
        if (inventoryPanel != null)
        {
            // DB에서 인벤토리 다시 읽어와서, 이 itemId 기준으로 슬롯 갱신/효과 처리
            inventoryPanel.ShowInventory(rewardItemId, true);
        }

        // 4) 버튼/뱃지 페이드 인
        if (nextButtonCanvasGroup != null)
            StartCoroutine(NextButtonRoutine());

        if (partCompleteBadgeCanvasGroup != null)
            StartCoroutine(BadgeRoutine());
    }

    // ==== Reward Item ====
    private IEnumerator RewardEnterRoutine()
    {
        if (rewardItemRoot == null)
            yield break;

        float t = 0f;
        Vector2 startPos = rewardItemRoot.anchoredPosition;
        Vector2 endPos = _rewardBasePos;

        Vector3 startScale = new Vector3(rewardStartScale, rewardStartScale, 1f);
        Vector3 overScale = new Vector3(rewardOvershootScale, rewardOvershootScale, 1f);
        Vector3 endScale = _rewardBaseScale;

        if (rewardItemCanvasGroup != null)
            rewardItemCanvasGroup.alpha = 0f;

        while (t < rewardEnterDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / rewardEnterDuration);

            // 위치: 아래에서 위로
            float posLerp = Mathf.SmoothStep(0f, 1f, x);
            rewardItemRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, posLerp);

            // 스케일: 절반까지 overshoot, 이후 1.0으로 수렴
            if (x < 0.5f)
            {
                float inner = x / 0.5f;
                float lerp = Mathf.SmoothStep(0f, 1f, inner);
                rewardItemRoot.localScale = Vector3.Lerp(startScale, overScale, lerp);
            }
            else
            {
                float inner = (x - 0.5f) / 0.5f;
                float lerp = Mathf.SmoothStep(0f, 1f, inner);
                rewardItemRoot.localScale = Vector3.Lerp(overScale, endScale, lerp);
            }

            if (rewardItemCanvasGroup != null)
                rewardItemCanvasGroup.alpha = x;

            yield return null;
        }

        rewardItemRoot.anchoredPosition = endPos;
        rewardItemRoot.localScale = endScale;
        if (rewardItemCanvasGroup != null)
            rewardItemCanvasGroup.alpha = 1f;
    }

    // ==== Text ====
    private IEnumerator TextEnterRoutine()
    {
        yield return new WaitForSeconds(textDelay);

        if (textGroupRoot == null || textGroupCanvasGroup == null)
            yield break;

        float t = 0f;
        Vector2 startPos = textGroupRoot.anchoredPosition;
        Vector2 endPos = _textBasePos;

        while (t < textEnterDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / textEnterDuration);
            float lerp = Mathf.SmoothStep(0f, 1f, x);

            textGroupRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, lerp);
            textGroupCanvasGroup.alpha = x;

            yield return null;
        }

        textGroupRoot.anchoredPosition = endPos;
        textGroupCanvasGroup.alpha = 1f;
    }

    // ==== Next Button ====
    private IEnumerator NextButtonRoutine()
    {
        yield return new WaitForSeconds(nextButtonDelay);

        if (nextButtonCanvasGroup == null) yield break;

        float t = 0f;
        while (t < nextButtonFadeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / nextButtonFadeDuration);
            nextButtonCanvasGroup.alpha = x;
            yield return null;
        }

        nextButtonCanvasGroup.alpha = 1f;
    }

    // ==== Badge ====
    private IEnumerator BadgeRoutine()
    {
        yield return new WaitForSeconds(badgeDelay);

        if (partCompleteBadgeCanvasGroup == null) yield break;

        float t = 0f;
        while (t < badgeFadeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / badgeFadeDuration);
            partCompleteBadgeCanvasGroup.alpha = x;
            yield return null;
        }

        partCompleteBadgeCanvasGroup.alpha = 1f;
    }

    // ====== 보상 로그 + 인벤토리 저장 ======
    private void SaveRewardToDbOnce()
    {
        if (_rewardSaved) return;
        _rewardSaved = true;

        if (DataService.Instance == null || DataService.Instance.User == null)
        {
            Debug.LogWarning("[Director_Problem1_Step4] DataService.Instance.User 없음 - 보상 저장 스킵");
            return;
        }

        // 혹시 _userEmail이 비어 있으면 현재 세션 이메일로 채워주기 (중요)
        if (string.IsNullOrEmpty(_userEmail) &&
            SessionManager.Instance != null &&
            SessionManager.Instance.CurrentUser != null)
        {
            _userEmail = SessionManager.Instance.CurrentUser.Email;
        }

        if (string.IsNullOrEmpty(_userEmail))
        {
            Debug.LogWarning("[Director_Problem1_Step4] userEmail이 없어 보상 저장 스킵");
            return;
        }

        // 1) Attempt 로그
        var itemLog = new RewardItemLog
        {
            itemId = rewardItemId,
            itemName = rewardItemName,
            unlocked = true
        };

        var payload = new RewardLogPayload
        {
            stepKey = "Director_Problem1_Step4",
            theme = _theme.ToString(),
            problemIndex = _problemIndex,
            items = new[] { itemLog }
        };

        string contentJson = JsonUtility.ToJson(payload);

        var attempt = new Attempt
        {
            SessionId = _sessionId,
            UserEmail = _userEmail,
            Content = contentJson,
            ProblemId = string.IsNullOrEmpty(_problemId) ? null : _problemId,
            Theme = _theme,
            ProblemIndex = _problemIndex
        };

        DataService.Instance.User.SaveAttempt(attempt);

        // 2) 인벤토리 저장
        var invItem = new InventoryItem
        {
            UserEmail = _userEmail,
            ItemId = rewardItemId,
            ItemName = rewardItemName,
            Theme = _theme,
            AcquiredAt = DateTime.UtcNow
        };

        var invResult = DataService.Instance.User.GrantInventoryItem(_userEmail, invItem);
        if (!invResult.Ok)
        {
            Debug.LogWarning("[Director_Problem1_Step4] 인벤토리 저장 실패: " + invResult.Error);
            return;
        }

        
    }

}
