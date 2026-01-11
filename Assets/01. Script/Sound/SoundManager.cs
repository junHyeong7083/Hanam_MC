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
    [SerializeField] private AudioSource ttsPlayer;      // TTS 전용 AudioSource

    private Dictionary<string, AudioClip> ttsClipsDic = new Dictionary<string, AudioClip>();

    /// <summary>TTS 재생 상태</summary>
    public bool IsTTSPlaying => ttsPlayer != null && ttsPlayer.isPlaying;

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
        if (ttsPlayer == null)
        {
            Debug.LogWarning("[SoundManager] ttsPlayer가 할당되지 않았습니다");
            return;
        }

        // 재생 중이면 중지
        if (ttsPlayer.isPlaying)
        {
            Debug.Log($"[SoundManager] TTS 강제 중지: {ttsPlayer.clip?.name}");
            ttsPlayer.Stop();
            return;
        }

        // 클립 찾기
        if (!ttsClipsDic.ContainsKey(clipName))
        {
            Debug.LogWarning($"[SoundManager] TTS 클립을 찾을 수 없음: {clipName}");
            return;
        }

        // 재생
        ttsPlayer.clip = ttsClipsDic[clipName];
        ttsPlayer.Play();
        Debug.Log($"[SoundManager] TTS 재생 시작: {clipName}");
    }

    /// <summary>
    /// TTS 강제 중지
    /// </summary>
    public void StopTTS()
    {
        if (ttsPlayer != null && ttsPlayer.isPlaying)
        {
            ttsPlayer.Stop();
            Debug.Log("[SoundManager] TTS 중지");
        }
    }
}
