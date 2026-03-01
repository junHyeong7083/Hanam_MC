using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
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
    [SerializeField] private AudioSource[] ttsPlayers;

    private Dictionary<int, AudioClip> _ttsClipsByTextId = new Dictionary<int, AudioClip>();
    private Dictionary<string, int> _textToIdMap;

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

    private void RegisterTTSClips()
    {
        _ttsClipsByTextId.Clear();
        _textToIdMap = null;

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

    public int FindTextIdByText(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (_textToIdMap == null) BuildReverseMap();
        return _textToIdMap.TryGetValue(text, out int id) ? id : 0;
    }

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

    private AudioSource GetAvailablePlayer()
    {
        if (ttsPlayers == null || ttsPlayers.Length == 0)
            return null;

        foreach (var player in ttsPlayers)
        {
            if (player != null && !player.isPlaying)
                return player;
        }

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
}
