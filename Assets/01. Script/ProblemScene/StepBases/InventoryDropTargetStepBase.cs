using System.Collections;
using UnityEngine;

/// <summary>
/// 인벤토리 아이템 사용 스텝 베이스
/// - 화면에 보이는 인벤토리 패널 없이, DB에서 아이템 보유 여부를 확인
/// - 보유 시 자동으로 활성화 연출 + Gate 완료
/// - 파생 클래스에서 필요한 UI 프로퍼티를 제공
/// </summary>
public abstract class InventoryDropTargetStepBase : ProblemStepBase
{
    [Header("필요 아이템 (DB InventoryItem.ItemId)")]
    [SerializeField] private string requiredItemId;

    #region 파생 클래스에서 제공할 프로퍼티

    /// <summary>사용할 아이템 ID</summary>
    protected string RequiredItemId => requiredItemId;

    /// <summary>활성화 연출 대상 비주얼 루트 (스케일 애니메이션)</summary>
    protected abstract RectTransform TargetVisualRoot { get; }

    /// <summary>안내 텍스트/UI 루트 (활성화 시 숨김)</summary>
    protected abstract GameObject InstructionRoot { get; }

    /// <summary>완료 게이트 (옵션, null 가능)</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    /// <summary>활성화 연출 최대 스케일 비율</summary>
    protected virtual float ActivateScale => 1.05f;

    /// <summary>활성화 연출 시간</summary>
    protected virtual float ActivateDuration => 0.6f;

    /// <summary>연출 후 Gate 완료까지 딜레이</summary>
    protected virtual float DelayBeforeComplete => 1.5f;

    /// <summary>스텝 진입 후 자동 활성화까지 딜레이</summary>
    protected virtual float AutoActivateDelay => 0.5f;

    #endregion

    private bool _activated;
    private bool _animPlaying;

    // ================================
    // ProblemStepBase
    // ================================

    protected override void OnStepEnter()
    {
        ResetBaseState();

        var gate = CompletionGate;
        if (gate != null)
            gate.ResetGate(1);

        OnStepEnterExtra();

        // DB에서 아이템 보유 확인 후 자동 활성화
        if (HasItemInDb())
        {
            StartCoroutine(AutoActivateRoutine());
        }
        else
        {
            Debug.LogWarning($"[InventoryDropTargetStepBase] 아이템 미보유: {RequiredItemId}");
        }
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
    }

    /// <summary>
    /// 파생 클래스에서 추가 초기화가 필요할 때 override
    /// </summary>
    protected virtual void OnStepEnterExtra() { }

    private void ResetBaseState()
    {
        _activated = false;
        _animPlaying = false;

        var inst = InstructionRoot;
        if (inst != null)
            inst.SetActive(true);

        var visual = TargetVisualRoot;
        if (visual != null)
            visual.localScale = Vector3.one;
    }

    // ================================
    // DB 아이템 확인
    // ================================

    private bool HasItemInDb()
    {
        if (ProblemSession.DemoMode)
            return true;

        string itemId = RequiredItemId;
        if (string.IsNullOrEmpty(itemId))
            return false;

        var ds = DataService.Instance;
        if (ds == null || ds.Reward == null)
            return false;

        var session = SessionManager.Instance;
        var user = (session != null) ? session.CurrentUser : null;
        if (user == null || string.IsNullOrEmpty(user.Email))
            return false;

        var result = ds.Reward.GetInventory(user.Email);
        if (!result.Ok || result.Value == null)
            return false;

        foreach (var item in result.Value)
        {
            if (item != null && item.ItemId == itemId)
                return true;
        }

        return false;
    }

    // ================================
    // 자동 활성화
    // ================================

    private IEnumerator AutoActivateRoutine()
    {
        if (AutoActivateDelay > 0f)
            yield return new WaitForSeconds(AutoActivateDelay);

        yield return HandleActivatedRoutine();
    }

    private IEnumerator HandleActivatedRoutine()
    {
        if (_activated || _animPlaying)
            yield break;

        _activated = true;
        _animPlaying = true;

        var inst = InstructionRoot;
        if (inst != null)
            inst.SetActive(false);

        yield return PlayActivateAnimation();

        _animPlaying = false;

        if (DelayBeforeComplete > 0f)
            yield return new WaitForSeconds(DelayBeforeComplete);

        OnActivateComplete();

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();
    }

    /// <summary>
    /// 활성화 스케일 애니메이션 (살짝 커졌다 돌아옴)
    /// </summary>
    protected virtual IEnumerator PlayActivateAnimation()
    {
        var visual = TargetVisualRoot;
        if (visual == null || ActivateDuration <= 0f)
            yield break;

        float t = 0f;

        while (t < ActivateDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / ActivateDuration);
            float s = Mathf.Sin(x * Mathf.PI);
            float scale = Mathf.Lerp(1f, ActivateScale, s);

            visual.localScale = Vector3.one * scale;
            yield return null;
        }

        visual.localScale = Vector3.one;
    }

    /// <summary>
    /// 활성화 완료 후 추가 처리 (파생 클래스에서 override)
    /// </summary>
    protected virtual void OnActivateComplete() { }
}
