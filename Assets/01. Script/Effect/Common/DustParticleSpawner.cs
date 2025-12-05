using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 먼지 파티클 스포너
/// - 화면 아래에서 위로 올라가는 먼지 효과
/// - 분위기 연출용
///
/// [사용처]
/// - Problem3 Step1: 배경 먼지 효과
/// - 스튜디오/빈티지 분위기 연출
/// </summary>
public class DustParticleSpawner : MonoBehaviour
{
    [Header("===== 파티클 설정 =====")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private int maxParticles = 8;
    [SerializeField] private float spawnInterval = 0.5f;

    [Header("영역")]
    [SerializeField] private RectTransform spawnArea;  // 없으면 부모 RectTransform 사용

    [Header("파티클 속성")]
    [SerializeField] private float minSize = 2f;
    [SerializeField] private float maxSize = 6f;
    [SerializeField] private float minSpeed = 30f;
    [SerializeField] private float maxSpeed = 60f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float horizontalDrift = 30f;

    [Header("색상")]
    [SerializeField] private Color dustColor = new Color(1f, 1f, 1f, 0.3f);

    // 내부
    private RectTransform _rectTransform;
    private int _currentCount;
    private bool _isSpawning = true;

    private void Awake()
    {
        _rectTransform = spawnArea != null ? spawnArea : GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 초기 파티클 생성
        for (int i = 0; i < maxParticles / 2; i++)
        {
            SpawnParticle(true);
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (_isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (_currentCount < maxParticles)
            {
                SpawnParticle(false);
            }
        }
    }

    private void SpawnParticle(bool randomY)
    {
        if (dustPrefab == null)
        {
            // 프리팹 없으면 기본 이미지 생성
            CreateDefaultDust(randomY);
        }
        else
        {
            GameObject dust = Instantiate(dustPrefab, transform);
            SetupParticle(dust, randomY);
        }

        _currentCount++;
    }

    private void CreateDefaultDust(bool randomY)
    {
        GameObject dust = new GameObject("DustParticle");
        dust.transform.SetParent(transform, false);

        Image img = dust.AddComponent<Image>();
        img.color = dustColor;

        // 원형으로
        // 스프라이트 없으면 기본 흰색 사용
        img.raycastTarget = false;

        RectTransform rt = dust.GetComponent<RectTransform>();
        float size = Random.Range(minSize, maxSize);
        rt.sizeDelta = new Vector2(size, size);

        SetupParticle(dust, randomY);
    }

    private void SetupParticle(GameObject dust, bool randomY)
    {
        RectTransform rt = dust.GetComponent<RectTransform>();
        if (rt == null) return;

        // 시작 위치
        float areaWidth = _rectTransform.rect.width;
        float areaHeight = _rectTransform.rect.height;

        float startX = Random.Range(-areaWidth / 2f, areaWidth / 2f);
        float startY = randomY
            ? Random.Range(-areaHeight / 2f, areaHeight / 2f)
            : -areaHeight / 2f - 20f;

        rt.anchoredPosition = new Vector2(startX, startY);

        // 애니메이션 시작
        StartCoroutine(AnimateDust(dust, rt));
    }

    private IEnumerator AnimateDust(GameObject dust, RectTransform rt)
    {
        float speed = Random.Range(minSpeed, maxSpeed);
        float drift = Random.Range(-horizontalDrift, horizontalDrift);
        float elapsed = 0f;

        CanvasGroup cg = dust.GetComponent<CanvasGroup>();
        if (cg == null) cg = dust.AddComponent<CanvasGroup>();

        float areaHeight = _rectTransform.rect.height;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < lifetime && dust != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            // 위로 이동 + 좌우 드리프트
            float y = startPos.y + speed * elapsed;
            float x = startPos.x + Mathf.Sin(elapsed * 2f) * drift * 0.1f;
            rt.anchoredPosition = new Vector2(x, y);

            // 페이드 인/아웃
            if (t < 0.2f)
            {
                cg.alpha = t / 0.2f * dustColor.a;
            }
            else if (t > 0.7f)
            {
                cg.alpha = (1f - t) / 0.3f * dustColor.a;
            }
            else
            {
                cg.alpha = dustColor.a;
            }

            // 화면 위로 나가면 종료
            if (y > areaHeight / 2f + 50f)
                break;

            yield return null;
        }

        _currentCount--;
        if (dust != null)
            Destroy(dust);
    }

    /// <summary>
    /// 스포닝 시작/중지
    /// </summary>
    public void SetSpawning(bool spawning)
    {
        _isSpawning = spawning;

        if (spawning)
            StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        _isSpawning = false;
    }
}
