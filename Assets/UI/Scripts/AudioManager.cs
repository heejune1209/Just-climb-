using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public AudioMixer theMixer;
    public AudioClip[] clips;
    float defaultVolume = 0.5f;
    private AudioSource audioSource;
    private int currentClipIndex = 0;
    private int loopStart = 2;
    private int loopEnd = 6;
    public AudioClip[] sfxClips;
    private AudioSource sfxSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = theMixer.FindMatchingGroups("BackGround Music")[0];
        sfxSource = gameObject.AddComponent<AudioSource>();
        
    }

    public void PlayBGM(int index)
    {
        if (index < 0 || index >= clips.Length) return;

        audioSource.clip = clips[index];
        audioSource.Play();
        currentClipIndex = index;
    }

    public void StopBGM()
    {
        audioSource.Stop();
    }
    public void Destroy()
    {
        foreach (AudioSource sfxSource in sfxSources)
        {
            Destroy(sfxSource);
        }
        sfxSources.Clear();
    }

    private void Update()
    {
        if (!audioSource.isPlaying)
        {
            if (currentClipIndex == 0 || currentClipIndex == 1)
            {
                PlayBGM(currentClipIndex);
            }
            else if (currentClipIndex >= loopStart && currentClipIndex <= loopEnd)
            {
                currentClipIndex++;

                if (currentClipIndex > loopEnd)
                {
                    currentClipIndex = loopStart;
                }

                PlayBGM(currentClipIndex);
            }
        }
    }

    public void PlaySFX(int index)
    {
        if (index < 0 || index >= sfxClips.Length) return;

        AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.clip = sfxClips[index];
        sfxSource.outputAudioMixerGroup = theMixer.FindMatchingGroups("Effect Sound Group")[0];

        sfxSource.Play();

        Destroy(sfxSource, sfxClips[index].length);
        sfxSources.Add(sfxSource);
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("MusicVol"))
        {
            theMixer.SetFloat("MusicVol", PlayerPrefs.GetFloat("MusicVol"));
        }
        else
        {
            theMixer.SetFloat("MusicVol", defaultVolume);
        }

        if (PlayerPrefs.HasKey("SFXVol"))
        {
            theMixer.SetFloat("SFXVol", PlayerPrefs.GetFloat("SFXVol"));
        }
        else
        {
            theMixer.SetFloat("SFXVol", defaultVolume);
        }
    }
}
