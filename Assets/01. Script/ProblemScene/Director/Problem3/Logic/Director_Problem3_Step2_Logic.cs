using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem3 / Step2 로직 베이스.
/// - 시나리오를 단계별로 "새로운 관점"으로 다시 작성 유도.
/// - 애니메이션은 EffectController에 위임, 로직만 담당.
/// </summary>
public interface IRewriteStepData
{
    int Id { get; }
    string OriginalText { get; }
    string RewrittenText { get; }
    string[] Options { get; }
}

public abstract class Director_Problem3_Step2_Logic : ProblemStepBase
{
    // ===== 자식에서 넘겨주는 추상 프로퍼티들 =====

    [Header("재작성 단계 데이터 (자식 구현)")]
    protected abstract IRewriteStepData[] Steps { get; }

    [Header("이펙트 컨트롤러")]
    protected abstract Problem3_Step2_EffectController EffectController { get; }

    [Header("옵션 버튼들 (최대 N개)")]
    protected abstract Button[] OptionButtons { get; }
    protected abstract Text[] OptionLabels { get; }

    [Header("옵션 버튼 이미지 (인덱스별)")]
    protected abstract Image[] OptionButtonImages { get; }      // 버튼의 Image 컴포넌트
    protected abstract Sprite[] OptionNormalSprites { get; }    // 기본 상태 스프라이트
    protected abstract Sprite[] OptionSelectedSprites { get; }  // 선택 상태 스프라이트
    protected abstract GameObject[] OptionSelectedMarkers { get; } // 선택 시 체크 마커

    [Header("넘버링 이미지 (인덱스별)")]
    protected abstract Image[] NumberingImages { get; }         // 넘버링 Image 컴포넌트
    protected abstract Sprite[] NumberingNormalSprites { get; } // 기본 상태 넘버링 스프라이트
    protected abstract Sprite[] NumberingSelectedSprites { get; } // 선택 상태 넘버링 스프라이트

    [Header("상단 진행도 점들 (옵션)")]
    protected abstract GameObject[] ProgressDots { get; }

    [Header("하단 네비게이션 버튼")]
    protected abstract GameObject NextProblemButton { get; }  // 다음문제 버튼 (중간 단계용)
    protected abstract GameObject NextStepButton { get; }     // 다음스텝 버튼 (마지막 단계용)

    [Header("완료 게이트 (옵션)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // ===== 내부 상태 =====
    private int _currentIndex;
    private int[] _selectedOptionIndices;   // step별 선택된 옵션 인덱스 (-1 이면 미선택)
    private bool[] _stepCompleted;          // step별 재작성 완료 여부

    // 애니메이션 중인지 여부 (EffectController에서 확인)
    private bool IsAnimating => EffectController != null && EffectController.IsAnimating;

    // === Attempt 기록용 DTO ===
    [Serializable]
    private class AttemptStepLog
    {
        public int stepId;
        public string originalText;
        public string selectedOption;
        public string rewrittenText;
    }

    [Serializable]
    private class AttemptBody
    {
        public AttemptStepLog[] steps;
    }

    // =============================
    // ProblemStepBase 오버라이드
    // =============================
    protected override void OnStepEnter()
    {
        Debug.Log("[Problem3_Step2] OnStepEnter");

        var steps = Steps;
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[Problem3_Step2] steps 데이터가 비어있음");
            if (CompletionGate != null)
            {
                CompletionGate.ResetGate(1);
            }
            return;
        }

        _currentIndex = 0;
        _selectedOptionIndices = new int[steps.Length];
        _stepCompleted = new bool[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            _selectedOptionIndices[i] = -1;
            _stepCompleted[i] = false;
        }

        if (CompletionGate != null)
        {
            // 이 스텝 전체를 1칸짜리 Gate로 사용
            CompletionGate.ResetGate(1);
        }

        RefreshAllUI();
    }

    protected override void OnStepExit()
    {
        // 필요 시 정리
    }

    // =============================
    // UI 갱신
    // =============================

    private void RefreshAllUI()
    {
        ApplyProgressDots();
        ApplySentenceOriginal();
        SetupOptionsForCurrentStep();
        UpdateNextButton();
    }

    private void ApplyProgressDots()
    {
        var dots = ProgressDots;
        if (dots == null || dots.Length == 0)
            return;

        // 현재 인덱스만 활성화, 나머지는 비활성화
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
                dots[i].SetActive(i == _currentIndex);
        }
    }

