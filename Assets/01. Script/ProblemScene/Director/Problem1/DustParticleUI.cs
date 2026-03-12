using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DustParticleUI - 먼지 파티클 UI 개별 동작 컴포넌트.
///
/// 【역할】 하나의 먼지 입자 UI 요소의 움직임과 페이드 효과를 담당한다.
///         부모 영역 내 랜덤 위치에서 시작하여, 위아래로 반복 이동하면서
///         투명도가 변하는 루프 애니메이션을 수행한다.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝1에서 사용 (분위기 연출)
/// 【부모 클래스】 MonoBehaviour (독립 컴포넌트)
/// 【참조하는 곳】 Director_Problem1_Step1_Logic (SpawnDustParticles에서 생성 + Initialize 호출)
/// 【참조되는 곳】 없음 (독립적으로 루프 동작)
/// 【흐름】 Initialize() → OnEnable() → 랜덤 위치 설정 → 딜레이 후 →
///         위로 이동(페이드인) ↔ 아래로 이동(페이드아웃) 무한 반복
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DustParticleUI : MonoBehaviour
{
    private RectTransform _rect;   // 자신의 RectTransform 캐시
    private Image _image;          // 자신의 Image 캐시 (알파 제어용)

    private float _duration = 4f;  // 한 사이클(위+아래) 총 시간
    private float _delay = 0f;     // 시작 전 지연 시간

    private float _startY;         // 시작 Y 위치 (아래)
    private float _endY;           // 종료 Y 위치 (위, startY + 100px)

    private Coroutine _loopRoutine;  // 현재 루프 코루틴 참조

    /// <summary>
    /// 파티클 초기화. Director_Problem1_Step1_Logic에서 생성 직후 호출한다.
    /// </summary>
    /// <param name="duration">한 사이클 총 시간 (초)</param>
    /// <param name="delay">시작 전 대기 시간 (초)</param>
    public void Initialize(float duration, float delay)
    {
        _duration = duration;
        _delay = delay;
    }

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        SetupRandomPosition();

        _startY = _rect.anchoredPosition.y;
        _endY = _startY + 100f;

        if (_loopRoutine != null)
            StopCoroutine(_loopRoutine);

        _loopRoutine = StartCoroutine(PlayLoop());
    }

    private void OnDisable()
    {
        if (_loopRoutine != null)
        {
            StopCoroutine(_loopRoutine);
            _loopRoutine = null;
        }
    }

    /// <summary>부모 RectTransform 영역 내 랜덤 위치에 파티클을 배치한다.</summary>
    private void SetupRandomPosition()
    {
        var parentRect = _rect.parent as RectTransform;
        if (parentRect == null) return;

        float x = Random.Range(0f, parentRect.rect.width);
        float y = Random.Range(0f, parentRect.rect.height);
        _rect.anchoredPosition = new Vector2(x, y);
    }

    /// <summary>딜레이 후 위↔아래 이동 + 페이드인/아웃을 무한 반복하는 루프 코루틴.</summary>
    private IEnumerator PlayLoop()
    {
        yield return new WaitForSeconds(_delay);

        while (true)
        {
            yield return MoveWithFade(_startY, _endY, 0f, 0.6f, _duration * 0.5f);
            yield return MoveWithFade(_endY, _startY, 0.6f, 0f, _duration * 0.5f);
        }
    }

    /// <summary>Y축 이동 + 알파 페이드를 동시에 수행하는 코루틴.</summary>
    private IEnumerator MoveWithFade(float fromY, float toY, float fromA, float toA, float time)
    {
        float t = 0f;
        var color = _image.color;

        while (t < time)
        {
            t += Time.deltaTime;
            float lerp = t / time;

            float y = Mathf.Lerp(fromY, toY, lerp);
            float a = Mathf.Lerp(fromA, toA, lerp);

            var pos = _rect.anchoredPosition;
            pos.y = y;
            _rect.anchoredPosition = pos;

            color.a = a;
            _image.color = color;

            yield return null;
        }
    }
}
