using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SoundManager - TTS(하남 캐릭터 음성), BGM, SFX를 관리하는 싱글톤 사운드 매니저
///
/// 【역할】 앱 전체의 사운드를 관리하는 싱글톤 컴포넌트.
///          TTS: textId 기반으로 Resources/TTS/ 폴더의 음성 클립을 재생 (하남 캐릭터 대사)
///          BGM: Resources/BGM/ 폴더의 배경 음악을 루프 재생
///          SFX: Resources/SFX/ 폴더의 효과음을 1회 재생
///          DontDestroyOnLoad로 씬 전환 시에도 유지된다.
///
/// 【참조하는 곳】 DialogueSequencer — PlayTTS(textId)로 대사 음성 재생 / StopTTS()로 정지,
///                TTSTrigger — PlayTTS(textId)로 개별 TTS 트리거,
///                StepFlowController — PlayBGM/StopBGM으로 BGM 제어,
///                ButtonHover — 버튼 효과음 재생,
///                일부 Problem Director Logic (P1, P3, P7, P8, P9 등) — SFX 재생
/// 【참조되는 곳】 ProblemRuntime.L(textId) — 역방향 맵(BuildReverseMap)에서 텍스트→textId 변환 시 사용
///
/// 【흐름】
///   1. Bootstrap 씬에서 Awake() → DontDestroyOnLoad + Resources/TTS/ 전체 클립 로드 및 등록
///   2. 클립 파일명에서 textId 추출 (예: TTS_C01_S01_101010001 → 101010001)
///   3. DialogueSequencer.ShowCurrent()에서 PlayTTS(textId) 호출 → 해당 클립 재생
///   4. 스텝 전환 시 StopTTS()로 이전 음성 정지
/// </summary>
public class SoundManager : MonoBehaviour
{
    /// <summary>싱글톤 인스턴스 (씬에 하나만 존재)</summary>
    private static SoundManager instance;

