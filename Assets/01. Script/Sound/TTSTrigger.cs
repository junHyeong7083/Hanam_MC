using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TTS 재생 컴포넌트
/// - OnEnable 시 자동 재생
/// - Button이 있으면 클릭 시 다시 재생
/// - textId 기반으로 SoundManager에서 클립 검색
/// - watchedText 설정 시 텍스트 변경을 감지하여 textId 자동 동기화
/// </summary>
public class TTSTrigger : MonoBehaviour
{
    [Header("TTS 설정")]
    [Tooltip("재생할 대사 textId (DataTable 기준)")]
    [SerializeField] private int textId;

    [Header("자동 동기화")]
    [Tooltip("연결하면 이 Text의 내용이 바뀔 때 자동으로 textId를 업데이트합니다")]
    [SerializeField] private Text watchedText;

    private Button _button;
    private string _lastWatchedText;

    public int TextId
    {
        get => textId;
        set => textId = value;
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        // watchedText가 설정되어 있으면 LateUpdate 자동 동기화에 맡김
        if (watchedText != null) return;

        if (textId > 0)
            Play();
    }

    private void OnDisable()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopTTS();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    private void LateUpdate()
    {
        if (watchedText == null || SoundManager.Instance == null) return;

        string current = watchedText.text;
        if (current == _lastWatchedText) return;
        _lastWatchedText = current;

        int foundId = SoundManager.Instance.FindTextIdByText(current);
        if (foundId > 0)
        {
            textId = foundId;
            Play();
        }
    }

    private void OnClick()
    {
        if (textId > 0)
            Play();
    }

    private void Play()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[TTSTrigger] SoundManager가 없습니다");
            return;
        }

        SoundManager.Instance.PlayTTS(textId);
    }
}
