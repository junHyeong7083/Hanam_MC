using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 리워드 화면용 스파클 이펙트 (DOTween)
/// - React RewardScreen의 Sparkles 효과 재현
/// - 영역 내 랜덤 위치에 스파클 생성
/// - opacity: [0, 1, 0], scale: [0, 1.5, 0], rotate: [0, 180, 360]
/// - 무한 반복
///
/// [사용처]
/// - 리워드 화면: 아이템 주변 반짝임
/// </summary>
public class RewardParticleBurst : MonoBehaviour
{
    [Header("스파클 설정")]
    [SerializeField] private int sparkleCount = 30;
    [SerializeField] private GameObject sparklePrefab;

    [Header("영역 설정")]
    [SerializeField] private RectTransform spawnArea;

    [Header("애니메이션")]
    [SerializeField] private float minDuration = 2f;
    [SerializeField] private float maxDuration = 3f;
    [SerializeField] private float maxDelay = 2f;
    [SerializeField] private float maxScale = 1.5f;

    [Header("색상")]
    [SerializeField] private Color sparkleColor = new Color(1f, 0.54f, 0.24f);  // #FF8A3D

    [Header("크기")]
    [SerializeField] private float minSize = 12f;
    [SerializeField] private float maxSize = 20f;

    [Header("자동 시작")]
    [SerializeField] private bool playOnEnable = true;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = spawnArea != null ? spawnArea : GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    /// <summary>
    /// 스파클 생성 시작
    /// </summary>
    public void Play()
    {
        Stop();

        for (int i = 0; i < sparkleCount; i++)
        {
            CreateSparkle();
        }
    }

    /// <summary>
    /// 모든 스파클 제거
    /// </summary>
    public void Stop()
    {
        // spawnArea가 있으면 거기서, 없으면 자신에서 정리
        Transform parent = spawnArea != null ? spawnArea : transform;

        DOTween.Kill(parent);

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            // Sparkle로 시작하는 이름만 삭제 (다른 자식 보호)
            if (child.name.StartsWith("Sparkle"))
            {
                DOTween.Kill(child);
                Destroy(child.gameObject);
            }
        }
    }

    private void CreateSparkle()
    {
        // spawnArea가 있으면 그 아래에, 없으면 자신 아래에 생성
        Transform parent = spawnArea != null ? spawnArea : transform;

        GameObject sparkle = sparklePrefab != null
            ? Instantiate(sparklePrefab, parent)
            : CreateDefaultSparkle(parent);

        RectTransform rt = sparkle.GetComponent<RectTransform>();
        if (rt == null) return;

        // anchor를 중앙으로 설정 (프리팹 사용 시에도)
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        CanvasGroup cg = sparkle.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = sparkle.AddComponent<CanvasGroup>();

        // spawnArea Rect 내부에 랜덤 배치
        float halfWidth = _rectTransform.rect.width / 2f;
        float halfHeight = _rectTransform.rect.height / 2f;

        float x = Random.Range(-halfWidth, halfWidth);
        float y = Random.Range(-halfHeight, halfHeight);
        rt.anchoredPosition = new Vector2(x, y);

        // 랜덤 파라미터
        float duration = Random.Range(minDuration, maxDuration);
        float delay = Random.Range(0f, maxDelay);

        // 초기 상태
        rt.localScale = Vector3.zero;
        cg.alpha = 0f;

        // 애니메이션 시작
        AnimateSparkle(rt, cg, duration, delay);
    }

    private void AnimateSparkle(RectTransform rt, CanvasGroup cg, float duration, float initialDelay)
    {
        // React 패턴: opacity: [0, 1, 0], scale: [0, 1.5, 0], rotate: [0, 180, 360]
        Sequence seq = DOTween.Sequence();

        // 초기 딜레이
        seq.AppendInterval(initialDelay);

        // 전반부: 0 → 1 (scale, opacity), 0 → 180 (rotation)
        float halfDuration = duration / 2f;
        seq.Append(rt.DOScale(maxScale, halfDuration).SetEase(Ease.OutQuad));
        seq.Join(cg.DOFade(1f, halfDuration).SetEase(Ease.OutQuad));
        seq.Join(rt.DORotate(new Vector3(0, 0, 180f), halfDuration, RotateMode.LocalAxisAdd));

        // 후반부: 1 → 0 (scale, opacity), 180 → 360 (rotation)
        seq.Append(rt.DOScale(0f, halfDuration).SetEase(Ease.InQuad));
        seq.Join(cg.DOFade(0f, halfDuration).SetEase(Ease.InQuad));
        seq.Join(rt.DORotate(new Vector3(0, 0, 180f), halfDuration, RotateMode.LocalAxisAdd));

        // 무한 반복 + 오브젝트 파괴 시 자동 Kill
        seq.SetLoops(-1, LoopType.Restart);
        seq.SetTarget(rt);
        seq.SetLink(rt.gameObject);  // GameObject 파괴 시 자동으로 Tween Kill
    }

    private GameObject CreateDefaultSparkle(Transform parent)
    {
        GameObject go = new GameObject("Sparkle");
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = sparkleColor;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        // anchor를 중앙으로 설정해야 anchoredPosition이 부모 중심 기준으로 동작
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float size = Random.Range(minSize, maxSize);
        rt.sizeDelta = new Vector2(size, size);

        return go;
    }

    /// <summary>
    /// 스파클 수 설정
    /// </summary>
    public void SetSparkleCount(int count)
    {
        sparkleCount = count;
    }

    /// <summary>
    /// 색상 설정
    /// </summary>
    public void SetColor(Color color)
    {
        sparkleColor = color;
    }
}
