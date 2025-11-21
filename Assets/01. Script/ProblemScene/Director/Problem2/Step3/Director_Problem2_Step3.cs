using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director 테마 / Problem2 / Step3
/// - NG 문장을 보여주고,
/// - 사용자가 새로운 관점(카메라 앵글)을 하나 선택
/// - 마이크 버튼을 눌러 답변(녹음 완료했다고 가정)
/// - 카드가 OK 장면으로 뒤집히고, 요약 보기 버튼 노출
/// - 요약 보기 버튼에서 DB에 Attempt 저장 + 패널 전환
/// </summary>
public class Director_Problem2_Step3 : MonoBehaviour
{
    [Serializable]
    public class PerspectiveOption
    {
        public int id;            // 1..N (인스펙터에서 부여)
        [TextArea]
        public string text;       // 예: "상대도 긴장했을 수도 있어"
    }

    [Header("문구 설정")]
    [TextArea]
    [SerializeField] private string ngSentence = "모두 나를 이상하게 생각할 거야";
    [SerializeField] private PerspectiveOption[] perspectives;

    [Header("초기 텍스트 설정 옵션")]
    [Tooltip("true면 Reset 시 항상 ngSentence로 덮어씀, false면 외부에서 미리 넣어둔 sceneText를 그대로 사용")]
    [SerializeField] private bool overwriteSceneTextOnReset = false;

    [Header("씬 카드 UI (NG / OK)")]
    [SerializeField] private Text sceneText;                // 카드 안에 들어갈 문장 텍스트
    [SerializeField] private GameObject ngBadgeRoot;        // "NG" 배지 오브젝트
    [SerializeField] private GameObject okBadgeRoot;        // "OK" 배지 오브젝트
    [SerializeField] private RectTransform sceneCardRect;   // 카드 전체 RectTransform (코루틴 플립용)

    [Header("관점 선택지 UI")]
    [SerializeField] private GameObject perspectiveButtonsRoot; // 전체 선택지 묶음 루트
    [SerializeField] private Button[] perspectiveButtons;       // 각 버튼
    [SerializeField] private Text[] perspectiveTexts;           // 버튼 안에 들어갈 텍스트
    [SerializeField] private GameObject[] perspectiveSelectedMarks; // 체크마크 등 선택 표시

    [Header("마이크 UI")]
    [SerializeField] private GameObject micButtonRoot;          // 마이크 버튼 루트
    [SerializeField] private MicRecordingIndicator micIndicator; // 기존 Indicator 재사용

    [Header("요약 버튼 / 패널 전환")]
    [SerializeField] private GameObject summaryButtonRoot;      // "요약 보기" 버튼 루트
    [SerializeField] private GameObject stepRoot;               // 현재 Step3 패널 루트
    [SerializeField] private GameObject summaryPanelRoot;       // 요약 패널 루트

    [Header("공용 Problem Context")]
    [SerializeField] private ProblemContext context;

    [Header("연출 옵션")]
    [SerializeField] private float flipDelay = 0.3f;    // 말하기 끝 ~ 플립 시작까지 대기 시간
    [SerializeField] private float flipDuration = 0.5f; // 카드 한 번 뒤집히는 전체 시간

    // 내부 상태
    private PerspectiveOption _selected;
    private bool _isRecording;
    private bool _hasRecordedAnswer;
    private bool _isFinished; // OK 장면으로 전환 완료 여부

    private void OnEnable()
    {
        Debug.Log("[Step3] OnEnable 호출");
        ResetState();
    }

