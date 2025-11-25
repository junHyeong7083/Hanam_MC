using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface Problem3_Question
{
    string Id { get; }
    string QuestionText { get; }
    string[] Options { get; }
    int CorrectIndex { get; }
    string[] WrongHints { get; }
}

/// <summary>
/// Director / Problem3 / Step3 전용 공통 베이스
/// - MultipleChoiceStepBase<TQuestion> 위에 얹어서
///   힌트 UI / 버튼 색 / Attempt 저장까지 한 번에 처리.
/// - 실제 Step3 쪽에서는 질문 배열 + 필드만 가지고 있고,
///   이 베이스가 전부 처리해 줌.
/// </summary>
public abstract class Problem3_MultipleChoiceStepBase<TQuestion>
    : MultipleChoiceStepBase<TQuestion>
    where TQuestion : class, Problem3_Question
{
    [Serializable]
    protected class QuestionAttemptBody
    {
        public string stepKey;
        public string questionId;
        public int questionIndex;
        public int selectedOptionIndex;
        public string selectedOptionText;
        public bool isCorrect;
        public DateTime answeredAt;
    }

    // ====== 자식이 실제 필드를 가지고 있고, 여기선 프로퍼티로만 접근 ======
    protected abstract GameObject HintRoot { get; }
    protected abstract Text HintLabel { get; }
    protected abstract CanvasGroup HintCanvasGroup { get; }
    protected abstract float HintShowDuration { get; }
    protected abstract float HintFadeDuration { get; }
    protected abstract GameObject HideRootOnCorrect { get; }
    protected abstract Color DisabledColor { get; }

    private Coroutine _hintRoutine;

    // =======================
    // MultipleChoiceStepBase 구현
    // =======================

    /// <summary>
    /// 현재 문항을 UI에 적용.
    /// - questionLabel / optionButtons / optionLabels 는 상속 필드 사용.
    /// - 힌트 UI 초기화.
    /// </summary>
    protected override void ApplyQuestionUI(int index, TQuestion q)
    {
        if (q == null)
        {
            Debug.LogWarning("[Problem3_MultipleChoice] Question is null at index " + index);
            return;
        }

        // 질문 텍스트
        if (questionLabel != null)
            questionLabel.text = q.QuestionText;

        // 힌트 초기화
        ResetHintImmediate();

        // 보기 버튼 세팅
        if (optionButtons == null) return;

        var options = q.Options ?? Array.Empty<string>();
        int optionCount = Mathf.Min(options.Length, optionButtons.Length);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            var label = (optionLabels != null && i < optionLabels.Length)
                ? optionLabels[i]
                : null;

            if (i < optionCount)
            {
                btn.gameObject.SetActive(true);
                btn.interactable = true;

                string optionText = options[i];

                // 1순위: 인스펙터에서 연결한 label
                if (label != null)
                {
                    label.text = optionText;
                }
                else
                {
                    // 2순위: Button 자식의 Text
                    var childText = btn.GetComponentInChildren<Text>();
                    if (childText != null)
                    {
                        childText.text = optionText;
                    }
                    else
                    {
                        // 3순위: TMP_Text 사용 시
                        var tmp = btn.GetComponentInChildren<TMP_Text>();
                        if (tmp != null)
                            tmp.text = optionText;
                    }
                }
            }
            else
            {
                // 사용하지 않는 버튼은 숨김
                btn.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>현재 문항의 정답 인덱스 반환</summary>
    protected override int GetCorrectOptionIndex(TQuestion q)
    {
        if (q == null) return -1;
        return q.CorrectIndex;
    }

    /// <summary>
    /// 사용자가 보기 하나를 클릭했을 때 호출됨.
    /// - Attempt 로깅만 담당 (정답/오답 처리, 다음 문제 이동은 HandleCorrect/HandleWrong에서).
    /// </summary>
    protected override void OnQuestionAttempted(TQuestion q, int optionIndex, bool isCorrect)
    {
        if (q == null) return;

        string optionText = null;
        var options = q.Options;

        if (options != null &&
            optionIndex >= 0 &&
            optionIndex < options.Length)
        {
            optionText = options[optionIndex];
        }

        var body = new QuestionAttemptBody
        {
            stepKey = context != null ? context.CurrentStepKey : null,
            questionId = q.Id,
            questionIndex = _currentQuestionIndex,
            selectedOptionIndex = optionIndex,
            selectedOptionText = optionText,
            isCorrect = isCorrect,
            answeredAt = DateTime.UtcNow
        };

        SaveAttempt(body);
    }

    /// <summary>
    /// 정답 클릭 시 처리.
    /// - 버튼 색/인터랙션 처리
    /// - 힌트 숨김
    /// - hideRootOnCorrect 옵션 처리
    /// - Gate 카운트 + 다음 문항 or 종료
    /// </summary>
    protected override void HandleCorrect(int optionIndex)
    {
        // 현재 문항의 버튼 상태 정리
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null || !btn.gameObject.activeSelf)
                continue;

            var img = btn.targetGraphic as Image;
            if (img != null)
            {
                if (i == optionIndex)
                {
                    // 정답 버튼은 optionCorrectColor 사용 (베이스 필드)
                    img.color = optionCorrectColor;
                }
                else
                {
                    // 나머지는 비활성 느낌
                    img.color = DisabledColor;
                }
            }

            btn.interactable = false;
        }

        // 힌트 숨기기
        ResetHintImmediate();

        // 정답 시 루트 전체를 숨기고 싶을 때
        if (HideRootOnCorrect != null)
            HideRootOnCorrect.SetActive(false);

        // Gate 1 증가
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 다음 문항으로 넘어가거나, 마지막이면 종료
        GoNextQuestionOrFinish();
    }

    /// <summary>
    /// 오답 클릭 시 처리.
    /// - 버튼 색은 바꾸지 않고
    /// - 힌트 텍스트를 잠깐 보여주고 Fade Out.
    /// </summary>
    protected override void HandleWrong(int optionIndex)
    {
        var q = GetQuestion(_currentQuestionIndex);
        if (q == null)
            return;

        var wrongHints = q.WrongHints;
        string hint = null;

        if (wrongHints != null &&
            optionIndex >= 0 &&
            optionIndex < wrongHints.Length &&
            !string.IsNullOrEmpty(wrongHints[optionIndex]))
        {
            hint = wrongHints[optionIndex];
        }

        if (string.IsNullOrEmpty(hint))
            hint = "조금만 더 생각해볼까요? 화면에 나온 단서를 다시 떠올려보세요.";

        if (HintLabel != null)
            HintLabel.text = hint;

        if (HintRoot != null)
            HintRoot.SetActive(true);

        var cg = HintCanvasGroup;
        if (cg != null)
        {
            // 바로 1로 세팅 후 코루틴에서 페이드
            cg.alpha = 1f;

            if (_hintRoutine != null)
                StopCoroutine(_hintRoutine);

            _hintRoutine = StartCoroutine(HintFadeRoutine());
        }
    }

    /// <summary>
    /// 모든 문항을 다 풀었을 때 호출.
    /// - 지금은 별도 처리 없음. 필요하면 override 해서 사용.
    /// </summary>
    protected override void OnAllQuestionsCompleted()
    {
        // 필요하면 자식 클래스에서 override
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_hintRoutine != null)
        {
            StopCoroutine(_hintRoutine);
            _hintRoutine = null;
        }

        ResetHintImmediate();
    }

    // =======================
    // 힌트 코루틴/유틸
    // =======================

    private IEnumerator HintFadeRoutine()
    {
        var cg = HintCanvasGroup;
        if (cg == null)
            yield break;

        float showDuration = Mathf.Max(0f, HintShowDuration);
        float fadeDuration = Mathf.Max(0f, HintFadeDuration);

        // 잠깐 보여주기
        if (showDuration > 0f)
            yield return new WaitForSeconds(showDuration);

        if (fadeDuration <= 0f)
        {
            ResetHintImmediate();
            yield break;
        }

        float t = 0f;
        float startAlpha = cg.alpha;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / fadeDuration);
            cg.alpha = Mathf.Lerp(startAlpha, 0f, x);
            yield return null;
        }

        ResetHintImmediate();
    }

    private void ResetHintImmediate()
    {
        var cg = HintCanvasGroup;
        if (cg != null)
            cg.alpha = 0f;

        if (HintRoot != null)
            HintRoot.SetActive(false);

        _hintRoutine = null;
    }
}
