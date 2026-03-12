using UnityEngine;

/// <summary>
/// FilmCardWiggle - 필름 카드 미세 회전(흔들림) 애니메이션 컴포넌트.
///
/// 【역할】 필름 카드가 살짝 좌우로 기울어지는 미세 회전 효과를 제공한다.
///         SetRandomRotation() 호출 시 -maxAngle~maxAngle 사이의 랜덤 각도로
///         부드럽게 회전한다. 필름 카드를 터치할 때마다 다른 각도로 회전하여
///         자연스러운 느낌을 연출한다.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝2에서 필름 조각에 부착
/// 【부모 클래스】 MonoBehaviour (독립 컴포넌트)
/// 【참조하는 곳】 Director_Problem1_Step2_Logic (SetRandomRotation/SetRandomRotationImmediate 호출)
/// 【참조되는 곳】 없음 (Update에서 자체적으로 회전 보간)
/// </summary>
public class FilmCardWiggle : MonoBehaviour
{
    private RectTransform target;                        // 자기 자신의 RectTransform
    [SerializeField] private float maxAngle = 3f;        // 최대 회전 각도 (디폴트 -3도 ~ 3도)
    [SerializeField] private float duration = 0.25f;     // 회전 애니메이션 소요 시간 (초)

    private Vector3 _baseEuler;     // Awake 시 저장한 기본 오일러 각도
    private Quaternion _fromRot;    // 현재(시작) 회전값
    private Quaternion _toRot;      // 목표 회전값
    private float _t;               // 보간 진행도 (0~1)
    private bool _animating;        // 회전 애니메이션 진행 중 여부

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
    /// 새로운 랜덤 회전 각도로 부드럽게 회전 시작. Update에서 SmoothStep 보간으로 처리.
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
    /// 즉시 랜덤 회전 적용 (애니메이션 없이). 초기 배치 시 사용.
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