    private void ApplySentenceOriginal()
    {
        var steps = Steps;
        if (steps == null || steps.Length == 0 || _currentIndex < 0 || _currentIndex >= steps.Length)
            return;

        var step = steps[_currentIndex];

        // 이펙트 컨트롤러에 위임
        var effectController = EffectController;
        if (effectController != null)
        {
            effectController.ResetForNextStep();
            effectController.ShowOriginalTextImmediate(step.OriginalText);
        }
    }

    private void SetupOptionsForCurrentStep()
    {
        var optionButtons = OptionButtons;
        if (optionButtons == null || optionButtons.Length == 0)
            return;

        var steps = Steps;
        var step = steps[_currentIndex];
        var options = (step.Options != null) ? step.Options : Array.Empty<string>();

        var optionLabels = OptionLabels;
        var optionImages = OptionButtonImages;
        var normalSprites = OptionNormalSprites;
        var markers = OptionSelectedMarkers;
        var numberingImages = NumberingImages;
        var numberingNormalSprites = NumberingNormalSprites;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            Text label = (optionLabels != null && i < optionLabels.Length) ? optionLabels[i] : null;

            bool active = i < options.Length;

            if (btn != null)
            {
                btn.gameObject.SetActive(active);

                if (active)
                {
                    btn.interactable = true;

                    // 기본 스프라이트로 초기화
                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null &&
                        normalSprites != null && i < normalSprites.Length && normalSprites[i] != null)
                    {
                        optionImages[i].sprite = normalSprites[i];
                    }

                    // 넘버링 이미지도 기본 스프라이트로 초기화
                    if (numberingImages != null && i < numberingImages.Length && numberingImages[i] != null &&
                        numberingNormalSprites != null && i < numberingNormalSprites.Length && numberingNormalSprites[i] != null)
                    {
                        numberingImages[i].sprite = numberingNormalSprites[i];
                    }
                }
            }

            if (active && label != null)
            {
                label.text = options[i];
            }

