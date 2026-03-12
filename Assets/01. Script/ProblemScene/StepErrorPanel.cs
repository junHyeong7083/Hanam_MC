using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// StepErrorPanel - 오답 시 일시적으로 표시되는 에러/피드백 패널
///
/// 【역할】 사용자가 오답을 선택했을 때 "다시 생각해볼까요?" 같은 메시지를 일정 시간 동안 표시한다.
///          showDuration 후 자동으로 숨겨진다.
/// 【참조하는 곳】 MultipleChoiceStepBase.HandleWrong()에서 호출 가능,
///                각 Problem Director Logic에서 오답 처리 시 사용
/// 【참조되는 곳】 없음 (독립적인 UI 컴포넌트)
/// 【흐름】 Show(owner, msg) → 에러 메시지 표시 → showDuration 초 대기 → 자동 숨김
/// </summary>
public class StepErrorPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;   // 에러 패널의 루트 오브젝트 (활성/비활성 제어)
    [SerializeField] private Text label;        // 에러 메시지를 표시할 Text UI
    [SerializeField] private string defaultMessage = "다시 생각해볼까요?"; // 메시지 미지정 시 기본 텍스트
    [SerializeField] private float showDuration = 1f; // 패널 표시 시간 (초). 0이면 자동 숨김 안 함

    Coroutine _routine;    // 자동 숨김 코루틴 참조 (중복 방지용)
    MonoBehaviour _owner;  // 코루틴 실행 주체 (에러 패널 자체가 아닌 호출자의 MonoBehaviour)

    /// <summary>초기 상태: 에러 패널 숨김</summary>
    void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    /// <summary>
    /// 에러 패널을 표시한다. showDuration 후 자동으로 숨겨진다.
    /// </summary>
    /// <param name="owner">코루틴 실행 주체. 패널이 비활성 상태일 수 있으므로 호출자의 MonoBehaviour를 전달한다.</param>
    /// <param name="msg">표시할 에러 메시지. null이면 defaultMessage 사용.</param>
    public void Show(MonoBehaviour owner, string msg = null)
    {
        _owner = owner;
        if (string.IsNullOrEmpty(msg)) msg = defaultMessage;

        if (label != null) label.text = msg;
        if (root != null) root.SetActive(true);

        if (_routine != null) _owner.StopCoroutine(_routine);
        if (showDuration > 0f && _owner != null)
            _routine = _owner.StartCoroutine(HideAfterDelay());
    }

    /// <summary>showDuration 후 에러 패널을 자동으로 숨기는 코루틴</summary>
    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);
        if (root != null) root.SetActive(false);
        _routine = null;
    }
}
