using DA_Assets.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem4_Step2 : ProblemStepBase
{
    [Serializable]
    public class FilmCutData
    {
        [Tooltip("컷 ID")]
        public string cutID;

        [TextArea]
        [Tooltip("화면에 표시할 컷 문장")]
        public string text;

        [Tooltip("생각 컷이면 true, 사실이면 false")]
        public bool isThinking;
    }

    private enum CutStatus
    {
        ACTIVE,   // 아직 처리 안됨
        CUTTING,  // 잘라내는 중(연출용)
        PASSED,   // 통과된 사실 컷
        DELETED   // 잘라낸 생각 컷
    }

    [Serializable]
    private class CutAttemptLog
    {
        public string cutID;
        public string text;
        public bool isThinking;
        public string finalStatus; // "active" / "deleted" / "passed" / "cutting"
    }

    /// <summary>
    /// 사용자가 버튼을 누를 때마다 쌓이는 로그
    /// </summary>
    [Serializable]
    private class CutActionLog
    {
        public string cutID;     // 어떤 컷에 대한 선택인지
        public string action;    // "cut" 또는 "pass"
        public bool wasCorrect;  // 이 선택이 정답이었는지
    }

    [Serializable]
    private class AttemptBody
    {
        // 최종 결과 요약
        public CutAttemptLog[] cuts;

        // 시도 로그 (틀린 선택 포함)
        public CutActionLog[] actions;
    }

    [Header("컷 데이터")]
    [SerializeField] private FilmCutData[] filmCuts;

    [Header("필름 카드 UI")]
    [SerializeField] private GameObject filmCardRoot;
    [SerializeField] private Text filmSentenceLabel;
    [SerializeField] private Text filmIndexLabel;

    [Header("오류 메세지 UI")]
    [SerializeField] private GameObject errorRoot;
    [SerializeField] private Text errorLabel;
    [SerializeField] private string defaultErrorMessage = "다시 생각해보세요!";

    [Header("컬러 복원 연출용 UI")]
    [SerializeField] private GameObject colorRestoreRoot;
    [SerializeField] private GameObject beforeColorRoot;

    [Header("하단 버튼")]
    [SerializeField] private Button cutBtn;
    [SerializeField] private Button passBtn;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    [Header("오류 메시지 유지 시간")]
    [SerializeField] private float errorShowDuration = 1f;

    // ===== 카드 등장/위치 관련 =====
    [Header("카드 위치/등장 연출")]
    [SerializeField] private RectTransform filmCardRect;      // 카드 전체 Rect
    [SerializeField] private CanvasGroup filmCardCanvasGroup; // 카드 알파 제어용
    [SerializeField] private RectTransform filmAppearStart;   // 새 카드 출발 위치
    [SerializeField] private float appearDuration = 0.4f;

    // ===== 통과(PASS) 연출 관련 =====
    [Header("PASS 연출 (통과 카드 이동 위치)")]
    [SerializeField] private RectTransform passTargetRect;    // 통과 시 이동할 타겟 위치 (빈 오브젝트)
    [SerializeField] private float passMoveDuration = 0.5f;

    // ===== 컷(CUT) 연출 관련 =====
    [Header("가위 연출")]
    [SerializeField] private RectTransform scissorsRect;
    [SerializeField] private float scissorsMoveDuration = 0.4f;
    [SerializeField] private Vector2 scissorsOffsetFromCard = new Vector2(0f, 150f);

    [Header("분할 카드 연출")]
    [SerializeField] private RectTransform cardLeftRect;      // 왼쪽 반쪽
    [SerializeField] private RectTransform cardRightRect;     // 오른쪽 반쪽
    [SerializeField] private CanvasGroup cardLeftCanvas;      // 왼쪽 알파
    [SerializeField] private CanvasGroup cardRightCanvas;     // 오른쪽 알파
    [SerializeField] private float splitDuration = 0.6f;
    [SerializeField] private float splitHorizontalOffset = 120f;
    [SerializeField] private float splitFallDistance = 200f;
    [SerializeField] private float splitRotateAngle = 18f;

    private CutStatus[] _status;
    private bool _isColorRestored;
    private bool _stepCompleted;
    private Coroutine _errorRoutine;

    // 사용자의 모든 선택(정답/오답)을 쌓는 리스트
    private readonly List<CutActionLog> _actionLogs = new List<CutActionLog>();

    // 카드 기본 위치 저장
    private Vector2 _filmCardDefaultPos;
    private bool _defaultPosInitialized;

    // =========================================
    // ProblemStepBase 구현
    // =========================================

    protected override void OnStepEnter()
    {
        // 컷이 하나도 없으면 방어
        if (filmCuts == null || filmCuts.Length == 0)
        {
            Debug.LogWarning("[Problem4_Step2] filmCuts 가 비어 있음");
            if (filmSentenceLabel != null)
                filmSentenceLabel.text = "(설정된 필름 컷이 없습니다)";
            if (stepCompletionGate != null)
                stepCompletionGate.ResetGate(1);
            return;
        }

        _status = new CutStatus[filmCuts.Length];
        for (int i = 0; i < _status.Length; ++i)
            _status[i] = CutStatus.ACTIVE;

        _isColorRestored = false;
        _stepCompleted = false;

        // 시도 로그 초기화
        _actionLogs.Clear();

        // 카드 기본 위치 저장 (한 번만)
        if (filmCardRect != null && !_defaultPosInitialized)
        {
            _filmCardDefaultPos = filmCardRect.anchoredPosition;
            _defaultPosInitialized = true;
        }

        // 컬러/에러 초기 상태
        if (colorRestoreRoot != null) colorRestoreRoot.SetActive(false);
        if (beforeColorRoot != null) beforeColorRoot.SetActive(true);

        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }
        if (errorRoot != null)
            errorRoot.SetActive(false);

        // 버튼 초기화
        if (cutBtn != null) cutBtn.interactable = true;
        if (passBtn != null) passBtn.interactable = true;

        // Gate 초기화
        if (stepCompletionGate != null)
            stepCompletionGate.ResetGate(1);

        // 카드 / 분할 카드 / 가위 초기 상태
        if (filmCardRoot != null)
            filmCardRoot.SetActive(true);
        if (filmCardCanvasGroup != null)
            filmCardCanvasGroup.alpha = 1f;

        if (cardLeftRect != null)
            cardLeftRect.gameObject.SetActive(false);
        if (cardRightRect != null)
            cardRightRect.gameObject.SetActive(false);

        if (cardLeftCanvas != null)
            cardLeftCanvas.alpha = 0f;
        if (cardRightCanvas != null)
            cardRightCanvas.alpha = 0f;

        if (scissorsRect != null)
            scissorsRect.gameObject.SetActive(false);

        // 첫 카드 내용 표시
        RefreshCurrentCutUI();

        // 첫 카드 등장 애니메이션
        if (filmCardRect != null && filmCardCanvasGroup != null)
            StartCoroutine(PlayAppearAnimationForCurrentCard());
    }

    protected override void OnStepExit()
    {
        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }
    }

    // =========================================
    // 현재 컷 찾기 & UI 갱신
    // =========================================

    private int GetCurrentActiveIndex()
    {
        if (_status == null) return -1;

        for (int i = 0; i < _status.Length; i++)
        {
            if (_status[i] == CutStatus.ACTIVE ||
                _status[i] == CutStatus.CUTTING)
            {
                return i;
            }
        }

        return -1;
    }

    private void RefreshCurrentCutUI()
    {
        int idx = GetCurrentActiveIndex();

        if (idx == -1)
        {
            // 더 이상 처리할 컷이 없음 → 완료 체크
            TryCompleteStep();
            return;
        }

        var cut = filmCuts[idx];

        if (filmSentenceLabel != null)
            filmSentenceLabel.text = cut.text;

        if (filmIndexLabel != null)
            filmIndexLabel.text = string.Format("{0} / {1}", idx + 1, filmCuts.Length);

        if (filmCardRoot != null)
            filmCardRoot.SetActive(true);

        // 오류 메시지는 기본적으로 숨김
        if (errorRoot != null)
            errorRoot.SetActive(false);
    }

    // =========================================
    // 버튼 OnClick (여기서 "틀린 선택"도 로그로 남김)
    // =========================================

    public void OnClickCut()
    {
        if (_stepCompleted) return;

        int idx = GetCurrentActiveIndex();
        if (idx == -1) return;

        var cut = filmCuts[idx];

        // "컷"이 정답인 경우 = 생각 컷일 때
        bool isCorrect = cut.isThinking;

        // 무조건 시도 로그에 남김 (정답/오답 상관없음)
        _actionLogs.Add(new CutActionLog
        {
            cutID = cut.cutID,
            action = "cut",
            wasCorrect = isCorrect
        });

        if (isCorrect)
        {
            // 정답: 생각 컷 → 삭제 상태로 마킹 후 컷 연출
            _status[idx] = CutStatus.DELETED;
            StartCoroutine(PlayCutAnimationAndProceed(idx));
        }
        else
        {
            // 오답: 사실 컷인데 잘라내려 함
            ShowError("이 문장은 '사실'이에요. 통과시켜 볼까요?");
        }
    }

    public void OnClickPass()
    {
        if (_stepCompleted) return;

        int idx = GetCurrentActiveIndex();
        if (idx == -1) return;

        var cut = filmCuts[idx];

        // "통과"가 정답인 경우 = 사실 컷일 때
        bool isCorrect = !cut.isThinking;

        // 무조건 시도 로그에 남김
        _actionLogs.Add(new CutActionLog
        {
            cutID = cut.cutID,
            action = "pass",
            wasCorrect = isCorrect
        });

        if (isCorrect)
        {
            // 정답: 사실 컷 → 통과 상태로 마킹 후 PASS 연출
            _status[idx] = CutStatus.PASSED;
            StartCoroutine(PlayPassAnimationAndProceed(idx));
        }
        else
        {
            // 오답: 생각 컷인데 통과시키려 함
            ShowError("이 문장은 '내 생각' 같아요. 잘라내 볼까요?");
        }
    }

    // =========================================
    // 오류 메시지
    // =========================================

    private void ShowError(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            msg = defaultErrorMessage;

        if (errorLabel != null)
            errorLabel.text = msg;

        if (errorRoot != null)
            errorRoot.SetActive(true);

        if (_errorRoutine != null)
            StopCoroutine(_errorRoutine);

        if (errorShowDuration > 0f)
            _errorRoutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorShowDuration);

        if (errorRoot != null)
            errorRoot.SetActive(false);

        _errorRoutine = null;
    }

    // =========================================
    // 완료 조건 체크 + 마무리
    // =========================================

    private bool AllThinkingCutsDeleted()
    {
        if (filmCuts == null || _status == null) return false;

        for (int i = 0; i < filmCuts.Length; i++)
        {
            if (filmCuts[i].isThinking)
            {
                if (_status[i] != CutStatus.DELETED)
                    return false;
            }
        }

        return true;
    }

    private bool AllFactCutsPassed()
    {
        if (filmCuts == null || _status == null) return false;

        for (int i = 0; i < filmCuts.Length; i++)
        {
            if (!filmCuts[i].isThinking)
            {
                if (_status[i] != CutStatus.PASSED)
                    return false;
            }
        }

        return true;
    }

    private void TryCompleteStep()
    {
        if (_stepCompleted) return;

        bool doneThinking = AllThinkingCutsDeleted();
        bool doneFact = AllFactCutsPassed();

        if (!doneThinking || !doneFact)
        {
            // 아직 조건이 안 맞으면, 다시 첫 Active 찾도록 방어적으로 호출
            int idx = GetCurrentActiveIndex();
            if (idx != -1)
                RefreshCurrentCutUI();
            return;
        }

        _stepCompleted = true;

        // 컬러 복원 연출
        _isColorRestored = true;

        if (beforeColorRoot != null)
            beforeColorRoot.SetActive(false);
        if (colorRestoreRoot != null)
            colorRestoreRoot.SetActive(true);

        // 버튼 막기
        if (cutBtn != null) cutBtn.interactable = false;
        if (passBtn != null) passBtn.interactable = false;

        // Attempt 저장
        SaveFilmEditingAttempt();

        // Gate 완료
        if (stepCompletionGate != null)
            stepCompletionGate.MarkOneDone();

        Debug.Log("[Problem4_Step2] 필름 편집 스텝 완료");
    }

    // =========================================
    // 카드 등장 연출 (공통)
    // =========================================

    private IEnumerator PlayAppearAnimationForCurrentCard()
    {
        if (filmCardRect == null || filmCardCanvasGroup == null)
            yield break;

        int idx = GetCurrentActiveIndex();
        if (idx == -1) yield break;   // 더 이상 카드 없음

        if (filmCardRoot != null)
            filmCardRoot.SetActive(true);

        // 출발 위치 세팅
        if (filmAppearStart != null)
            filmCardRect.anchoredPosition = filmAppearStart.anchoredPosition;
        else
            filmCardRect.anchoredPosition = _filmCardDefaultPos + new Vector2(-500f, 0f); // 예비값

        filmCardCanvasGroup.alpha = 0f;

        float t = 0f;
        float dur = Mathf.Max(0.01f, appearDuration);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = t * t * (3f - 2f * t); // smoothstep

            filmCardRect.anchoredPosition = Vector2.Lerp(
                filmCardRect.anchoredPosition,
                _filmCardDefaultPos,
                eased
            );

            filmCardCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        filmCardRect.anchoredPosition = _filmCardDefaultPos;
        filmCardCanvasGroup.alpha = 1f;
    }

    // =========================================
    // CUT 애니메이션 (분할 + 떨어짐) 후 다음 카드
    // =========================================

    private IEnumerator PlayCutAnimationAndProceed(int cutIndex)
    {
        // 버튼 잠금
        if (cutBtn != null) cutBtn.interactable = false;
        if (passBtn != null) passBtn.interactable = false;

        // 1) 가위 내려오는 연출
        if (scissorsRect != null && filmCardRect != null)
        {
            scissorsRect.gameObject.SetActive(true);

            Vector2 cardPos = filmCardRect.anchoredPosition;
            Vector2 startPos = cardPos + scissorsOffsetFromCard;   // 카드 위쪽
            Vector2 endPos = cardPos;                              // 카드 중앙

            scissorsRect.anchoredPosition = startPos;

            float t = 0f;
            float dur = Mathf.Max(0.01f, scissorsMoveDuration);
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float eased = t * t * (3f - 2f * t);

                scissorsRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

                yield return null;
            }

            scissorsRect.anchoredPosition = endPos;
            yield return new WaitForSeconds(0.05f);
        }

        // 2) 카드 반으로 쪼개서 떨어지는 연출
        if (filmCardRect != null &&
            cardLeftRect != null && cardRightRect != null &&
            cardLeftCanvas != null && cardRightCanvas != null)
        {
            Vector2 center = filmCardRect.anchoredPosition;

            // 원래 카드 숨기고 분할 카드 활성화
            if (filmCardRoot != null)
                filmCardRoot.SetActive(false);

            cardLeftRect.gameObject.SetActive(true);
            cardRightRect.gameObject.SetActive(true);

            cardLeftRect.anchoredPosition = center;
            cardRightRect.anchoredPosition = center;

            cardLeftRect.localRotation = Quaternion.identity;
            cardRightRect.localRotation = Quaternion.identity;

            cardLeftCanvas.alpha = 1f;
            cardRightCanvas.alpha = 1f;

            float t2 = 0f;
            float dur2 = Mathf.Max(0.01f, splitDuration);
            while (t2 < 1f)
            {
                t2 += Time.deltaTime / dur2;
                float eased = t2 * t2 * (3f - 2f * t2);
                float alpha = 1f - eased;

                // 좌우로 벌어지면서 아래로 떨어짐
                Vector2 leftPos = center + new Vector2(-splitHorizontalOffset * eased,
                                                       -splitFallDistance * eased);
                Vector2 rightPos = center + new Vector2(splitHorizontalOffset * eased,
                                                        -splitFallDistance * eased);

                cardLeftRect.anchoredPosition = leftPos;
                cardRightRect.anchoredPosition = rightPos;

                cardLeftRect.localRotation = Quaternion.Euler(0f, 0f, -splitRotateAngle * eased);
                cardRightRect.localRotation = Quaternion.Euler(0f, 0f, splitRotateAngle * eased);

                cardLeftCanvas.alpha = alpha;
                cardRightCanvas.alpha = alpha;

                yield return null;
            }

            cardLeftCanvas.alpha = 0f;
            cardRightCanvas.alpha = 0f;

            cardLeftRect.gameObject.SetActive(false);
            cardRightRect.gameObject.SetActive(false);
        }
        else
        {
            // 분할 카드 설정 안 되어 있으면, 최소한 카드만 아래로 떨어뜨리기 (fallback)
            if (filmCardRect != null && filmCardCanvasGroup != null)
            {
                Vector2 start = filmCardRect.anchoredPosition;
                Vector2 end = start + new Vector2(0f, -splitFallDistance);

                float t = 0f;
                float dur = Mathf.Max(0.01f, splitDuration);
                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    float eased = t * t * (3f - 2f * t);

                    filmCardRect.anchoredPosition = Vector2.Lerp(start, end, eased);
                    filmCardCanvasGroup.alpha = 1f - eased;

                    yield return null;
                }
            }
        }

        // 가위 숨기기
        if (scissorsRect != null)
            scissorsRect.gameObject.SetActive(false);

        // 3) 다음 카드로 넘어가거나, 스텝 완료 체크
        RefreshCurrentCutUI();   // 여기서 idx == -1이면 TryCompleteStep 호출됨

        if (_stepCompleted)
            yield break;

        // 다음 카드 등장 연출
        if (filmCardRect != null && filmCardCanvasGroup != null)
        {
            // 분할 카드 애니 이후 원래 카드 다시 활성화해서 등장
            filmCardRoot.SetActive(true);
            filmCardCanvasGroup.alpha = 0f;

            yield return StartCoroutine(PlayAppearAnimationForCurrentCard());
        }

        // 버튼 다시 살리기
        if (cutBtn != null) cutBtn.interactable = true;
        if (passBtn != null) passBtn.interactable = true;
    }

    // =========================================
    // PASS 애니메이션 (지정 위치로 이동 후 사라짐) 후 다음 카드
    // =========================================

    private IEnumerator PlayPassAnimationAndProceed(int cutIndex)
    {
        // 버튼 잠금
        if (cutBtn != null) cutBtn.interactable = false;
        if (passBtn != null) passBtn.interactable = false;

        if (filmCardRect != null && filmCardCanvasGroup != null)
        {
            Vector2 start = filmCardRect.anchoredPosition;
            Vector2 end;

            if (passTargetRect != null)
                end = passTargetRect.anchoredPosition;
            else
                end = start + new Vector2(300f, 0f);  // 타겟 없으면 오른쪽으로 밀려나게

            float t = 0f;
            float dur = Mathf.Max(0.01f, passMoveDuration);

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float eased = t * t * (3f - 2f * t);

                filmCardRect.anchoredPosition = Vector2.Lerp(start, end, eased);
                filmCardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);

                yield return null;
            }

            filmCardCanvasGroup.alpha = 0f;
        }

        // 2) 다음 카드로 넘어가거나, 스텝 완료 체크
        RefreshCurrentCutUI();

        if (_stepCompleted)
            yield break;

        // 다음 카드 등장
        if (filmCardRect != null && filmCardCanvasGroup != null)
        {
            // 새 카드 등장 전에 카드 위치/알파 초기화
            filmCardRoot.SetActive(true);
            filmCardCanvasGroup.alpha = 0f;

            yield return StartCoroutine(PlayAppearAnimationForCurrentCard());
        }

        // 버튼 다시 살리기
        if (cutBtn != null) cutBtn.interactable = true;
        if (passBtn != null) passBtn.interactable = true;
    }

    // =========================================
    // Attempt 저장 (정답/오답 포함)
    // =========================================

    private void SaveFilmEditingAttempt()
    {
        if (filmCuts == null || _status == null)
            return;

        int len = filmCuts.Length;
        var logs = new CutAttemptLog[len];

        for (int i = 0; i < len; i++)
        {
            string statusStr = "active";

            switch (_status[i])
            {
                case CutStatus.DELETED:
                    statusStr = "deleted";
                    break;
                case CutStatus.PASSED:
                    statusStr = "passed";
                    break;
                case CutStatus.CUTTING:
                    statusStr = "cutting";
                    break;
                case CutStatus.ACTIVE:
                    statusStr = "active";
                    break;
            }

            logs[i] = new CutAttemptLog
            {
                cutID = filmCuts[i].cutID,
                text = filmCuts[i].text,
                isThinking = filmCuts[i].isThinking,
                finalStatus = statusStr
            };
        }

        var body = new AttemptBody
        {
            cuts = logs,
            actions = _actionLogs.ToArray()   // 👈 여기 안에 "틀린 선택"도 전부 들어감
        };

        SaveAttempt(body);   // ProblemStepBase 쪽으로 넘겨서 DB 저장
    }
}