    private void ResetState()
    {
        Debug.Log("[Step3] ResetState");

        _selected = null;
        _isRecording = false;
        _hasRecordedAnswer = false;
        _isFinished = false;

        // NG 문장으로 강제 덮어쓸지 여부
        // - STT/외부 입력으로 sceneText.text를 이미 세팅했다면, 
        //   Inspector에서 overwriteSceneTextOnReset = false 로 두면 그대로 사용.
        if (sceneText != null && overwriteSceneTextOnReset)
        {
            sceneText.text = ngSentence;
        }

        if (ngBadgeRoot != null) ngBadgeRoot.SetActive(true);
        if (okBadgeRoot != null) okBadgeRoot.SetActive(false);

        if (perspectiveButtonsRoot != null)
            perspectiveButtonsRoot.SetActive(true);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(false);

        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        // 선택지 텍스트 세팅
        if (perspectiveTexts != null && perspectives != null)
        {
            int count = Mathf.Min(perspectiveTexts.Length, perspectives.Length);
            for (int i = 0; i < count; i++)
            {
                if (perspectiveTexts[i] != null)
                    perspectiveTexts[i].text = perspectives[i].text;
            }
        }

        // 버튼/체크마크 초기화
        if (perspectiveButtons != null)
        {
            foreach (var btn in perspectiveButtons)
            {
                if (btn != null) btn.interactable = true;
            }
        }

        if (perspectiveSelectedMarks != null)
        {
            foreach (var mark in perspectiveSelectedMarks)
            {
                if (mark != null) mark.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 관점 선택 버튼에서 호출
    /// Button OnClick → Director_Problem2_Step3.OnClickPerspective(인덱스)
    /// 인덱스 = 0,1,2 ... : perspectives 배열의 인덱스
    /// </summary>
    public void OnClickPerspective(int optionIndex)
    {
        Debug.Log($"[Step3] OnClickPerspective 호출: optionIndex={optionIndex}");

        if (_isFinished)
        {
            Debug.Log("[Step3] 이미 완료 상태라 클릭 무시");
            return;
        }

        if (perspectives == null || perspectives.Length == 0)
        {
            Debug.LogWarning("[Step3] perspectives 배열이 비어있음");
            return;
        }

        if (optionIndex < 0 || optionIndex >= perspectives.Length)
        {
            Debug.LogWarning($"[Step3] optionIndex 범위 밖: {optionIndex} / len={perspectives.Length}");
            return;
        }

        _selected = perspectives[optionIndex];
        Debug.Log($"[Step3] 선택된 관점: id={_selected.id}, text={_selected.text}");

        // 선택 표시
        if (perspectiveSelectedMarks != null)
        {
            for (int i = 0; i < perspectiveSelectedMarks.Length; i++)
            {
                if (perspectiveSelectedMarks[i] != null)
                    perspectiveSelectedMarks[i].SetActive(i == optionIndex);
            }
        }

        // 선택 후 다른 버튼 잠금 (1회 선택 구조)
        if (perspectiveButtons != null)
        {
            for (int i = 0; i < perspectiveButtons.Length; i++)
            {
                if (perspectiveButtons[i] != null)
                    perspectiveButtons[i].interactable = (i == optionIndex);
            }
        }

        // 마이크 버튼 노출
        if (micButtonRoot != null)
        {
            micButtonRoot.SetActive(true);
            Debug.Log("[Step3] micButtonRoot 활성화");
        }
    }

    /// <summary>
    /// 마이크 버튼 클릭
    /// - 첫 클릭: 녹음 시작 (Indicator On)
    /// - 두 번째 클릭: 녹음 종료로 보고 카드 플립 → OK 장면
    /// </summary>
    public void OnClickMic()
    {
        Debug.Log("[Step3] OnClickMic");

        if (_selected == null)
        {
            Debug.LogWarning("[Step3] 아직 선택된 관점이 없음");
            return;
        }

        if (_isFinished)
        {
            Debug.Log("[Step3] 이미 완료 상태, 마이크 무시");
            return;
        }

        _isRecording = !_isRecording;

        if (micIndicator != null)
            micIndicator.ToggleRecording();

        Debug.Log($"[Step3] _isRecording={_isRecording}");

        // false가 된 시점 = 녹음 종료라고 간주
        if (!_isRecording)
        {
            _hasRecordedAnswer = true;
            Debug.Log("[Step3] 녹음 종료 → PlayRefilmCompleteSequence 시작");
            StartCoroutine(PlayRefilmCompleteSequence());
        }
    }

    /// <summary>
    /// 카드 NG -> OK로 뒤집는 연출을 코루틴으로 처리
    /// - 1단계: 가로 스케일 1 -> 0 (카드가 말려 들어가며 사라짐)
    /// - 2단계: 가로 스케일 0 -> 1 (카드가 다시 펼쳐짐)
    /// - ★ 애니메이션이 완전히 끝난 뒤에 텍스트/배지를 선택 관점으로 교체
    /// </summary>
    private IEnumerator PlayCardFlipCoroutine()
    {
        // sceneCardRect가 없으면 단순히 텍스트와 배지만 교체
        if (sceneCardRect == null)
        {
            Debug.LogWarning("[Step3] sceneCardRect가 없어 플립 없이 텍스트만 교체");
            if (sceneText != null && _selected != null)
                sceneText.text = _selected.text;

            if (ngBadgeRoot != null) ngBadgeRoot.SetActive(false);
            if (okBadgeRoot != null) okBadgeRoot.SetActive(true);
            yield break;
        }

        float duration = Mathf.Max(0.05f, flipDuration); // React 쪽 0.5s랑 맞추고 싶으면 0.5로 세팅
        float half = duration * 0.5f;

        RectTransform rt = sceneCardRect;

        // 원래 UI 사이즈 / 위치 저장
        Vector2 originalSize = rt.sizeDelta;
        Vector2 originalPos = rt.anchoredPosition;

        // pivot은 가운데 기준이 자연스러움 (Inspector에서 0.5,0.5로 맞춰두는 거 추천)
        // rt.pivot = new Vector2(0.5f, 0.5f);

        // ==========================
        // 1단계: width -> 0 (NG 장면 그대로, 접히기만 함)
        // ==========================
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / half);

            float width = Mathf.Lerp(originalSize.x, 0f, lerp);
            rt.sizeDelta = new Vector2(width, originalSize.y);
            rt.anchoredPosition = originalPos; // 중앙 유지

            yield return null;
        }

        // 완전히 납작한 상태
        rt.sizeDelta = new Vector2(0f, originalSize.y);
        rt.anchoredPosition = originalPos;

        // ==========================
        // 2단계: 0 -> 원래 width (여전히 NG 텍스트 유지)
        // ==========================
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / half);

            float width = Mathf.Lerp(0f, originalSize.x, lerp);
            rt.sizeDelta = new Vector2(width, originalSize.y);
            rt.anchoredPosition = originalPos;

            yield return null;
        }

