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

    [Header("TTS Clips")]
    [SerializeField] private AudioClip[] ttsAudioClips;  // P1_S1, P1_S2, P2_S1 등
    [SerializeField] private AudioSource[] ttsPlayers;   // TTS 전용 AudioSource 배열

    private Dictionary<string, AudioClip> ttsClipsDic = new Dictionary<string, AudioClip>();

    /// <summary>TTS 재생 상태 (하나라도 재생 중이면 true)</summary>
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

        // TTS 클립 등록
        foreach (AudioClip clip in ttsAudioClips)
        {
            if (clip != null)
                ttsClipsDic[clip.name] = clip;
        }
    }

    // ============== TTS ==============

    /// <summary>
    /// 사용 가능한 AudioSource 찾기 (재생 중이지 않은 것)
    /// </summary>
    private AudioSource GetAvailablePlayer()
    {
        if (ttsPlayers == null || ttsPlayers.Length == 0)
            return null;

        // 재생 중이지 않은 플레이어 찾기
        foreach (var player in ttsPlayers)
        {
            if (player != null && !player.isPlaying)
                return player;
        }

        // 모두 재생 중이면 첫 번째 것 반환
        return ttsPlayers[0];
    }

    /// <summary>
    /// 현재 재생 중인 AudioSource 찾기
    /// </summary>
    private AudioSource GetPlayingPlayer()
    {
        if (ttsPlayers == null) return null;

        foreach (var player in ttsPlayers)
        {
            if (player != null && player.isPlaying)
                return player;
        }
        return null;
    }

    /// <summary>
    /// TTS 토글 재생 (문제번호, 스텝번호)
    /// - 재생 중이면 중지
    /// - 재생 중이 아니면 해당 클립 재생
    /// </summary>
    public void ToggleTTS(int problemNum, int stepNum)
    {
        string clipName = $"P{problemNum}_S{stepNum}";
        ToggleTTS(clipName);
    }

    /// <summary>
    /// TTS 토글 재생 (클립 이름으로)
    /// </summary>
    public void ToggleTTS(string clipName)
    {
        if (ttsPlayers == null || ttsPlayers.Length == 0)
        {
            Debug.LogWarning("[SoundManager] ttsPlayers가 할당되지 않았습니다");
            return;
        }

        // 재생 중인 플레이어 확인
        var playingPlayer = GetPlayingPlayer();
        if (playingPlayer != null)
        {
            Debug.Log($"[SoundManager] TTS 강제 중지: {playingPlayer.clip?.name}");
            playingPlayer.Stop();
            return;
        }

        // 클립 찾기
        if (!ttsClipsDic.ContainsKey(clipName))
        {
            Debug.LogWarning($"[SoundManager] TTS 클립을 찾을 수 없음: {clipName}");
            return;
        }

        // 사용 가능한 플레이어로 재생
        var player = GetAvailablePlayer();
        if (player == null)
        {
            Debug.LogWarning("[SoundManager] 사용 가능한 ttsPlayer가 없습니다");
            return;
        }

        player.clip = ttsClipsDic[clipName];
        player.Play();
        Debug.Log($"[SoundManager] TTS 재생 시작: {clipName}");
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
            {
                player.Stop();
            }
        }
        Debug.Log("[SoundManager] TTS 중지");
    }

    /// <summary>
    /// TTS 무조건 재생 (자동 재생용)
    /// - 기존 재생 중이면 중지 후 새로 재생
    /// </summary>
    public void PlayTTS(int problemNum, int stepNum)
    {
        string clipName = $"P{problemNum}_S{stepNum}";
        PlayTTS(clipName);
    }

    /// <summary>
    /// TTS 무조건 재생 (클립 이름으로)
    /// </summary>
    public void PlayTTS(string clipName)
    {
        if (ttsPlayers == null || ttsPlayers.Length == 0)
        {
            Debug.LogWarning("[SoundManager] ttsPlayers가 할당되지 않았습니다");
            return;
        }

        // 클립 찾기
        if (!ttsClipsDic.ContainsKey(clipName))
        {
            Debug.LogWarning($"[SoundManager] TTS 클립을 찾을 수 없음: {clipName}");
            return;
        }

        // 기존 재생 중인 것 모두 중지
        foreach (var p in ttsPlayers)
        {
            if (p != null && p.isPlaying)
                p.Stop();
        }

        // 사용 가능한 플레이어로 재생
        var player = GetAvailablePlayer();
        if (player == null)
        {
            Debug.LogWarning("[SoundManager] 사용 가능한 ttsPlayer가 없습니다");
            return;
        }

        player.clip = ttsClipsDic[clipName];
        player.Play();
        Debug.Log($"[SoundManager] TTS 재생: {clipName}");
    }
}
