using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem_1 / Step1 전용 로직.
/// - Step1 패널이 활성화될 때(OnEnable) 먼지 파티클을 생성/재시작한다.
/// - 버튼 클릭, Step 전환, 텍스트/레이아웃은 전부 다른 스크립트/인스펙터에서 처리.
/// </summary>
public class Director_Problem1_Step1 : MonoBehaviour
{
    [Header("Dust Particle (옵션)")]
    [SerializeField] private bool useDust = true;          // 이 스텝에서 먼지 효과를 쓸지
    [SerializeField] private RectTransform dustParent;     // 파티클 올라갈 UI 영역
    [SerializeField] private Image dustPrefab;             // 작은 원형 이미지 프리팹
    [SerializeField] private int dustCount = 20;
    [SerializeField] private Vector2 dustDurationRange = new Vector2(3f, 7f);
    [SerializeField] private Vector2 dustDelayRange = new Vector2(0f, 3f);

    private bool _dustSpawned = false;

    private void OnEnable()
    {
        // 이 Step 패널이 켜질 때마다 호출됨 (StepFlowController에서 SetActive(true) 할 때)
        if (!useDust) return;

        if (!_dustSpawned)
        {
            SpawnDustParticles();
            _dustSpawned = true;
        }
        else
        {
            RestartDustParticles();
        }

        // TODO: 이 스텝에서만 필요한 다른 로직 있으면 여기 추가
        // ex) 로그 찍기, ProblemSceneController에 상태 알리기 등
    }

    private void OnDisable()
    {
        // 굳이 파괴할 필요 없으면 비워둬도 됨
        // 필요하면 여기서 상태 초기화 가능
    }

    private void SpawnDustParticles()
    {
        if (dustParent == null || dustPrefab == null) return;

        for (int i = 0; i < dustCount; i++)
        {
            var img = Instantiate(dustPrefab, dustParent);
            var mover = img.gameObject.AddComponent<DustParticleUI>();

            mover.Initialize(
                duration: Random.Range(dustDurationRange.x, dustDurationRange.y),
                delay: Random.Range(dustDelayRange.x, dustDelayRange.y)
            );
        }
    }

    private void RestartDustParticles()
    {
        if (dustParent == null) return;

        foreach (Transform child in dustParent)
        {
            var mover = child.GetComponent<DustParticleUI>();
            if (mover != null)
            {
                // 비활성→활성로 OnEnable 다시 태우기
                mover.gameObject.SetActive(false);
                mover.gameObject.SetActive(true);
            }
        }
    }
}
