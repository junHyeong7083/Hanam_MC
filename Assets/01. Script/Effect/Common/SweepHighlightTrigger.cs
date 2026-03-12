using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SweepHighlightTrigger - UI Image에 대각선 반짝임(sweep) 효과를 1회 재생하는 트리거 컴포넌트
///
/// 【역할】 UI/SweepHighlight 커스텀 셰이더의 _SweepT 파라미터를 코루틴으로 0.25→1.0으로
///          변화시켜 대각선 빛이 지나가는 효과를 1회 재생한다.
///          머티리얼 인스턴스를 생성하여 다른 오브젝트와 공유 방지.
///          enableSweep=false로 설정하면 PlaySweep() 호출을 무시 (같은 프리팹 재사용 시 편의).
/// 【사용 위치】 보상 화면, 완료 카드 등 하이라이트가 필요한 UI Image
/// 【트리거】 외부에서 PlaySweep() 호출 (보상 지급 시 등)
/// 【의존성】 Image(같은 GameObject), UI/SweepHighlight 셰이더 Material 할당 필요
///
/// 【사용법】
///   1) UI/SweepHighlight 셰이더로 만든 Material을 Image에 할당
///   2) 이 컴포넌트를 같은 GameObject에 추가
///   3) 보상 지급 시 PlaySweep() 호출
/// </summary>
public class SweepHighlightTrigger : MonoBehaviour
{
    [Header("===== Sweep 설정 =====")]
    [Tooltip("false 이면 PlaySweep() 이 아무 동작도 하지 않음\n(같은 머티리얼을 재사용하는 다른 화면에서 효과 비활성화 시 사용)")]
    [SerializeField] private bool enableSweep = true;

    [Tooltip("스윕 한 번 재생에 걸리는 시간 (초)")]
    [SerializeField] private float sweepDuration = 0.45f;

    [Tooltip("PlaySweep 호출 후 실제 재생까지의 딜레이 (초)")]
    [SerializeField] private float sweepDelay = 0f;

    [Header("===== 색상 / 방향 (머티리얼 기본값 재정의) =====")]
    [Tooltip("스윕 빛 색상 (흰색 = 머티리얼 기본값 그대로)")]
    [SerializeField] private Color  sweepColor = Color.white;

    [Tooltip("스윕 각도 (deg). 45 = 좌하→우상 대각선")]
    [SerializeField] private float  sweepAngle = 45f;

    // ─────────────────────────────────────────────────────────────────
    private static readonly int PropSweepT     = Shader.PropertyToID("_SweepT");
    private static readonly int PropSweepColor = Shader.PropertyToID("_SweepColor");
    private static readonly int PropSweepAngle = Shader.PropertyToID("_SweepAngle");

    private Image    _image;
    private Material _originalMat;   // OnDisable 시 복원할 원본 머티리얼
    private Material _matInstance;   // 공유 머티리얼 오염 방지용 복사본
    private Coroutine _routine;

    // ── 초기화 ────────────────────────────────────────────────────────

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        if (_image == null)
        {
            Debug.LogWarning($"[SweepHighlightTrigger] Image 컴포넌트를 찾지 못했습니다: {name}");
            return;
        }

        if (_image.material == null)
        {
            Debug.LogWarning($"[SweepHighlightTrigger] Material 이 할당되지 않았습니다: {name}");
            return;
        }

        // 공유 머티리얼을 건드리지 않도록 복사본 생성
        _originalMat = _image.material;
        _matInstance = Instantiate(_originalMat);
        _image.material = _matInstance;
        _image.SetMaterialDirty();

        // 인스펙터 값으로 색상 / 방향 재정의
        _matInstance.SetColor(PropSweepColor, sweepColor);
        _matInstance.SetFloat(PropSweepAngle, sweepAngle);

        // 초기 상태: 효과 없음
        _matInstance.SetFloat(PropSweepT, 0f);
    }

    private void OnDisable()
    {
        // 원본 머티리얼 복원 후 인스턴스 파괴
        if (_image != null && _originalMat != null)
            _image.material = _originalMat;

        if (_matInstance != null)
            Destroy(_matInstance);

        _matInstance = null;
    }

    // ── 공개 API ──────────────────────────────────────────────────────

    /// <summary>
    /// 스윕 효과를 1회 재생합니다.
    /// enableSweep = false 이면 아무 동작도 하지 않습니다.
    /// </summary>
    public void PlaySweep()
    {
        if (!enableSweep) return;
        if (_matInstance == null) return;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(SweepRoutine());
    }

    /// <summary>
    /// 효과를 즉시 중단하고 원래 상태로 돌립니다.
    /// </summary>
    public void StopSweep()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (_matInstance != null)
            _matInstance.SetFloat(PropSweepT, 0f);
    }

    // ── 코루틴 ────────────────────────────────────────────────────────

    private IEnumerator SweepRoutine()
    {
        // 딜레이 (있으면)
        if (sweepDelay > 0f)
            yield return new WaitForSeconds(sweepDelay);

        // SweepT: 0.3 → 1 (sweepDuration 동안, ease-in-out)
        float elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / sweepDuration);
            float eased  = Mathf.SmoothStep(0f, 1f, t);
            float sweepT = Mathf.Lerp(0.25f, 1f, eased);
            _matInstance.SetFloat(PropSweepT, sweepT);
            if (_image != null) _image.SetMaterialDirty();
            yield return null;
        }

        // 재생 완료 → 원상복구
        _matInstance.SetFloat(PropSweepT, 0f);
        if (_image != null) _image.SetMaterialDirty();
        _routine = null;
    }
}
