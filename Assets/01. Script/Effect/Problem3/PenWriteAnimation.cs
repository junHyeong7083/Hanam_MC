using UnityEngine;

/// <summary>
/// Problem3 Step2: Pen write animation
/// - 대기 상태: originPos에서 알파 0.5~1 펄스
/// - Play() 호출 시: startPoint -> endPoint로 이동
/// - 완료 후 originPos로 복귀
/// </summary>
public class PenWriteAnimation : MonoBehaviour
{
    [Header("===== 이동 경로 (RectTransform) =====")]
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform endPoint;

    [Header("===== 이동 애니메이션 =====")]
    [SerializeField] private float moveDuration = 0.8f;

    [Header("===== 회전 =====")]
    [SerializeField] private float startRotation = -10f;
    [SerializeField] private float endRotation = 0f;

    [Header("===== 대기 상태 펄스 =====")]
    [SerializeField] private float idleAlphaMin = 0.5f;
    [SerializeField] private float idleAlphaMax = 1f;
    [SerializeField] private float idlePulseSpeed = 2f;

    [Header("===== 이동 중 페이드 =====")]
    [SerializeField] private bool enableMoveFade = true;
    [SerializeField] private float fadeInRatio = 0.2f;
    [SerializeField] private float fadeOutStart = 0.8f;

    // 상태
    private enum State { Idle, Playing }
    private State _state = State.Idle;

    // 내부
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector3 _originPos;  // 펜 아이콘의 원래 위치 (대기 시 위치)
    private float _elapsed;
    private float _idleTime;
    private bool _initialized;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // 최초 Enable 시 현재 위치를 originPos로 저장
        if (!_initialized && _rectTransform != null)
        {
            _originPos = _rectTransform.position;
            _initialized = true;
        }

        // 대기 상태로 시작 (originPos에서)
        _state = State.Idle;
        _idleTime = 0f;

        if (_rectTransform != null)
        {
            _rectTransform.position = _originPos;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, startRotation);
        }
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Idle:
                UpdateIdle();
                break;

            case State.Playing:
                UpdatePlaying();
                break;
        }
    }

    #region Idle State (originPos에서 알파 펄스)

    private void UpdateIdle()
    {
        _idleTime += Time.deltaTime;

        // 사인함수로 알파 펄스 (0.5 ~ 1)
        float sin = Mathf.Sin(_idleTime * idlePulseSpeed * Mathf.PI);
        float alpha = Mathf.Lerp(idleAlphaMin, idleAlphaMax, (sin + 1f) * 0.5f);

        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    #endregion

    #region Playing State (startPoint -> endPoint 이동)

    private void UpdatePlaying()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / moveDuration);

        // 위치: startPoint -> endPoint로 이동
        if (startPoint != null && endPoint != null && _rectTransform != null)
        {
            Vector3 startPos = startPoint.position;
            Vector3 endPos = endPoint.position;
            _rectTransform.position = Vector3.Lerp(startPos, endPos, EaseOutQuad(t));
        }

        // 회전
        if (_rectTransform != null)
        {
            float rot = Mathf.Lerp(startRotation, endRotation, t);
            _rectTransform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // 알파: 페이드인 -> 유지 -> 페이드아웃
        if (enableMoveFade && _canvasGroup != null)
        {
            float alpha;
            if (t < fadeInRatio)
            {
                alpha = t / fadeInRatio;
            }
            else if (t < fadeOutStart)
            {
                alpha = 1f;
            }
            else
            {
                alpha = 1f - ((t - fadeOutStart) / (1f - fadeOutStart));
            }
            _canvasGroup.alpha = alpha;
        }

        // 완료 -> originPos로 복귀, 대기 상태로
        if (t >= 1f)
        {
            _state = State.Idle;
            _idleTime = 0f;

            // originPos로 복귀
            if (_rectTransform != null)
            {
                _rectTransform.position = _originPos;
                _rectTransform.localRotation = Quaternion.Euler(0, 0, startRotation);
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 이동 애니메이션 시작 (startPoint -> endPoint)
    /// </summary>
    public void Play()
    {
        _state = State.Playing;
        _elapsed = 0f;

        // startPoint 위치로 이동
        if (startPoint != null && _rectTransform != null)
        {
            _rectTransform.position = startPoint.position;
        }

        // 초기 회전
        if (_rectTransform != null)
        {
            _rectTransform.localRotation = Quaternion.Euler(0, 0, startRotation);
        }

        // 초기 알파
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 즉시 정지, originPos로 복귀
    /// </summary>
    public void Stop()
    {
        _state = State.Idle;
        _idleTime = 0f;

        if (_rectTransform != null)
        {
            _rectTransform.position = _originPos;
        }
    }

    /// <summary>
    /// 대기 상태로 리셋
    /// </summary>
    public void ResetToIdle()
    {
        _state = State.Idle;
        _idleTime = 0f;

        if (_rectTransform != null)
        {
            _rectTransform.position = _originPos;
            _rectTransform.localRotation = Quaternion.Euler(0, 0, startRotation);
        }
    }

    #endregion

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
