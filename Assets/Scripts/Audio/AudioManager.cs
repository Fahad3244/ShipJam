using UnityEngine;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Settings")]
    [SerializeField] private bool debugLogs = true;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Content")]
    public Sound[] musicTracks;
    public Sound[] sfxClips;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadSettings()
    {
        // PlayerPrefs stores 1 for Muted, 0 for Unmuted
        musicSource.mute = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        sfxSource.mute = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
    }

    // --- PLAYBACK ---

    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musicTracks, x => x.name == name);
        if (s == null) { if(debugLogs) Debug.LogWarning("Music: " + name + " not found!"); return; }
        
        musicSource.clip = s.clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxClips, x => x.name == name);
        if (s == null) { if(debugLogs) Debug.LogWarning("SFX: " + name + " not found!"); return; }
        
        sfxSource.PlayOneShot(s.clip);
    }

    // --- CONTROLS & SAVING ---

    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
        PlayerPrefs.SetInt("MusicMuted", musicSource.mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSFX()
    {
        sfxSource.mute = !sfxSource.mute;
        PlayerPrefs.SetInt("SFXMuted", sfxSource.mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsMusicMuted() => musicSource.mute;
    public bool IsSFXMuted() => sfxSource.mute;
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}