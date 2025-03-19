using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource musicSource;

    public void SetMusicVoiume(float volume)
    {
        musicSource.volume = volume;
    }
}
