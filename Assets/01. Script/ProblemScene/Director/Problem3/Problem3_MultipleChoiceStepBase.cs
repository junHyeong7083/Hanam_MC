using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 문제3용 객관식 문제 데이터 인터페이스.
/// 문제 ID, 질문 텍스트, 보기 목록, 정답 인덱스, 오답 힌트를 정의한다.
/// </summary>
public interface Problem3_Question
{
    string Id { get; }             // 문제 ID (로그용)
    string QuestionText { get; }   // 질문 텍스트
    string[] Options { get; }      // 보기 텍스트 배열
    int CorrectIndex { get; }      // 정답 인덱스 (0-based)
    string[] WrongHints { get; }   // 각 보기별 오답 힌트 (정답 인덱스 포함, 해당 항목은 사용 안 됨)
}

/// <summary>
/// Director / Problem3 / Step3 ���� ���� ���̽�
/// - MultipleChoiceStepBase<TQuestion> ���� ��
///   ��Ʈ UI / ��ư �� / Attempt ������� �� ���� ó��.
/// - ���� Step3 �ʿ����� ���� �迭 + �ʵ常 ������ �ְ�,
///   �� ���̽��� ���� ó���� ��.
/// </summary>
/// <summary>
/// Problem3_MultipleChoiceStepBase - 문제3용 객관식 스텝 베이스 클래스.
///
/// 【역할】 MultipleChoiceStepBase를 상속하여 힌트 UI, 버튼 색상, Attempt 로그 저장 등
///         문제3에 특화된 공통 처리를 제공한다.
///         - 문제 표시: questionLabel + optionButtons/optionLabels 자동 설정
///         - 정답 처리: 정답 버튼 색상 변경 + 나머지 비활성화 + 힌트 숨김
///         - 오답 처리: 힌트 텍스트 표시 + CanvasGroup 페이드아웃
///         - Attempt 로그: SaveAttempt()로 DB 저장
/// 【패턴】 제네릭 abstract 클래스. TQuestion은 Problem3_Question 인터페이스 구현 필요.
/// 【문제/스텝】 Director 테마 / 문제3에서 사용
/// 【부모 클래스】 MultipleChoiceStepBase → ProblemStepBase
/// 【참조하는 곳】 Director_Problem3_Step3 (구체 자식 클래스)
/// </summary>
public abstract class Problem3_MultipleChoiceStepBase<TQuestion>
    : MultipleChoiceStepBase<TQuestion>
    where TQuestion : class, Problem3_Question
{
    /// <summary>개별 문제 시도 결과를 DB에 저장하기 위한 데이터 구조</summary>
    [Serializable]
    protected class QuestionAttemptBody
    {
        public string stepKey;               // 현재 스텝 키
        public string questionId;            // 문제 ID
        public int questionIndex;            // 문제 인덱스
        public int selectedOptionIndex;      // 선택한 옵션 인덱스
        public string selectedOptionText;    // 선택한 옵션 텍스트
        public bool isCorrect;              // 정답 여부
        public DateTime answeredAt;          // 응답 시각 (UTC)
    }

    // ====== �ڽ��� ���� �ʵ带 ������ �ְ�, ���⼱ ������Ƽ�θ� ���� ======
    protected abstract GameObject HintRoot { get; }
    protected abstract Text HintLabel { get; }
    protected abstract CanvasGroup HintCanvasGroup { get; }
    protected abstract float HintShowDuration { get; }
    protected abstract float HintFadeDuration { get; }
    protected abstract GameObject HideRootOnCorrect { get; }
    protected abstract Color DisabledColor { get; }

    private Coroutine _hintRoutine;

    // =======================
    // MultipleChoiceStepBase ����
    // =======================

    /// <summary>
    /// ���� ������ UI�� ����.
    /// - questionLabel / optionButtons / optionLabels �� ��� �ʵ� ���.
    /// - ��Ʈ UI �ʱ�ȭ.
    /// </summary>
    protected override void ApplyQuestionUI(int index, TQuestion q)
    {
        if (q == null)
        {
            Debug.LogWarning("[Problem3_MultipleChoice] Question is null at index " + index);
            return;
        }

        // ���� �ؽ�Ʈ
        if (questionLabel != null)
            questionLabel.text = q.QuestionText;

        // ��Ʈ �ʱ�ȭ
        ResetHintImmediate();

        // ���� ��ư ����
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

                // 1����: �ν����Ϳ��� ������ label
                if (label != null)
                {
                    label.text = optionText;
                }
                else
                {
                    // 2����: Button �ڽ��� Text
                    var childText = btn.GetComponentInChildren<Text>();
                    if (childText != null)
                    {
                        childText.text = optionText;
                    }
                    else
                    {
                        // 3����: TMP_Text ��� ��
                        var tmp = btn.GetComponentInChildren<TMP_Text>();
                        if (tmp != null)
                            tmp.text = optionText;
                    }
                }
            }
            else
            {
                // ������� �ʴ� ��ư�� ����
                btn.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>���� ������ ���� �ε��� ��ȯ</summary>
    protected override int GetCorrectOptionIndex(TQuestion q)
    {
        if (q == null) return -1;
        return q.CorrectIndex;
    }

    /// <summary>
    /// ����ڰ� ���� �ϳ��� Ŭ������ �� ȣ���.
    /// - Attempt �α븸 ��� (����/���� ó��, ���� ���� �̵��� HandleCorrect/HandleWrong����).
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
    /// ���� Ŭ�� �� ó��.
    /// - ��ư ��/���ͷ��� ó��
    /// - ��Ʈ ����
    /// - hideRootOnCorrect �ɼ� ó��
    /// - Gate ī��Ʈ + ���� ���� or ����
    /// </summary>
    protected override void HandleCorrect(int optionIndex)
    {
        // ���� ������ ��ư ���� ����
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
                    // ���� ��ư�� optionCorrectColor ��� (���̽� �ʵ�)
                    img.color = optionCorrectColor;
                }
                else
                {
                    // �������� ��Ȱ�� ����
                    img.color = DisabledColor;
                }
            }

            btn.interactable = false;
        }

        // ��Ʈ �����
        ResetHintImmediate();

        // ���� �� ��Ʈ ��ü�� ����� ���� ��
        if (HideRootOnCorrect != null)
            HideRootOnCorrect.SetActive(false);

        // Gate 1 ����
        if (completionGate != null)
            completionGate.MarkOneDone();

        // ���� �������� �Ѿ�ų�, �������̸� ����
        GoNextQuestionOrFinish();
    }

    /// <summary>
    /// ���� Ŭ�� �� ó��.
    /// - ��ư ���� �ٲ��� �ʰ�
    /// - ��Ʈ �ؽ�Ʈ�� ��� �����ְ� Fade Out.
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
            hint = "���ݸ� �� �����غ����? ȭ�鿡 ���� �ܼ��� �ٽ� ���÷�������.";

        if (HintLabel != null)
            HintLabel.text = hint;

        if (HintRoot != null)
            HintRoot.SetActive(true);

        var cg = HintCanvasGroup;
        if (cg != null)
        {
            // �ٷ� 1�� ���� �� �ڷ�ƾ���� ���̵�
            cg.alpha = 1f;

            if (_hintRoutine != null)
                StopCoroutine(_hintRoutine);

            _hintRoutine = StartCoroutine(HintFadeRoutine());
        }
    }

    /// <summary>
    /// ��� ������ �� Ǯ���� �� ȣ��.
    /// - ������ ���� ó�� ����. �ʿ��ϸ� override �ؼ� ���.
    /// </summary>
    protected override void OnAllQuestionsCompleted()
    {
        // �ʿ��ϸ� �ڽ� Ŭ�������� override
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
    // ��Ʈ �ڷ�ƾ/��ƿ
    // =======================

    private IEnumerator HintFadeRoutine()
    {
        var cg = HintCanvasGroup;
        if (cg == null)
            yield break;

        float showDuration = Mathf.Max(0f, HintShowDuration);
        float fadeDuration = Mathf.Max(0f, HintFadeDuration);

        // ��� �����ֱ�
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
