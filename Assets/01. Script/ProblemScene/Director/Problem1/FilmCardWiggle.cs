using UnityEngine;

/// <summary>
/// 필름 카드가 살짝 기울어지도록 회전 애니메이션을 주는 스크립트.
/// - SetRandomRotation()을 호출할 때마다 -maxAngle~maxAngle 사이로 부드럽게 회전.
/// </summary>
public class FilmCardWiggle : MonoBehaviour
{
    private RectTransform target;   // 비우면 자기 RectTransform
    [SerializeField] private float maxAngle = 3f;    // 리액트의 -3 ~ 3도 느낌
    [SerializeField] private float duration = 0.25f; // 회전 애니메이션 시간

    private Vector3 _baseEuler;
    private Quaternion _fromRot;
    private Quaternion _toRot;
    private float _t;
    private bool _animating;

    private void Awake()
    {
       target = transform as RectTransform;
       _baseEuler = target.localEulerAngles;
    }

    private void Update()
    {
        if (!_animating || target == null) return;

        _t += Time.deltaTime / duration;
        float lerp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t));
        target.localRotation = Quaternion.Slerp(_fromRot, _toRot, lerp);

        if (_t >= 1f)
            _animating = false;
    }

    /// <summary>
    /// 새로운 랜덤 회전 각도로 부드럽게 회전.
    /// </summary>
    public void SetRandomRotation()
    {
        if (target == null) return;

        float angle = Random.Range(-maxAngle, maxAngle);
        _fromRot = target.localRotation;
        _toRot = Quaternion.Euler(0f, 0f, _baseEuler.z + angle);

        _t = 0f;
        _animating = true;
    }

    /// <summary>
    /// 처음 설정용: 애니메이션 없이 바로 랜덤 각도로 세팅하고 싶을 때.
    /// </summary>
    public void SetRandomRotationImmediate()
    {
        if (target == null) return;

        float angle = Random.Range(-maxAngle, maxAngle);
        _toRot = Quaternion.Euler(0f, 0f, _baseEuler.z + angle);
        target.localRotation = _toRot;
        _animating = false;
    }
}
