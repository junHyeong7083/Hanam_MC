using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StepCompletionGate - 스텝 내 완료 조건을 관리하는 게이트 컴포넌트
///
/// 【역할】 스텝 안에서 "N개 중 몇 개를 완료했는가"를 추적한다.
///          ResetGate(total)로 초기화 → MarkOneDone()으로 하나씩 완료 카운트 증가
///          → 전부 완료되면 completeRoot(다음 스텝 버튼)를 표시하거나,
///            자동으로 StepFlowController.NextStep()을 호출한다.
/// 【참조하는 곳】 MultipleChoiceStepBase (정답 맞출 때마다 MarkOneDone),
///                RandomCardSequenceStepBase (카드 처리마다 MarkOneDone),
///                InventoryDropTargetStepBase (아이템 활성화 시 MarkOneDone),
///                Problem1~10의 Step1~3 Binder/Logic 클래스 (ResetGate/MarkOneDone 호출),
///                Director_Problem1_Step3_SummaryPanel (요약 패널에서 MarkOneDone)
/// 【참조되는 곳】 StepFlowController (자동 넘김 시), Image (progressFillImage)
/// 【흐름】 ResetGate(total) → MarkOneDone() × total회 → Apply() → 완료 시 completeRoot 표시 or NextStep()
/// </summary>
public class StepCompletionGate : MonoBehaviour
{
    [Header("진행도 바 사용 여부")]
    [SerializeField] private bool useProgressFill = false; // true이면 progressFillImage로 진행률을 시각적으로 표시

    [Header("진행도 Fill 이미지 (옵션)")]
    [SerializeField] private Image progressFillImage; // fillAmount를 0~1로 조절하는 UI Image (Filled 타입)

    [Header("Complete Root 사용 여부")]
    [SerializeField] private bool useCompleteRoot = true; // true이면 완료 시 completeRoot를 표시 (수동 버튼 방식)

    [Header("다음 스텝으로 넘어가는 버튼 루트 (Complete Root)")]
    [SerializeField] private GameObject completeRoot; // 모든 조건 완료 시 표시되는 "다음" 버튼의 부모 오브젝트

    [Header("자동 넘김용 StepFlowController (useCompleteRoot=false 일 때 사용)")]
    [SerializeField] private StepFlowController stepFlowController; // 자동 모드: 완료 시 직접 NextStep() 호출할 대상

    [Header("Hide Root 사용 여부")]
    [SerializeField] private bool useHideRoot = true; // true이면 완료 시 hideRoot를 숨김

    [Header("CompleteRoot가 보일 때 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRoot; // 완료 시 숨길 UI (예: 문제 선택지 영역)

    private int _totalCount;      // 완료에 필요한 총 조건 수
    private int _currentCount;    // 현재까지 완료된 조건 수

    private bool _initialized;       // Apply()가 최초 실행되었는지 여부
    private bool _autoNextFired;     // 자동 넘김이 이미 발동했는지 (중복 호출 방지 플래그)

    /// <summary>
    /// 활성화 시 자동 넘김 플래그를 리셋하고 현재 상태를 UI에 반영한다.
    /// 스텝이 다시 활성화될 때마다 호출되어 UI를 최신 상태로 갱신한다.
    /// </summary>
    private void OnEnable()
    {
        // 활성화 시 자동 넘김 플래그 리셋
        _autoNextFired = false;

        // 이 컴포넌트가 처음 활성화될 때 한 번 기본 상태 적용
        if (!_initialized)
        {
            Apply();
            _initialized = true;
        }
        else
        {
            Apply();
        }
    }

    /// <summary>
    /// 각 스텝에서 "몇 개 조건 채우면 완료인지 설정"
    /// ex) 필드 4개면 ResetGate(4)
    /// </summary>
    public void ResetGate(int total)
    {
        _totalCount = Mathf.Max(0, total);
        _currentCount = 0;
        _autoNextFired = false;
        Apply();
    }

    /// <summary>
    /// 새로운 항목이 "처음으로" 완료되었을 때 한 번씩 호출
    /// ex) 빈칸 중 하나의 필드가 클릭되었을 때
    /// </summary>
    public void MarkOneDone()
    {
        if (_totalCount <= 0)
            return;

        _currentCount = Mathf.Clamp(_currentCount + 1, 0, _totalCount);
        Apply();
    }

    /// <summary>
    /// 완료 상태를 하나 되돌림 (선택 해제 등)
    /// </summary>
    public void MarkOneUndone()
    {
        if (_totalCount <= 0)
            return;

        _currentCount = Mathf.Clamp(_currentCount - 1, 0, _totalCount);
        Apply();
    }

    /// <summary>
    /// 현재 진행 상태를 UI에 반영하는 내부 메서드.
    /// - 진행도 바(progressFillImage) 업데이트
    /// - 완료 여부에 따라 completeRoot 표시/숨김 또는 자동 NextStep() 호출
    /// - hideRoot 표시/숨김
    /// </summary>
    private void Apply()
    {
        // 1) 진행도 계산 (0.0 ~ 1.0)
        float progress = (_totalCount > 0)
            ? (float)_currentCount / _totalCount
            : 0f;

        // 2) 진행도 바 업데이트 (사용할때만 + 옵션 체크)
        if (progressFillImage != null)
        {
            progressFillImage.gameObject.SetActive(useProgressFill);

            if (useProgressFill)
                progressFillImage.fillAmount = progress;
        }

        // 3) 완료 여부
        bool completed = (_totalCount > 0 && _currentCount >= _totalCount);

        // 4) 함께 숨겨질 루트 처리 (사용할때만 + 옵션 체크)
        if (useHideRoot && hideRoot != null)
            hideRoot.SetActive(!completed);

        // 5) 완료 처리
        if (useCompleteRoot)
        {
            // 버튼으로 진행하는 방식: CompleteRoot 활성/비활성화
            if (completeRoot != null)
                completeRoot.SetActive(completed);
        }
        else
        {
            // 자동으로 다음 스텝으로 넘어가는 방식
            if (completed && !_autoNextFired && stepFlowController != null)
            {
                _autoNextFired = true;
                stepFlowController.NextStep();
            }
        }
    }
}
