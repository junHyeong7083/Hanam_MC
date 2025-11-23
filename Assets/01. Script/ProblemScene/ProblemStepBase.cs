// ProblemStepBase.cs
using UnityEngine;

public abstract class ProblemStepBase : MonoBehaviour
{
    [Header("DB 저장 사용 여부")]
    [SerializeField] private bool useDBSave = false;

    [Header("공용 Problem 컨텍스트")]
    [SerializeField] protected ProblemContext context;

    [Header("이 스텝의 고유 키 (DB용)")]
    [SerializeField] protected string stepKey;

    protected virtual void OnEnable()
    {
        OnStepEnter();
    }

    protected virtual void OnDisable()
    {
        OnStepExit();
    }

    /// <summary>
    /// 스텝이 켜질 때 호출 (각 Step에서 구현)
    /// </summary>
    protected abstract void OnStepEnter();

    /// <summary>
    /// 스텝이 꺼질 때 필요한 정리 작업 있으면 오버라이드
    /// </summary>
    protected virtual void OnStepExit() { }

    /// <summary>
    /// 이 스텝의 시도/결과를 DB에 저장하는 공통 함수
    /// </summary>
    protected void SaveAttempt(object body)
    {
        if (context == null)
        {
            Debug.LogWarning($"{name}: ProblemContext가 없어 SaveAttempt 스킵");
            return;
        }

        context.CurrentStepKey = stepKey;
        context.SaveStepAttempt(body);
    }
}
