using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem1 / Step1 공통 먼지 연출 로직 베이스.
/// - ProblemStepBase 를 상속받고,
/// - 구체 Step(Director_Problem1_Step1)은 단순히 필드만 들고 프로퍼티로 매핑.
/// </summary>
public abstract class Director_Problem1_Step1_Logic : ProblemStepBase
{
    // === 자식에서 UI를 매핑해 줄 추상 프로퍼티들 ===
    protected abstract bool UseDust { get; }
    protected abstract RectTransform DustParent { get; }
    protected abstract Image DustPrefab { get; }
    protected abstract int DustCount { get; }
    protected abstract Vector2 DustDurationRange { get; }
    protected abstract Vector2 DustDelayRange { get; }

    private bool _dustSpawned;

    // StepFlowController에서 이 스텝이 활성화될 때 호출
    protected override void OnStepEnter()
    {
        if (!UseDust)
            return;

        if (!_dustSpawned)
        {
            SpawnDustParticles();
            _dustSpawned = true;
        }
        else
        {
            RestartDustParticles();
        }
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        // 필요하면 정리 로직 추가 가능
    }

    private void SpawnDustParticles()
    {
        var parent = DustParent;
        var prefab = DustPrefab;

        if (parent == null || prefab == null)
            return;

        for (int i = 0; i < DustCount; i++)
        {
            var img = Object.Instantiate(prefab, parent);

            var mover = img.gameObject.GetComponent<DustParticleUI>();
            if (mover == null)
                mover = img.gameObject.AddComponent<DustParticleUI>();

            mover.Initialize(
                duration: Random.Range(DustDurationRange.x, DustDurationRange.y),
                delay: Random.Range(DustDelayRange.x, DustDelayRange.y)
            );
        }
    }

    private void RestartDustParticles()
    {
        var parent = DustParent;
        if (parent == null)
            return;

        foreach (Transform child in parent)
        {
            var mover = child.GetComponent<DustParticleUI>();
            if (mover != null)
            {
                // 비활성 -> 활성로 OnEnable 다시 태우기
                mover.gameObject.SetActive(false);
                mover.gameObject.SetActive(true);
            }
        }
    }
}
