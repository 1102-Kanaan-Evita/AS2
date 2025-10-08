// Assets/Scripts/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    void Awake()
    {
        if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) { musicSource.Stop(); return; }
        musicSource.clip = clip; musicSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
