/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SoundManager>();
            }
            return instance;
        }
    }

    [SerializeField] AudioSource bgmPlayer;
    [SerializeField] AudioSource[] sfxPlayers;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip TitleSceneBGM;
    [SerializeField] private AudioClip GameSceneBGM;
    [SerializeField] private AudioClip CreditSceneBGM;


    [Header("SFX Clips")]
    [SerializeField] private AudioClip[] sfxAudioClips;

    private Dictionary<string, AudioClip> audioClipsDic = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioSource> playingAudios = new Dictionary<string, AudioSource>();

    private bool isPause = false;

    private void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        foreach (AudioClip clip in sfxAudioClips)
        {
            audioClipsDic[clip.name] = clip;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMByScene(scene.name);
        UpdateBGMVolume();
    }

    // 씬 이름에 따라 BGM 변경
    private void PlayBGMByScene(string sceneName)
    {
        if (bgmPlayer == null)
            return;

        AudioClip clipToPlay = null;

        switch (sceneName)
        {
            case "Title":
                clipToPlay = TitleSceneBGM;
                break;
            case "GameScene":
                clipToPlay = GameSceneBGM;
                break;


            case "EndingCreditScene":
                clipToPlay = CreditSceneBGM;
                break;
        }

        if (bgmPlayer.clip != clipToPlay)
        {
            bgmPlayer.clip = clipToPlay;
            bgmPlayer.loop = true;
            bgmPlayer.volume = DataManager.Instance.soundValue;
            bgmPlayer.Play();
        }
    }

    // --- SFX ---

    public void PlaySFXSound(string name)
    {
        float volume = PlayerPrefs.GetFloat("SoundVolume");
        if (!audioClipsDic.ContainsKey(name))
            return;

        if (!isPause)
        {
            GameObject sfxObj = new GameObject($"SFX_{name}");
            sfxObj.transform.SetParent(this.transform);

            AudioSource audioSource = sfxObj.AddComponent<AudioSource>();
            audioSource.clip = audioClipsDic[name];
            audioSource.volume = volume;
            audioSource.Play();

            playingAudios[name] = audioSource;

            Destroy(sfxObj, audioClipsDic[name].length);
        }
    }

    public void PlaySFXSound(string name, float customVolume)
    {
        if (!audioClipsDic.ContainsKey(name))
            return;

        if (!isPause)
        {
            GameObject sfxObj = new GameObject($"SFX_{name}");
            sfxObj.transform.SetParent(this.transform);

            AudioSource audioSource = sfxObj.AddComponent<AudioSource>();
            audioSource.clip = audioClipsDic[name];
            audioSource.volume = customVolume; // 여기만 다름!
            audioSource.Play();

            playingAudios[name] = audioSource;

            Destroy(sfxObj, audioClipsDic[name].length);
        }
    }

    public void StopAudioClip(string name)
    {
        if (playingAudios.ContainsKey(name))
        {
            AudioSource audioSource = playingAudios[name];
            audioSource.Stop();
            Destroy(audioSource.gameObject);
            playingAudios.Remove(name);
        }
    }

    // --- BGM 제어 ---

    public void PlayBGM()
    {
        if (bgmPlayer != null)
        {
            bgmPlayer.Play();
            isPause = false;
        }
    }

    public void PauseBGM()
    {
        if (bgmPlayer != null)
        {
            bgmPlayer.Pause();
            isPause = true;
        }
    }

    public void UpdateBGMVolume()
    {
        float volume = PlayerPrefs.GetFloat("SoundVolume", 0f); // 없으면 1로
        bgmPlayer.volume = volume;
        for (int e = 0; e < sfxPlayers.Length; ++e)
            sfxPlayers[e].volume = volume;
    }
}*/