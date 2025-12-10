using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 모든 EffectController의 베이스 클래스
/// - DOTween Sequence 관리
/// - 공통 상태 및 유틸리티 메서드 제공
/// </summary>
public abstract class EffectControllerBase : MonoBehaviour
{
    // 현재 실행 중인 시퀀스
    protected Sequence _currentSequence;

    /// <summary>
    /// 애니메이션 진행 중 여부
    /// </summary>
    public bool IsAnimating => _currentSequence != null && _currentSequence.IsActive() && _currentSequence.IsPlaying();

    /// <summary>
    /// 현재 시퀀스 강제 종료
    /// </summary>
    protected void KillCurrentSequence()
    {
        if (_currentSequence != null)
        {
            _currentSequence.Kill();
            _currentSequence = null;
        }
    }

    /// <summary>
    /// 새 시퀀스 시작 (기존 시퀀스 자동 Kill)
    /// </summary>
    protected Sequence CreateSequence()
    {
        KillCurrentSequence();
        _currentSequence = DOTween.Sequence();
        return _currentSequence;
    }

    /// <summary>
    /// 오브젝트 비활성화 시 시퀀스 정리
    /// </summary>
    protected virtual void OnDisable()
    {
        KillCurrentSequence();
    }

    /// <summary>
    /// 오브젝트 파괴 시 시퀀스 정리
    /// </summary>
    protected virtual void OnDestroy()
    {
        KillCurrentSequence();
    }

    #region 공통 유틸리티

    /// <summary>
    /// CanvasGroup 페이드
    /// </summary>
    protected Tween DOFade(CanvasGroup cg, float endValue, float duration)
    {
        if (cg == null) return null;
        return cg.DOFade(endValue, duration);
    }

    /// <summary>
    /// RectTransform 앵커 위치 이동
    /// </summary>
    protected Tween DOAnchorPos(RectTransform rt, Vector2 endValue, float duration)
    {
        if (rt == null) return null;
        return rt.DOAnchorPos(endValue, duration);
    }

    /// <summary>
    /// Transform 스케일
    /// </summary>
    protected Tween DOScale(Transform t, float endValue, float duration)
    {
        if (t == null) return null;
        return t.DOScale(endValue, duration);
    }

    /// <summary>
    /// Transform 스케일 (Vector3)
    /// </summary>
    protected Tween DOScale(Transform t, Vector3 endValue, float duration)
    {
        if (t == null) return null;
        return t.DOScale(endValue, duration);
    }

    /// <summary>
    /// GameObject 활성화/비활성화 콜백
    /// </summary>
    protected TweenCallback SetActiveCallback(GameObject go, bool active)
    {
        return () => { if (go != null) go.SetActive(active); };
    }

    /// <summary>
    /// CanvasGroup 가져오기 (없으면 추가)
    /// </summary>
    protected CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    #endregion
}
