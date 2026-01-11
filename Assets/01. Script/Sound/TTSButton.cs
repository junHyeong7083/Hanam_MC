using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TTS 재생 버튼 컴포넌트
/// - Inspector에서 문제번호/스텝번호 설정
/// - 버튼 클릭 시 토글 재생 (재생 중이면 중지, 아니면 재생)
/// </summary>
[RequireComponent(typeof(Button))]
public class TTSButton : MonoBehaviour
{
    [Header("TTS 설정")]
    [Tooltip("문제 번호 (1~10)")]
    [SerializeField] private int problemNumber = 1;

    [Tooltip("스텝 번호 (1~3)")]
    [SerializeField] private int stepNumber = 1;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[TTSButton] SoundManager가 없습니다");
            return;
        }

        SoundManager.Instance.ToggleTTS(problemNumber, stepNumber);
    }
}
