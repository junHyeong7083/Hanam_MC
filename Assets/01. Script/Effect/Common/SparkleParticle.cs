using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스파클 파티클 (단일 입자)
/// - 프리팹으로 사용
/// - 생성 후 자동으로 애니메이션 + 삭제
///
/// [사용처]
/// - Problem3 Step1: 책 활성화 시 스파클
/// - 성공/완료 이펙트
/// </summary>
[RequireComponent(typeof(Image))]
public class SparkleParticle : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float spreadDistance = 100f;
    [SerializeField] private bool randomDirection = true;
    [SerializeField] private Vector2 fixedDirection = Vector2.up;

    [Header("스케일")]
    [SerializeField] private float startScale = 0f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float endScale = 0f;

    [Header("색상")]
    [SerializeField] private Color sparkleColor = new Color(1f, 0.84f, 0f, 1f);  // Gold

    [Header("자동 시작")]
    [SerializeField] private bool autoStart = true;

    // 내부
    private RectTransform _rectTransform;
    private Image _image;
    private CanvasGroup _canvasGroup;
    private Vector2 _startPos;
    private Vector2 _targetOffset;
    private float _elapsed;
    private bool _isPlaying;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _image.color = sparkleColor;
    }

    private void Start()
    {
        if (autoStart)
            Play();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);

        // 위치: 중앙에서 바깥으로
        Vector2 currentPos = Vector2.Lerp(_startPos, _startPos + _targetOffset, EaseOutQuad(t));
        _rectTransform.anchoredPosition = currentPos;

        // 스케일: 0 → max → 0
        float scaleT;
        if (t < 0.3f)
        {
            // 0 ~ 0.3: 커지기
            scaleT = t / 0.3f;
            float scale = Mathf.Lerp(startScale, maxScale, scaleT);
            _rectTransform.localScale = Vector3.one * scale;
        }
        else
        {
            // 0.3 ~ 1: 작아지기
            scaleT = (t - 0.3f) / 0.7f;
            float scale = Mathf.Lerp(maxScale, endScale, scaleT);
            _rectTransform.localScale = Vector3.one * scale;
        }

        // 알파: 1 → 0
        _canvasGroup.alpha = 1f - t;

        // 완료
        if (t >= 1f)
        {
            _isPlaying = false;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 애니메이션 시작
    /// </summary>
    public void Play()
    {
        _startPos = _rectTransform.anchoredPosition;
        _elapsed = 0f;
        _isPlaying = true;

        // 방향 설정
        if (randomDirection)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(spreadDistance * 0.5f, spreadDistance);
            _targetOffset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * distance;
        }
        else
        {
            _targetOffset = fixedDirection.normalized * spreadDistance;
        }

        _rectTransform.localScale = Vector3.one * startScale;
    }

    /// <summary>
    /// 방향 설정 (외부에서 호출)
    /// </summary>
    public void SetDirection(Vector2 direction, float distance)
    {
        randomDirection = false;
        _targetOffset = direction.normalized * distance;
    }

    /// <summary>
    /// 색상 설정
    /// </summary>
    public void SetColor(Color color)
    {
        sparkleColor = color;
        if (_image != null)
            _image.color = color;
    }

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
