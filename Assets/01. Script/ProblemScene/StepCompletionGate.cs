using UnityEngine;
using UnityEngine.UI;

public class StepCompletionGate : MonoBehaviour
{
    [Header("진행도 바 사용 여부")]
    [SerializeField] private bool useProgressFill = true;

    [Header("진행도 Fill 이미지 (옵션)")]
    [SerializeField] private Image progressFillImage;

    [Header("다음 스텝으로 넘어가기 버튼 루트 (Complete Root)")]
    [SerializeField] private GameObject completeRoot;   // 없으면 버튼 제어 안 함

    [Header("CompleteRoot가 켜질 때 숨길 루트 (옵션)")]
    [SerializeField] private GameObject hideRoot;

    private int _totalCount;
    private int _currentCount;

    private bool _initialized;

    private void OnEnable()
    {
        // 이 컴포넌트가 처음 켜질 때 한 번 기본 상태 적용
        if (!_initialized)
        {
            Apply();
            _initialized = true;
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
            // useProgressFill에 따라 통째로 켜고 끄기
            progressFillImage.gameObject.SetActive(useProgressFill);

            if (useProgressFill)
                progressFillImage.fillAmount = progress;
        }

        // 3) 완료 여부
        bool completed = (_totalCount > 0 && _currentCount >= _totalCount);

        // 4) 완료 버튼 켜기 (있으면만)
        if (completeRoot != null)
            completeRoot.SetActive(completed);

        // 5) 함께 숨기고 싶은 루트 처리 (있으면만)
        if (hideRoot != null)
            hideRoot.SetActive(!completed);
    }
}
