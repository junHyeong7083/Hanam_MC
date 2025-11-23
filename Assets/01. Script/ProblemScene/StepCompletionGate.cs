using UnityEngine;
using UnityEngine.UI;

public class StepCompletionGate : MonoBehaviour
{
    [Header("진행도 바 사용 여부")]
    [SerializeField] private bool useProgressFill = false;

    [Header("진행도 Fill 이미지 (옵션)")]
    [SerializeField] private Image progressFillImage;

    [Header("Complete Root 사용 여부")]
    [SerializeField] private bool useCompleteRoot = true;

    [Header("다음 스텝으로 넘어가기 버튼 루트 (Complete Root)")]
    [SerializeField] private GameObject completeRoot;

    [Header("자동 진행용 StepFlowController (useCompleteRoot=false 일 때 사용)")]
    [SerializeField] private StepFlowController stepFlowController;

    [Header("Hide Root 사용 여부")]
    [SerializeField] private bool useHideRoot = true;

    [Header("CompleteRoot가 켜질 때 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRoot;

    private int _totalCount;
    private int _currentCount;

    private bool _initialized;
    private bool _autoNextFired;   // 자동 진행 한 번만 호출하기 위한 플래그

    private void OnEnable()
    {
        // 새로 켜질 때 자동 진행 플래그 리셋
        _autoNextFired = false;

        // 이 컴포넌트가 처음 켜질 때 한 번 기본 상태 적용
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
    /// 이 스텝에서 "총 몇 개를 채우면 완료로 볼지" 셋업
    /// ex) 필름 4개면 ResetGate(4)
    /// </summary>
    public void ResetGate(int total)
    {
        _totalCount = Mathf.Max(0, total);
        _currentCount = 0;
        _autoNextFired = false;
        Apply();
    }

    /// <summary>
    /// 새로운 항목이 "처음으로" 완료되었을 때 한 번만 호출
    /// ex) 아직 안 눌렀던 필름을 클릭했을 때
    /// </summary>
    public void MarkOneDone()
    {
        if (_totalCount <= 0)
            return;

        _currentCount = Mathf.Clamp(_currentCount + 1, 0, _totalCount);
        Apply();
    }

    private void Apply()
    {
        // 1) 진행도 계산
        float progress = (_totalCount > 0)
            ? (float)_currentCount / _totalCount
            : 0f;

        // 2) 진행도 바 업데이트 (있으면만 + 사용 옵션 따라)
        if (progressFillImage != null)
        {
            progressFillImage.gameObject.SetActive(useProgressFill);

            if (useProgressFill)
                progressFillImage.fillAmount = progress;
        }

        // 3) 완료 여부
        bool completed = (_totalCount > 0 && _currentCount >= _totalCount);

        // 4) 함께 숨기고 싶은 루트 처리 (있으면만 + 옵션 체크)
        if (useHideRoot && hideRoot != null)
            hideRoot.SetActive(!completed);

        // 5) 완료 처리
        if (useCompleteRoot)
        {
            // 버튼으로 제어하는 모드: CompleteRoot 활성/비활성만
            if (completeRoot != null)
                completeRoot.SetActive(completed);
        }
        else
        {
            // 자동으로 다음 스텝으로 넘어가는 모드
            if (completed && !_autoNextFired && stepFlowController != null)
            {
                _autoNextFired = true;
                stepFlowController.NextStep();
            }
        }
    }
}
