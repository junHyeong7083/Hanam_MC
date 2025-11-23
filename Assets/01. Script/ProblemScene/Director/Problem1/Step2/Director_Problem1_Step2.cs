using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem_1 / Step2
/// - 여러 개의 "필름" 카드를 관리
/// - 카드가 클릭되면 체크 처리 + 플래시
/// - 전체 체크 개수에 따라 StepCompletionGate를 통해
///   프로그레스 바 / 다음 버튼을 제어
/// </summary>
public class Director_Problem1_Step2 : ProblemStepBase
{
    [System.Serializable]
    public class FilmFragment
    {
        public int id;                     // 버튼에서 넘겨줄 id (1, 2, 3...)
        public GameObject checkMark;       // 체크됐을 때 보이는 오브젝트 (아이콘 등)
        public GameObject flashOverlay;    // 클릭 시 잠깐 켜졌다 꺼질 흰색 오버레이 (옵션)
        public Graphic dimTarget;          // 알파 조절용 (Image/Text 등)
        public Text buttonText;            // 하단 텍스트 (처음엔 숨기고, 클릭 시 보이게)
        public FilmCardWiggle wiggle;      // 살짝 회전/흔들리는 연출
    }

    [Header("필름 목록")]
    [SerializeField] private FilmFragment[] films;

    [Header("알파 세팅 (흐림/선명)")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.4f;     // 아직 안 본 카드
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 1f;    // 클릭한 카드

    [Header("완료 게이트 (프로그레스/다음 버튼)")]
    [SerializeField] private StepCompletionGate completionGate;

    // 내부 상태
    private Dictionary<int, FilmFragment> _filmMap = new Dictionary<int, FilmFragment>();
    private HashSet<int> _checkedIds = new HashSet<int>();

    // ProblemStepBase에서 호출되는 진입 훅
    protected override void OnStepEnter()
    {
        BuildFilmMap();
        ResetState();
    }

    // 필요 없으면 OnStepExit는 안 만들어도 됨
    // protected override void OnStepExit() { }

    private void BuildFilmMap()
    {
        _filmMap.Clear();

        if (films == null)
            return;

        foreach (var f in films)
        {
            if (f == null) continue;
            if (!_filmMap.ContainsKey(f.id))
                _filmMap.Add(f.id, f);
        }
    }

    private void ResetState()
    {
        _checkedIds.Clear();

        if (films != null)
        {
            foreach (var f in films)
            {
                if (f == null) continue;

                // 처음에는 체크 표시/플래시 다 꺼두기
                if (f.checkMark != null)
                    f.checkMark.SetActive(false);

                if (f.flashOverlay != null)
                    f.flashOverlay.SetActive(false);

                if (f.buttonText != null)
                    f.buttonText.gameObject.SetActive(false);

                if (f.dimTarget != null)
                {
                    var c = f.dimTarget.color;
                    c.a = dimAlpha;
                    f.dimTarget.color = c;
                }

                if (f.wiggle != null)
                    f.wiggle.SetRandomRotationImmediate();
            }
        }

        // 게이트 초기화: 총 몇 개를 채워야 완료인지
        if (completionGate != null)
        {
            int total = (films != null) ? films.Length : 0;
            completionGate.ResetGate(total);
        }
    }

    /// <summary>
    /// 각 필름 버튼에서 OnClick 이벤트로 호출해주면 됨.
    /// 버튼 인스펙터에서 id를 파라미터로 넘겨줘.
    /// </summary>
    public void OnFilmClicked(int id)
    {
        if (_filmMap == null || !_filmMap.TryGetValue(id, out var fragment))
            return;

        // 플래시는 매번 줘도 됨 (이미 체크된 카드라도)
        if (fragment.flashOverlay != null)
            StartCoroutine(FlashRoutine(fragment.flashOverlay, 0.1f));

        // 이미 체크된 필름이면 상태 변화 없음
        if (_checkedIds.Contains(id))
            return;

        // 처음 체크되는 경우만 여기로 옴
        _checkedIds.Add(id);

        if (fragment.checkMark != null)
            fragment.checkMark.SetActive(true);

        if (fragment.buttonText != null)
            fragment.buttonText.gameObject.SetActive(true);

        // 알파 선명하게
        if (fragment.dimTarget != null)
        {
            var c = fragment.dimTarget.color;
            c.a = normalAlpha;
            fragment.dimTarget.color = c;
        }

        // 전체 카드들 살짝 다시 위글
        if (films != null)
        {
            foreach (var f in films)
            {
                if (f != null && f.wiggle != null)
                    f.wiggle.SetRandomRotation();
            }
        }

        // 새 필름이 처음으로 체크될 때만 완료 게이트에 1 증가 알림
        if (completionGate != null)
            completionGate.MarkOneDone();
    }

    private IEnumerator FlashRoutine(GameObject overlay, float duration)
    {
        overlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        overlay.SetActive(false);
    }
}
