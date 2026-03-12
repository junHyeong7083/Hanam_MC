using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TTSTrigger - 특정 textId의 TTS 음성을 자동/수동으로 트리거하는 간편 컴포넌트
///
/// 【역할】 UI 오브젝트에 부착하여 TTS 음성을 재생하는 두 가지 방식을 제공한다:
///          1. 고정 textId 모드: OnEnable 시 자동 재생 + 버튼 클릭 시 다시 재생
///          2. watchedText 모드: 연결된 Text 컴포넌트의 내용이 변경될 때마다
///             자동으로 textId를 동기화하고 TTS를 재생 (DialogueSequencer 등과 연동)
///
/// 【참조하는 곳】 HanamBoxRoot 프리팹 내부의 TTS 버튼,
///                대사 텍스트 옆 스피커 아이콘 버튼 등에 부착
/// 【참조되는 곳】 SoundManager.Instance.PlayTTS(textId) — TTS 재생,
///                SoundManager.Instance.FindTextIdByText() — watchedText 모드에서 텍스트→textId 변환
///
/// 【흐름 (고정 textId 모드)】
///   1. OnEnable() → textId > 0이면 즉시 Play()
///   2. Button이 있으면 클릭 시 OnClick() → Play()
///   3. OnDisable() → StopTTS()로 음성 정지
///
/// 【흐름 (watchedText 모드)】
///   1. OnEnable() → watchedText가 설정되어 있으면 자동 재생 건너뜀
///   2. LateUpdate()에서 매 프레임 watchedText.text 변경 감지
///   3. 변경 시 SoundManager.FindTextIdByText()로 textId 조회 → Play()
/// </summary>
public class TTSTrigger : MonoBehaviour
{
    [Header("TTS 설정")]
    [Tooltip("재생할 대사 textId (DataTable 기준)")]
    /// <summary>재생할 대사의 textId. 인스펙터에서 설정하거나 watchedText 모드에서 자동 업데이트된다.</summary>
    [SerializeField] private int textId;

    [Header("자동 동기화")]
    [Tooltip("연결하면 이 Text의 내용이 바뀔 때 자동으로 textId를 업데이트합니다")]
    /// <summary>감시할 Text 컴포넌트. 설정하면 이 텍스트 내용이 변경될 때 자동으로 textId를 동기화한다.</summary>
    [SerializeField] private Text watchedText;

    /// <summary>같은 오브젝트에 Button이 있으면 클릭 시 TTS 재생용으로 사용</summary>
    private Button _button;

    /// <summary>watchedText의 이전 프레임 텍스트 값 (변경 감지용)</summary>
    private string _lastWatchedText;

    /// <summary>textId의 외부 접근용 프로퍼티 (코드에서 동적으로 textId 설정 가능)</summary>
    public int TextId
    {
        get => textId;
        set => textId = value;
    }

    /// <summary>
    /// 활성화 시 호출. Button이 있으면 클릭 리스너를 등록하고,
    /// watchedText가 없고 textId가 설정되어 있으면 즉시 TTS를 재생한다.
    /// watchedText가 설정된 경우에는 LateUpdate에서 자동 동기화에 맡긴다.
    /// </summary>
    private void OnEnable()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnClick);


        // watchedText가 설정되어 있으면 LateUpdate 자동 동기화에 맡김
        if (watchedText != null) return;

        if (textId > 0)
            Play();
    }

    /// <summary>비활성화 시 현재 재생 중인 TTS를 정지한다.</summary>
    private void OnDisable()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopTTS();
    }

    /// <summary>파괴 시 Button 클릭 리스너를 해제한다 (메모리 누수 방지).</summary>
    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// 매 프레임 watchedText의 내용 변경을 감지한다.
    /// 텍스트가 변경되면 SoundManager.FindTextIdByText()로 역방향 조회하여
    /// textId를 자동 업데이트하고 TTS를 재생한다.
    /// LateUpdate를 사용하는 이유: 같은 프레임에서 Text가 업데이트된 후에 감지하기 위함.
    /// </summary>
    private void LateUpdate()
    {
        if (watchedText == null || SoundManager.Instance == null) return;

        string current = watchedText.text;
        // 이전 프레임과 같으면 변경 없음 → 건너뜀
        if (current == _lastWatchedText) return;
        _lastWatchedText = current;

        // 텍스트 문자열로부터 textId를 역방향 조회
        int foundId = SoundManager.Instance.FindTextIdByText(current);
        if (foundId > 0)
        {
            textId = foundId;
            Play();
        }
    }

    /// <summary>버튼 클릭 시 현재 textId의 TTS를 재생한다.</summary>
    private void OnClick()
    {
        if (textId > 0)
        {
            Debug.Log("왜 예전 하남이 목소리가 들리는 것인가 : " + textId);
            Play();
        }

    }

    /// <summary>SoundManager를 통해 현재 textId의 TTS 음성을 재생한다.</summary>
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
