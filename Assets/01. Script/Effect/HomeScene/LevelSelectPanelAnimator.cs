using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class LevelSelectPanelAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform[] ProblemPanels;
    [Header("애니메이션 설정")]
    [SerializeField] private float startOffsetY = -120f;  // 아래에서 얼마나 올라올지
    [SerializeField] private float duration = 0.35f;      // 한 버튼이 올라오는 시간
    [SerializeField] private float interval = 0.05f;      // 버튼 사이 딜레이
    [SerializeField] private bool playOnEnable = true;    // 패널 켜질 때 자동 재생

    private Vector2[] _originalPos;
    private CanvasGroup[] _canvasGroups;
    private Coroutine _introRoutine;
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
    public void PlayIntro()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_introRoutine != null)
            StopCoroutine(_introRoutine);

        _introRoutine = StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        // 초기 상태 세팅: 아래로 내리고 투명하게
        for (int i = 0; i < ProblemPanels.Length; i++)
        {
            var rt = ProblemPanels[i];
            var cg = _canvasGroups[i];

            rt.anchoredPosition = _originalPos[i] + new Vector2(0f, startOffsetY);
            cg.alpha = 0f;
        }

        // 순차적으로 버튼 애니메이션 시작
        for (int i = 0; i < ProblemPanels.Length; i++)
        {
            StartCoroutine(AnimateSingle(ProblemPanels[i], _canvasGroups[i], _originalPos[i]));
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private IEnumerator AnimateSingle(RectTransform rt, CanvasGroup cg, Vector2 targetPos)
    {
        Vector2 startPos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 살짝 부드러운 ease-out (cubic)
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            cg.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        // 마지막 오차 보정
        rt.anchoredPosition = targetPos;
        cg.alpha = 1f;
    }
}