    /// <summary>
    /// 싱글톤 접근자. 인스턴스가 없으면 FindAnyObjectByType으로 검색한다.
    /// Bootstrap 씬에서 생성된 후 DontDestroyOnLoad로 유지된다.
    /// </summary>
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SoundManager>();
            }
            return instance;
        }
    }

    [Header("TTS Players")]
    /// <summary>TTS 음성 재생용 AudioSource 배열 (여러 개 등록하여 동시 재생 대비, 실제로는 하나씩 재생)</summary>
    [SerializeField] private AudioSource[] ttsPlayers;

    [Header("BGM Player")]
    /// <summary>배경 음악 재생용 AudioSource (loop=true로 설정됨)</summary>
    [SerializeField] private AudioSource bgmPlayer;

    [Header("SFX Player")]
    /// <summary>효과음 재생용 AudioSource (PlayOneShot으로 1회 재생)</summary>
    [SerializeField] private AudioSource sfxPlayer;

    /// <summary>textId → AudioClip 매핑. Awake에서 Resources/TTS/ 폴더의 모든 클립을 등록한다.</summary>
    private Dictionary<int, AudioClip> _ttsClipsByTextId = new Dictionary<int, AudioClip>();

    /// <summary>텍스트 문자열 → textId 역방향 매핑. FindTextIdByText() 최초 호출 시 지연 생성된다.</summary>
    private Dictionary<string, int> _textToIdMap;

    /// <summary>현재 TTS가 재생 중인지 여부. 하나라도 재생 중이면 true.</summary>
    public bool IsTTSPlaying
    {
        get
        {
            if (ttsPlayers == null) return false;
            foreach (var player in ttsPlayers)
            {
                if (player != null && player.isPlaying)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 싱글톤 초기화. 중복 인스턴스 방지 및 TTS 클립 사전 등록.
    /// 주의: 이 클래스는 싱글톤이므로 예외적으로 Awake()를 사용한다.
    /// (일반 컴포넌트는 프로젝트 규칙에 따라 OnEnable() 사용)
    /// </summary>
    private void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        RegisterTTSClips();
    }

    /// <summary>
    /// Resources/TTS/ 폴더의 모든 AudioClip을 로드하여 textId별로 등록한다.
    /// 클립 파일명 규칙: TTS_C{챕터}_S{스테이지}_{textId} (예: TTS_C01_S01_101010001)
    /// 마지막 언더스코어 뒤의 숫자가 textId로 추출된다.
    /// </summary>
    private void RegisterTTSClips()
    {
        _ttsClipsByTextId.Clear();
        _textToIdMap = null;

        // Resources/TTS/ 폴더 하위의 모든 AudioClip을 일괄 로드
        var clips = Resources.LoadAll<AudioClip>("TTS");
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("[SoundManager] Resources/TTS 폴더에서 TTS 클립을 찾을 수 없습니다");
            return;
        }

        foreach (var clip in clips)
        {
            if (clip == null) continue;

            int textId = ExtractTextId(clip.name);
            if (textId > 0)
            {
                _ttsClipsByTextId[textId] = clip;
            }
        }

        Debug.Log($"[SoundManager] TTS 클립 {_ttsClipsByTextId.Count}개 등록 완료 (Resources/TTS)");
    }

    /// <summary>
    /// 클립명에서 textId 추출
    /// 형식: TTS_C01_S01_101010001 → 101010001
    /// </summary>
    private int ExtractTextId(string clipName)
    {
        int lastUnderscore = clipName.LastIndexOf('_');
        if (lastUnderscore >= 0 && lastUnderscore < clipName.Length - 1)
        {
            string idStr = clipName.Substring(lastUnderscore + 1);
            if (int.TryParse(idStr, out int id))
                return id;
        }
        return -1;
    }

    // ============== 역방향 맵 (Text → TextId) ==============

    /// <summary>
    /// UI에 표시된 텍스트 문자열로부터 textId를 역으로 조회한다.
    /// TTSTrigger의 watchedText 자동 동기화 기능에서 사용된다.
    /// 최초 호출 시 역방향 매핑을 지연 생성한다 (Lazy initialization).
    /// </summary>
    /// <param name="text">UI에 표시된 텍스트 문자열</param>
    /// <returns>매칭되는 textId. 못 찾으면 0</returns>
    public int FindTextIdByText(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (_textToIdMap == null) BuildReverseMap();
        return _textToIdMap.TryGetValue(text, out int id) ? id : 0;
    }

    /// <summary>
    /// 역방향 매핑(텍스트 문자열 → textId)을 생성한다.
    /// 등록된 모든 TTS 클립의 textId에 대해 ProblemRuntime.L()로 텍스트를 조회하여 매핑한다.
    /// FindTextIdByText() 최초 호출 시 1회만 실행된다.
    /// </summary>
    private void BuildReverseMap()
    {
        _textToIdMap = new Dictionary<string, int>();
        foreach (var kvp in _ttsClipsByTextId)
        {
            string t = ProblemRuntime.L(kvp.Key);
            if (!string.IsNullOrEmpty(t))
                _textToIdMap[t] = kvp.Key;
        }
        Debug.Log($"[SoundManager] TTS 역방향 맵 {_textToIdMap.Count}개 생성 완료");
    }

    // ============== TTS ==============

    /// <summary>
    /// 사용 가능한(재생 중이 아닌) TTS AudioSource를 반환한다.
    /// 모든 플레이어가 재생 중이면 첫 번째 플레이어를 반환하여 강제 교체한다.
    /// </summary>
    /// <returns>사용 가능한 AudioSource. 배열이 비어있으면 null</returns>
    private AudioSource GetAvailablePlayer()
    {
        if (ttsPlayers == null || ttsPlayers.Length == 0)
            return null;

        // 재생 중이 아닌 플레이어를 우선 사용
        foreach (var player in ttsPlayers)
        {
            if (player != null && !player.isPlaying)
                return player;
        }

        // 모두 재생 중이면 첫 번째 플레이어 반환 (기존 재생이 StopTTS()로 중지됨)
        return ttsPlayers[0];
    }

    /// <summary>
    /// TTS 재생 (textId 기반)
    /// - 기존 재생 중이면 중지 후 새로 재생
    /// </summary>
    public void PlayTTS(int textId)
    {
        if (ttsPlayers == null || ttsPlayers.Length == 0)
        {
            Debug.LogWarning("[SoundManager] ttsPlayers가 할당되지 않았습니다");
            return;
        }

        if (!_ttsClipsByTextId.TryGetValue(textId, out var clip))
        {
            Debug.LogWarning($"[SoundManager] TTS 클립을 찾을 수 없음: textId={textId}");
            return;
        }

        // 기존 재생 중지
        StopTTS();

        var player = GetAvailablePlayer();
        if (player == null)
        {
            Debug.LogWarning("[SoundManager] 사용 가능한 ttsPlayer가 없습니다");
            return;
        }

        player.clip = clip;
        player.Play();
    }

    /// <summary>
    /// TTS 강제 중지 (모든 플레이어)
    /// </summary>
    public void StopTTS()
    {
        if (ttsPlayers == null) return;

        foreach (var player in ttsPlayers)
        {
            if (player != null && player.isPlaying)
                player.Stop();
        }
    }

    /// <summary>
    /// TTS 일시정지 (현재 위치 유지)
    /// </summary>
    public void PauseTTS()
    {
        if (ttsPlayers == null) return;

        foreach (var player in ttsPlayers)
        {
            if (player != null && player.isPlaying)
                player.Pause();
        }
    }

    /// <summary>
    /// TTS 재개 (일시정지된 위치부터)
    /// </summary>
    public void ResumeTTS()
    {
        if (ttsPlayers == null) return;

        foreach (var player in ttsPlayers)
        {
            if (player != null && !player.isPlaying && player.clip != null && player.time > 0f)
                player.UnPause();
        }
    }

    // ============== BGM ==============

    /// <summary>
    /// BGM 재생. Resources/BGM/{clipName} 에서 로드, loop=true
    /// </summary>
    public void PlayBGM(string clipName)
    {
        if (bgmPlayer == null)
        {
            Debug.LogWarning("[SoundManager] bgmPlayer가 할당되지 않았습니다");
            return;
        }

        var clip = Resources.Load<AudioClip>($"BGM/{clipName}");
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없음: Resources/BGM/{clipName}");
            return;
        }

        bgmPlayer.clip = clip;
        bgmPlayer.loop = true;
        bgmPlayer.Play();
    }

    /// <summary>
    /// BGM 정지
    /// </summary>
    public void StopBGM()
    {
        if (bgmPlayer != null)
            bgmPlayer.Stop();
    }

    /// <summary>
    /// BGM 일시정지 (현재 위치 유지)
    /// </summary>
    public void PauseBGM()
    {
        if (bgmPlayer != null && bgmPlayer.isPlaying)
            bgmPlayer.Pause();
    }

    /// <summary>
    /// BGM 재개 (일시정지된 위치부터)
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmPlayer != null && !bgmPlayer.isPlaying)
            bgmPlayer.UnPause();
    }

    // ============== SFX ==============

    /// <summary>
    /// SFX 1회 재생. Resources/SFX/{clipName} 에서 로드
    /// </summary>
    public void PlaySFX(string clipName)
    {
        if (sfxPlayer == null)
        {
            Debug.LogWarning("[SoundManager] sfxPlayer가 할당되지 않았습니다");
            return;
        }

        var clip = Resources.Load<AudioClip>($"SFX/{clipName}");
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없음: Resources/SFX/{clipName}");
            return;
        }

        sfxPlayer.PlayOneShot(clip);
    }
}
