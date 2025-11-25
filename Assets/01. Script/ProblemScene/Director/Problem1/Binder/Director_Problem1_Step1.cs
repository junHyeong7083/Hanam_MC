using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem1 / Step1
/// - 이 클래스는 UI 바인딩 + 프로퍼티 매핑만 담당.
/// - 실제 먼지 파티클 로직은 Director_Problem1_Step1_Logic(베이스)에서 처리.
/// </summary>
public class Director_Problem1_Step1 : Director_Problem1_Step1_Logic
{
    [Header("Dust Particle (옵션)")]
    [SerializeField] private bool useDust = true;          // 이 스텝에서 먼지 효과를 쓸지
    [SerializeField] private RectTransform dustParent;     // 파티클 올라갈 UI 영역
    [SerializeField] private Image dustPrefab;             // 작은 원형 이미지 프리팹
    [SerializeField] private int dustCount = 20;
    [SerializeField] private Vector2 dustDurationRange = new Vector2(3f, 7f);
    [SerializeField] private Vector2 dustDelayRange = new Vector2(0f, 3f);

    // === 베이스 추상 프로퍼티 매핑 ===
    protected override bool UseDust => useDust;
    protected override RectTransform DustParent => dustParent;
    protected override Image DustPrefab => dustPrefab;
    protected override int DustCount => dustCount;
    protected override Vector2 DustDurationRange => dustDurationRange;
    protected override Vector2 DustDelayRange => dustDelayRange;
}
