using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem1 / Step2 공통 로직 베이스.
/// - 필름 카드 클릭 / 체크 / 플래시 / 알파 / 게이트 처리를 모두 담당.
/// - 실제 Step 스크립트(Director_Problem1_Step2)는
///   필드만 가지고 있고, 여기 추상 프로퍼티에 매핑만 해준다.
/// </summary>
public abstract class Director_Problem1_Step2_Logic : ProblemStepBase
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

    // === 자식에서 매핑해 줄 추상 프로퍼티들 ===
    protected abstract FilmFragment[] Films { get; }
    protected abstract float DimAlpha { get; }
    protected abstract float NormalAlpha { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    // 내부 상태
    private readonly Dictionary<int, FilmFragment> _filmMap = new Dictionary<int, FilmFragment>();
    private readonly HashSet<int> _checkedIds = new HashSet<int>();

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        BuildFilmMap();
        ResetState();
    }

    protected override void OnStepExit()
    {
        // 필요 시 정리
        _checkedIds.Clear();
        _filmMap.Clear();
    }

    // =========================
    // 초기화 관련
    // =========================

    private void BuildFilmMap()
    {
        _filmMap.Clear();

        var films = Films;
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

        var films = Films;
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
                    c.a = DimAlpha;
                    f.dimTarget.color = c;
                }

                if (f.wiggle != null)
                    f.wiggle.SetRandomRotationImmediate();
            }
        }

        // 게이트 초기화: 총 몇 개를 채워야 완료인지
        var gate = CompletionGate;
        if (gate != null)
        {
            int total = (films != null) ? films.Length : 0;
            gate.ResetGate(total);
        }
    }

    // =========================
    // 버튼 클릭 처리
    // =========================

    /// <summary>
    /// UI Button OnClick에서 id를 넘겨 호출.
    /// ex) OnClick -> OnFilmClicked(1)
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
            c.a = NormalAlpha;
            fragment.dimTarget.color = c;
        }

        // 전체 카드들 살짝 다시 위글
        var films = Films;
        if (films != null)
        {
            foreach (var f in films)
            {
                if (f != null && f.wiggle != null)
                    f.wiggle.SetRandomRotation();
            }
        }

        // 새 필름이 처음으로 체크될 때만 완료 게이트에 1 증가 알림
        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();
    }

    private IEnumerator FlashRoutine(GameObject overlay, float duration)
    {
        overlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        overlay.SetActive(false);
    }
}
