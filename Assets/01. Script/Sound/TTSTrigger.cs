using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TTS 재생 컴포넌트
/// - OnEnable 시 항상 자동 재생
/// - hasButton = true: 버튼 클릭으로 토글 가능 (재생 중이면 끄기, 아니면 다시 재생)
/// - hasButton = false: 버튼 없음 (자동 재생만)
/// </summary>
public class TTSTrigger : MonoBehaviour
{
    [Header("TTS 설정")]
    [Tooltip("문제 번호 (1~10)")]
    [SerializeField] private int problemNumber = 1;

    [Tooltip("스텝 번호 (1~3)")]
    [SerializeField] private int stepNumber = 1;

    [Header("버튼 사용 여부")]
    [Tooltip("true: 버튼으로 토글 가능 / false: 버튼 없음 (자동 재생만)")]
    [SerializeField] private bool hasButton = true;

    private Button _button;

    private void Awake()
    {
        if (hasButton)
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }
    }

    private void OnEnable()
    {
        // 항상 자동 재생
        PlayTTS();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// 버튼 클릭 시 토글
    /// </summary>
    private void OnClick()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[TTSTrigger] SoundManager가 없습니다");
            return;
        }

        SoundManager.Instance.ToggleTTS(problemNumber, stepNumber);
    }

    /// <summary>
    /// TTS 재생 (자동 재생용)
    /// </summary>
    private void PlayTTS()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[TTSTrigger] SoundManager가 없습니다");
            return;
        }

        string clipName = $"P{problemNumber}_S{stepNumber}";
        Debug.Log($"[TTSTrigger] 자동 재생: {clipName}");
        SoundManager.Instance.PlayTTS(problemNumber, stepNumber);
    }
}