        // 원래 사이즈/위치로 복원
        rt.sizeDelta = originalSize;
        rt.anchoredPosition = originalPos;

        // ==========================
        // ★ 여기서부터가 React의
        //    setSceneVariant('ok'); 에 해당하는 부분
        // ==========================

        // 텍스트를 선택한 관점으로 교체
        if (sceneText != null && _selected != null)
            sceneText.text = _selected.text;

        // NG/OK 배지 전환
        if (ngBadgeRoot != null) ngBadgeRoot.SetActive(false);
        if (okBadgeRoot != null) okBadgeRoot.SetActive(true);
    }

    private IEnumerator PlayRefilmCompleteSequence()
    {
        // 마이크 버튼은 더 이상 사용하지 않으니 숨김
        if (micButtonRoot != null)
            micButtonRoot.SetActive(false);

        // 관점 선택지는 "보이긴 하되" 더 이상 클릭은 안 되게 처리
        if (perspectiveButtonsRoot != null)
            perspectiveButtonsRoot.SetActive(true); // 혹시 꺼져 있을 수 있으니 보이게 유지

        // 말하기가 끝난 뒤 잠시 대기 후 플립 시작
        if (flipDelay > 0f)
            yield return new WaitForSeconds(flipDelay);

        // 여기선 버튼의 색상 알파값을 0.3으로 만들어주는 로직
        if (perspectiveButtons != null)
        {
            foreach (var btn in perspectiveButtons)
            {
                if (btn == null) continue;

                // 더 이상 클릭 안 되게
                btn.interactable = false;

                // 1) 버튼의 메인 그래픽 알파 줄이기
                if (btn.targetGraphic != null)
                {
                    var c = btn.targetGraphic.color;
                    c.a = 0.3f;
                    btn.targetGraphic.color = c;
                }

                // 2) 버튼 안에 들어있는 Text / Image / TMP 등 자식 Graphic들도 같이 알파 줄이기
                var childGraphics = btn.GetComponentsInChildren<Graphic>(true);
                foreach (var g in childGraphics)
                {
                    var gc = g.color;
                    gc.a = 0.3f;
                    g.color = gc;
                }
            }
        }

        // 카드 플립 코루틴 실행
        yield return StartCoroutine(PlayCardFlipCoroutine());

        // 플립 완료 후 상태 마무리
        _isFinished = true;
        Debug.Log("[Step3] 플립 완료, _isFinished = true");

        if (summaryButtonRoot != null)
        {
            summaryButtonRoot.SetActive(true);
            Debug.Log("[Step3] summaryButtonRoot 활성화");
        }
    }

    /// <summary>
    /// "요약 보기" 버튼에서 호출
    /// - Attempt DB 저장
    /// - 패널 전환
    /// </summary>
    public void OnClickSummaryButton()
    {
        Debug.Log("[Step3] OnClickSummaryButton");

        SaveRefilmLogToDb();

        if (stepRoot != null)
            stepRoot.SetActive(false);

        if (summaryPanelRoot != null)
            summaryPanelRoot.SetActive(true);
    }

    /// <summary>
    /// ProblemContext를 이용해 Attempt 저장
    /// </summary>
    private void SaveRefilmLogToDb()
    {
        if (context == null)
        {
            Debug.LogWarning("[Director_Problem2_Step3] ProblemContext가 설정되지 않아 저장 스킵");
            return;
        }

        if (_selected == null)
        {
            Debug.Log("[Director_Problem2_Step3] 선택된 관점이 없어 저장 스킵");
            return;
        }

        // 이 스텝 키 설정 (Problem2 / Step3)
        context.CurrentStepKey = "Director_Problem2_Step3";

        // body에는 이 스텝에서 필요한 정보만 넣기
        var body = new
        {
            ngText = ngSentence,          // 원래 NG 문장 (참고용)
            selectedId = _selected.id,    // 선택한 관점 id
            selectedText = _selected.text,// 선택한 관점 문장
            //recorded = _hasRecordedAnswer // 마이크를 한 번이라도 종료했는지 여부
        };

        Debug.Log("[Director_Problem2_Step3] SaveStepAttempt 호출");
        context.SaveStepAttempt(body);
    }
}