            // 마커 숨기기
            if (markers != null && i < markers.Length && markers[i] != null)
            {
                markers[i].SetActive(false);
            }
        }

        // 새 단계에서는 선택 값 초기화
        if (_selectedOptionIndices != null)
        {
            _selectedOptionIndices[_currentIndex] = -1;
        }
    }

    private void UpdateNextButton()
    {
        bool show = (_stepCompleted != null &&
                     _currentIndex >= 0 &&
                     _currentIndex < _stepCompleted.Length &&
                     _stepCompleted[_currentIndex]);

        bool isLast = (_currentIndex == Steps.Length - 1);

        // 중간 단계: 다음문제 버튼만 표시
        // 마지막 단계: 다음스텝 버튼만 표시
        if (NextProblemButton != null)
            NextProblemButton.SetActive(show && !isLast);

        if (NextStepButton != null)
            NextStepButton.SetActive(show && isLast);
    }

    // =============================
    // 옵션 클릭 & 재작성 시작
    // =============================

    /// <summary>
    /// 옵션 버튼 OnClick에서 index를 넘겨 호출.
    /// ex) Button(0) -> OnClickOption(0)
    /// </summary>
    public void OnClickOption(int optionIndex)
    {
        if (IsAnimating)
            return;

        var steps = Steps;
        if (steps == null || steps.Length == 0)
            return;

        if (_currentIndex < 0 || _currentIndex >= steps.Length)
            return;

        var step = steps[_currentIndex];
        if (step.Options == null || optionIndex < 0 || optionIndex >= step.Options.Length)
            return;

        // 이미 선택된 상태면 무시
        if (_selectedOptionIndices != null && _selectedOptionIndices[_currentIndex] != -1)
            return;

        _selectedOptionIndices[_currentIndex] = optionIndex;

        // 버튼 비주얼 업데이트
        ApplyOptionSelectionVisual(optionIndex);

        // 이펙트 컨트롤러에 재작성 애니메이션 요청
        var effectController = EffectController;
        if (effectController != null)
        {
            effectController.PlayRewriteSequence(step.RewrittenText, OnRewriteComplete);
        }
        else
        {
            // 이펙트 컨트롤러 없으면 바로 완료 처리
            OnRewriteComplete();
        }
    }

    private void ApplyOptionSelectionVisual(int selectedIndex)
    {
        var optionButtons = OptionButtons;
        if (optionButtons == null)
            return;

        var steps = Steps;
        var step = steps[_currentIndex];
        int optionCount = (step.Options != null) ? step.Options.Length : 0;

        var optionImages = OptionButtonImages;
        var normalSprites = OptionNormalSprites;
        var selectedSprites = OptionSelectedSprites;
        var markers = OptionSelectedMarkers;
        var numberingImages = NumberingImages;
        var numberingNormalSprites = NumberingNormalSprites;
        var numberingSelectedSprites = NumberingSelectedSprites;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            if (i >= optionCount)
            {
                btn.gameObject.SetActive(false);
                continue;
            }

            bool isSelected = (i == selectedIndex);

            // 한 번 선택하면 다시 못 누르도록 전부 비활성화
            btn.interactable = false;

            // 선택된 버튼은 선택 스프라이트로, 나머지는 기본 스프라이트 유지
            if (optionImages != null && i < optionImages.Length && optionImages[i] != null)
            {
                if (isSelected && selectedSprites != null && i < selectedSprites.Length && selectedSprites[i] != null)
                {
                    optionImages[i].sprite = selectedSprites[i];
                }
                else if (normalSprites != null && i < normalSprites.Length && normalSprites[i] != null)
                {
                    optionImages[i].sprite = normalSprites[i];
                }
            }

            // 넘버링 이미지도 선택 여부에 따라 스프라이트 변경
            if (numberingImages != null && i < numberingImages.Length && numberingImages[i] != null)
            {
                if (isSelected && numberingSelectedSprites != null && i < numberingSelectedSprites.Length && numberingSelectedSprites[i] != null)
                {
                    numberingImages[i].sprite = numberingSelectedSprites[i];
                }
                else if (numberingNormalSprites != null && i < numberingNormalSprites.Length && numberingNormalSprites[i] != null)
                {
                    numberingImages[i].sprite = numberingNormalSprites[i];
                }
            }

            // 선택된 버튼에만 마커 표시
            if (markers != null && i < markers.Length && markers[i] != null)
            {
                markers[i].SetActive(isSelected);
            }
        }
    }

    /// <summary>
    /// 재작성 애니메이션 완료 콜백
    /// </summary>
    private void OnRewriteComplete()
    {
        // 현재 step 완료 플래그
        if (_stepCompleted != null && _currentIndex >= 0 && _currentIndex < _stepCompleted.Length)
            _stepCompleted[_currentIndex] = true;

        // 다음 버튼 표시 갱신
        UpdateNextButton();
    }

    // =============================
    // 다음/완료 버튼
    // =============================

    /// <summary>
    /// 하단 "다음 장면" / "강점 찾기 단계로" 버튼에서 호출.
    /// </summary>
    public void OnClickNextOrComplete()
    {
        if (IsAnimating)
            return;

        var steps = Steps;
        if (steps == null || steps.Length == 0)
            return;

        if (_stepCompleted == null ||
            _currentIndex < 0 ||
            _currentIndex >= _stepCompleted.Length ||
            !_stepCompleted[_currentIndex])
        {
            Debug.Log("[Problem3_Step2] 현재 이 단계의 재작성이 완료되지 않음");
            return;
        }

        bool isLast = (_currentIndex == steps.Length - 1);

        if (!isLast)
        {
            // 다음 내부 step 이동
            _currentIndex++;

            // 새 step UI 갱신
            RefreshAllUI();
        }
        else
        {
            // 전체 step(3단계) 완료 시 Attempt 저장 + Gate 완료
            SaveRewriteLogToDb();

            if (CompletionGate != null)
            {
                CompletionGate.MarkOneDone();
            }
        }
    }

    // =============================
    // DB 저장 (Attempt)
    // =============================

    private void SaveRewriteLogToDb()
    {
        var steps = Steps;
        if (steps == null || steps.Length == 0)
            return;

        int len = steps.Length;
        var logs = new AttemptStepLog[len];

        for (int i = 0; i < len; i++)
        {
            var s = steps[i];

            int selIndex = (_selectedOptionIndices != null && i < _selectedOptionIndices.Length)
                ? _selectedOptionIndices[i]
                : -1;

            string selectedOptionText = null;
            var options = s.Options;
            if (selIndex >= 0 && options != null && selIndex < options.Length)
            {
                selectedOptionText = options[selIndex];
            }

            logs[i] = new AttemptStepLog
            {
                stepId = s.Id,
                originalText = s.OriginalText,
                selectedOption = selectedOptionText,
                rewrittenText = s.RewrittenText
            };
        }

        var body = new AttemptBody
        {
            steps = logs
        };

        // ProblemStepBase 에서 DBGateway + UserDataService 를 통해 저장
        SaveAttempt(body);
        Debug.Log("[Problem3_Step2] SaveRewriteLogToDb 호출 완료");
    }
}
