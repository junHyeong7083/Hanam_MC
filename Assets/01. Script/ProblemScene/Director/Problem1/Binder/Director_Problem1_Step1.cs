using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem1_Step1 - 문제1 스텝1의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 먼지 파티클 관련 설정값과 UI 참조를 바인딩한다.
///         실제 먼지 파티클 생성/관리 로직은 부모 클래스(Director_Problem1_Step1_Logic)에 있다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측. SerializeField로 값을 받아 override property로 전달.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝1 (도입부 - 먼지 파티클 연출)
/// 【부모 클래스】 Director_Problem1_Step1_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Step1 GameObject에 부착
/// 【참조되는 곳】 Director_Problem1_Step1_Logic (abstract property 구현 제공)
/// </summary>
public class Director_Problem1_Step1 : Director_Problem1_Step1_Logic
{
    [Header("Dust Particle (옵션)")]
    [SerializeField] private bool useDust = true;                                    // 이 스텝에서 먼지 효과를 사용할지 여부
    [SerializeField] private RectTransform dustParent;                               // 파티클이 올라갈 UI 영역
    [SerializeField] private Image dustPrefab;                                       // 먼지 입자 이미지 프리팹
    [SerializeField] private int dustCount = 20;                                     // 생성할 먼지 개수
    [SerializeField] private Vector2 dustDurationRange = new Vector2(3f, 7f);        // 이동 애니메이션 시간 범위
    [SerializeField] private Vector2 dustDelayRange = new Vector2(0f, 3f);           // 시작 지연 시간 범위

    // === 베이스 추상 프로퍼티 구현 (인스펙터 값을 Logic에 전달) ===
    protected override bool UseDust => useDust;
    protected override RectTransform DustParent => dustParent;
    protected override Image DustPrefab => dustPrefab;
    protected override int DustCount => dustCount;
    protected override Vector2 DustDurationRange => dustDurationRange;
    protected override Vector2 DustDelayRange => dustDelayRange;
}
