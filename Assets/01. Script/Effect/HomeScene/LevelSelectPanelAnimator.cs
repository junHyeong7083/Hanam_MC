using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// LevelSelectPanelAnimator - 레벨 선택 패널의 문제 버튼들을 순차적으로 등장시키는 애니메이터
///
/// 【역할】 문제(Problem) 패널 배열을 아래에서 위로 순차적으로 슬라이드 업 + 페이드인하여
///          등장시킨다. Cubic ease-out 이징을 수동 구현하여 부드러운 감속 효과 제공.
///          Time.unscaledDeltaTime 사용으로 타임스케일 영향 없이 동작.
/// 【사용 위치】 HomeScene의 레벨 선택 화면 (LevelSelectPanel)
/// 【트리거】 playOnEnable=true 시 OnEnable에서 자동 재생, 또는 외부에서 PlayIntro() 호출
/// 【의존성】 ProblemPanels(RectTransform 배열), CanvasGroup(없으면 자동 추가)
/// </summary>
public class LevelSelectPanelAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform[] ProblemPanels;  // 순차 등장시킬 문제 패널 배열
    [Header("애니메이션 설정")]
    [SerializeField] private float startOffsetY = -120f;  // 아래에서 얼마나 올라올지 (px)
    [SerializeField] private float duration = 0.35f;      // 각 버튼의 올라오는 시간
    [SerializeField] private float interval = 0.05f;      // 버튼 간의 딜레이
    [SerializeField] private bool playOnEnable = true;    // 패널 활성화 시 자동 재생

    private Vector2[] _originalPos;        // 각 패널의 원래 앵커 위치
    private CanvasGroup[] _canvasGroups;   // 각 패널의 CanvasGroup (페이드용)
    private Coroutine _introRoutine;       // 현재 실행 중인 인트로 코루틴

    void Awake()
    {
        _originalPos = new Vector2[ProblemPanels.Length];
        _canvasGroups = new CanvasGroup[ProblemPanels.Length];

        for (int i = 0; i < ProblemPanels.Length; i++)
        {
            var rt = ProblemPanels[i];
            _originalPos[i] = rt.anchoredPosition;

            var cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            _canvasGroups[i] = cg;
        }
    }

    void OnEnable()
    {
        if (playOnEnable)
            PlayIntro();
    }

    void OnDisable()
    {
        if (_introRoutine != null)
        {
            StopCoroutine(_introRoutine);
            _introRoutine = null;
        }
        StopAllCoroutines(); 
        if (ProblemPanels == null || _originalPos == null || _canvasGroups == null)
            return;

        for (int i = 0; i < ProblemPanels.Length; i++)
        {
            if (ProblemPanels[i] == null) continue;

            var rt = ProblemPanels[i];
            rt.anchoredPosition = _originalPos[i];

            var cg = _canvasGroups[i];
            if (cg != null)
                cg.alpha = 1f;
        }
    }
    /// <summary>
    /// 인트로 등장 애니메이션 재생 (이전 코루틴이 있으면 중단 후 새로 시작)
    /// </summary>
    public void PlayIntro()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_introRoutine != null)
            StopCoroutine(_introRoutine);

        _introRoutine = StartCoroutine(PlayIntroRoutine());
    }

    /// <summary>
    /// 인트로 코루틴: 모든 패널을 아래+투명 상태로 초기화한 후 interval 간격으로 순차 등장
    /// </summary>
    private IEnumerator PlayIntroRoutine()
    {
        // �ʱ� ���� ����: �Ʒ��� ������ �����ϰ�
        for (int i = 0; i < ProblemPanels.Length; i++)
        {
            var rt = ProblemPanels[i];
            var cg = _canvasGroups[i];

            rt.anchoredPosition = _originalPos[i] + new Vector2(0f, startOffsetY);
            cg.alpha = 0f;
        }

        // ���������� ��ư �ִϸ��̼� ����
        for (int i = 0; i < ProblemPanels.Length; i++)
        {
            StartCoroutine(AnimateSingle(ProblemPanels[i], _canvasGroups[i], _originalPos[i]));
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    /// <summary>
    /// 개별 패널 애니메이션: startPos→targetPos로 cubic ease-out 슬라이드 + 알파 0→1 페이드
    /// </summary>
    private IEnumerator AnimateSingle(RectTransform rt, CanvasGroup cg, Vector2 targetPos)
    {
        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // ��¦ �ε巯�� ease-out (cubic)
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            cg.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        // ������ ���� ����
        rt.anchoredPosition = targetPos;
        cg.alpha = 1f;
    }
}

