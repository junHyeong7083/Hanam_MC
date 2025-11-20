using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem_1 / Step2
/// - 여러 개의 "필름" 카드를 관리
/// - 카드가 클릭되면 체크 처리 + 플래시
/// - 전체 체크 개수에 따라 프로그레스 바 / 다음 버튼 제어
/// </summary>
public class Director_Problem1_Step2 : MonoBehaviour
{
    [System.Serializable]
    public class FilmFragment
    {
        public int id;                     // 버튼에서 넘겨줄 id (1,2,3...)
        public GameObject checkMark;       // 체크됐을 때 보이는 오브젝트 (아이콘 등)
        public GameObject flashOverlay;    // 클릭 시 잠깐 켜졌다 꺼질 흰색 오버레이 (옵션)
        public Graphic dimTarget;
        public Text buttonText;
        public FilmCardWiggle wiggle;
    }

    [Header("필름 목록")]
    [SerializeField] private FilmFragment[] films;

    [Header("프로그레스 바 (Image.fillAmount 사용)")]
    [SerializeField] private Image progressFillImage;  // type = Filled 권장 (0~1)

    [Header("알파 세팅 (흐림/선명)")]
    [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.4f;     // 아직 안 본 카드
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 1f;    // 클릭한 카드

    [Header("다음 버튼 루트 (보이기/숨기기만 담당)")]
    [SerializeField] private GameObject nextButtonRoot;

    // 내부 상태
    private Dictionary<int, FilmFragment> _filmMap;
    private HashSet<int> _checkedIds = new HashSet<int>();

    private void OnEnable()
    {
        // Step2 진입할 때마다 상태 리셋
        BuildFilmMap();
        ResetState();
    }

    private void BuildFilmMap()
    {
        if (_filmMap == null)
            _filmMap = new Dictionary<int, FilmFragment>();
        else
            _filmMap.Clear();

        if (films == null) return;

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

                if (f.wiggle != null)
                    f.wiggle.SetRandomRotationImmediate();
            }
        }

        UpdateProgressAndNextButton();
    }

    /// <summary>
    /// 각 필름 버튼에서 OnClick 이벤트로 호출해주면 됨.
    /// 버튼 인스펙터에서 id를 파라미터로 넘겨줘.
    /// </summary>
    public void OnFilmClicked(int id)
    {
        if (_filmMap == null || !_filmMap.TryGetValue(id, out var fragment))
            return;

        // 이미 체크된 필름이라도 플래시는 다시 줄 수 있음
        if (fragment.flashOverlay != null)
            StartCoroutine(FlashRoutine(fragment.flashOverlay, 0.1f));

        // 체크 상태는 한 번만 추가
        if (_checkedIds.Contains(id))
        {
            // 이미 체크된 경우, 상태 변화는 없음
            return;
        }

        _checkedIds.Add(id);

        if (fragment.checkMark != null)
            fragment.checkMark.SetActive(true);

        // 알파 선명하게
        if (fragment.dimTarget != null)
        {
            var c = fragment.dimTarget.color;
            c.a = normalAlpha;
            fragment.dimTarget.color = c;
        }

        if(fragment.buttonText !=null)
            fragment.buttonText.gameObject.SetActive(true);

        if (films != null)
        {
            foreach (var f in films)
            {
                if (f != null && f.wiggle != null)
                    f.wiggle.SetRandomRotation();
            }
        }
        UpdateProgressAndNextButton();
    }

    private void UpdateProgressAndNextButton()
    {
        int total = films != null ? films.Length : 0;
        int checkedCount = _checkedIds.Count;

        float progress = (total > 0) ? (float)checkedCount / total : 0f;

        if (progressFillImage != null)
            progressFillImage.fillAmount = progress;   // 0~1

        bool allChecked = (total > 0 && checkedCount >= total);

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(allChecked);
    }

    private IEnumerator FlashRoutine(GameObject overlay, float duration)
    {
        overlay.SetActive(true);
        yield return new WaitForSeconds(duration);
        overlay.SetActive(false);
    }
}
