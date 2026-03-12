using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem1_Step1_Logic - 문제1 스텝1의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 스텝1 진입 시 먼지(Dust) 파티클 UI를 생성하고 관리하는 로직을 담당한다.
///         먼지 파티클은 화면에 분위기를 연출하는 시각 효과로, DustParticleUI 컴포넌트를
///         동적으로 생성하여 부모 RectTransform 아래에 배치한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측. abstract property로 UI 참조를 선언하고,
///         자식 클래스(Director_Problem1_Step1)가 SerializeField로 실제 값을 바인딩한다.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝1 (도입부)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit() 추상 메서드 구현
/// 【참조하는 곳】 Director_Problem1_Step1 (Binder 자식 클래스)
/// 【참조되는 곳】 DustParticleUI (먼지 파티클 개별 동작), ProblemStepBase (스텝 라이프사이클)
/// 【흐름】 스텝 진입 → 먼지 파티클 생성(최초 1회) 또는 재시작 → 스텝 종료
/// </summary>
public abstract class Director_Problem1_Step1_Logic : ProblemStepBase
{
    // === 자식에서 UI를 제공할 때 사용할 추상 프로퍼티들 ===

    /// <summary>먼지 파티클 사용 여부 (false면 파티클 생성 안 함)</summary>
    protected abstract bool UseDust { get; }

    /// <summary>먼지 파티클이 생성될 부모 RectTransform (UI 캔버스 내 영역)</summary>
    protected abstract RectTransform DustParent { get; }

    /// <summary>먼지 파티클 프리팹 (Image 컴포넌트를 가진 UI 오브젝트)</summary>
    protected abstract Image DustPrefab { get; }

    /// <summary>생성할 먼지 파티클 개수</summary>
    protected abstract int DustCount { get; }

    /// <summary>각 파티클의 이동 애니메이션 지속 시간 범위 (x=최소, y=최대)</summary>
    protected abstract Vector2 DustDurationRange { get; }

    /// <summary>각 파티클의 시작 지연 시간 범위 (x=최소, y=최대)</summary>
    protected abstract Vector2 DustDelayRange { get; }

    /// <summary>먼지 파티클이 이미 생성되었는지 여부 (중복 생성 방지)</summary>
    private bool _dustSpawned;

    /// <summary>
    /// 스텝 진입 시 호출. StepFlowController가 이 스텝을 활성화할 때 실행된다.
    /// 최초 진입이면 먼지 파티클을 생성하고, 재진입이면 기존 파티클을 재시작한다.
    /// </summary>
    protected override void OnStepEnter()
    {
        if (!UseDust)
            return;

        if (!_dustSpawned)
        {
            // 최초 진입: 먼지 파티클 인스턴스 생성
            SpawnDustParticles();
            _dustSpawned = true;
        }
        else
        {
            // 재진입: 기존 파티클 재시작
            RestartDustParticles();
        }
    }

    /// <summary>
    /// 스텝 퇴장 시 호출. 현재는 추가 정리 작업 없음.
    /// </summary>
    protected override void OnStepExit()
    {
        base.OnStepExit();
        // 필요하면 정리 로직 추가 가능
    }

    /// <summary>
    /// 먼지 파티클 UI 오브젝트를 DustCount만큼 생성한다.
    /// 각 파티클에 DustParticleUI 컴포넌트를 부착하고, 랜덤한 duration/delay로 초기화한다.
    /// </summary>
    private void SpawnDustParticles()
    {
        var parent = DustParent;
        var prefab = DustPrefab;

        if (parent == null || prefab == null)
            return;

        for (int i = 0; i < DustCount; i++)
        {
            // 프리팹 인스턴스화 (부모 아래에 UI 요소로 배치)
            var img = Object.Instantiate(prefab, parent);

            // DustParticleUI 컴포넌트 확인/추가
            var mover = img.gameObject.GetComponent<DustParticleUI>();
            if (mover == null)
                mover = img.gameObject.AddComponent<DustParticleUI>();

            // 랜덤 범위 내에서 파라미터 설정
            mover.Initialize(
                duration: Random.Range(DustDurationRange.x, DustDurationRange.y),
                delay: Random.Range(DustDelayRange.x, DustDelayRange.y)
            );
        }
    }

    /// <summary>
    /// 이미 생성된 먼지 파티클들을 재시작한다.
    /// SetActive(false) → SetActive(true) 토글로 OnEnable을 다시 호출시켜 애니메이션 재시작.
    /// </summary>
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
                // 비활성화 → 활성화로 OnEnable 다시 호출
                mover.gameObject.SetActive(false);
                mover.gameObject.SetActive(true);
            }
        }
    }
}
