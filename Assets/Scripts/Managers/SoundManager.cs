using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Zenject;

// 통합 사운드 매니저: BGM과 SFX 모두 관리
public class SoundManager : MonoBehaviour, ISoundManager, IInitializable
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("BGM Clips (optional)")]
    [SerializeField] private AudioClip[] bgmClips;

    [Header("SFX Clips (optional)")]
    [SerializeField] private AudioClip[] sfxClips;

    [Header("Scene‑Specific BGM Settings")]
    [Tooltip("메인 메뉴 씬에서 재생할 BGM 인덱스")]
    [SerializeField] private int mainMenuBgm = 0;
    [Tooltip("로비 씬에서 재생할 BGM 인덱스")]
    [SerializeField] private int lobbyBgm = 1;
    [Tooltip("스테이지 씬에서 랜덤 재생할 BGM 인덱스 목록")]
    [SerializeField] private int[] stageBgmIndices;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    private Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

    /// <summary>
    /// SoundManager 초기화 (ISoundManager.Init)
    /// </summary>
    public void Init()
    {
        Debug.Log("SoundManager: Init");
    }

    /// <summary>
    /// Zenject에서 자동으로 호출됨.
    /// Initialize()는 “주입 완료 후 호출”
    /// </summary>
    public void Initialize()
    {
        Debug.Log("SoundManager: Initialize");
        Init();
    }

    // 이 로직은 [Inject] 필드가 아니라,
    // 에디터에서 할당된 AudioMixer, AudioClip[] 등을 직접 사용.
    void Awake()
    {
        Debug.Log("SoundManager: Awake");
        // AudioSource 세팅
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.outputAudioMixerGroup =
            audioMixer.FindMatchingGroups("BackGround Music")[0];

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.outputAudioMixerGroup =
            audioMixer.FindMatchingGroups("Effect Sound Group")[0];

        // 씬 전환 감지
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 볼륨 설정 (PlayerPrefs)
        float vol = PlayerPrefs.HasKey("MusicVol")
                  ? PlayerPrefs.GetFloat("MusicVol")
                  : 0.5f;
        SetBgmVolume(vol);

        vol = PlayerPrefs.HasKey("SFXVol")
            ? PlayerPrefs.GetFloat("SFXVol")
            : 0.5f;
        SetSfxVolume(vol);
    }

    void OnDestroy()
    {
        // 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // AudioSource 정리
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
            _bgmSource = null;
        }
        
        if (_sfxSource != null)
        {
            _sfxSource.Stop();
            _sfxSource = null;
        }
        
        // Dictionary 정리
        if (_clipCache != null)
        {
            _clipCache.Clear();
            _clipCache = null;
        }
        
        // 배열 참조 해제
        bgmClips = null;
        sfxClips = null;
        stageBgmIndices = null;
        
        // AudioMixer 참조 해제
        audioMixer = null;
    }

    // 씬이 바뀔 때마다 자동 호출
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Stage"))
        {
            // 스테이지 씬이면 랜덤 BGM
            PlayRandomStageBGM();
            return;
        }

        // 그 외는 enum 파싱
        if (System.Enum.TryParse<Define.Scene>(scene.name, out var s))
        {
            switch (s)
            {
                case Define.Scene.Main:
                    PlayBGM(mainMenuBgm);
                    break;
                case Define.Scene.Lobby:
                    PlayBGM(lobbyBgm);
                    break;
                case Define.Scene.SelectCharacter:
                    PlayBGM(mainMenuBgm);
                    break;
            }
        }
    }

    void PlayRandomStageBGM()
    {
        if (stageBgmIndices.Length == 0) return;
        int idx = stageBgmIndices[Random.Range(0, stageBgmIndices.Length)];
        PlayBGM(idx);
    }

    // 인덱스 기반 BGM
    public void PlayBGM(int index)
    {
        Debug.Log($"SoundManager: PlayBGM 호출 - index: {index}");
        if (index < 0 || index >= bgmClips.Length) return;
        _bgmSource.clip = bgmClips[index];
        _bgmSource.Play();
    }

    // 경로 기반 BGM (optional)
    public void PlayBGM(string path)
    {
        Debug.Log($"SoundManager: PlayBGM 호출 - path: {path}");
        var clip = LoadClip(path);
        if (clip != null)
        {
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }
    }

    public void StopBGM() => _bgmSource.Stop();

    // 인덱스 기반 SFX
    public void PlaySFX(int index)
    {
        if (index < 0 || index >= sfxClips.Length) return;
        _sfxSource.PlayOneShot(sfxClips[index]);
    }

    // 경로 기반 SFX (optional)
    public void PlaySFX(string path)
    {
        var clip = LoadClip(path);
        if (clip != null)
            _sfxSource.PlayOneShot(clip);
    }

    // 슬라이더 등으로 볼륨 조절
    public void SetBgmVolume(float v)
    {
        audioMixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Clamp(v, .0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("MusicVol", v);
    }
    public void SetSfxVolume(float v)
    {
        audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Clamp(v, .0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("SFXVol", v);
    }

    private AudioClip LoadClip(string path)
    {
        if (!_clipCache.TryGetValue(path, out var clip))
        {
            clip = Resources.Load<AudioClip>($"Sounds/{path}");
            if (clip != null) _clipCache[path] = clip;
            else Debug.LogWarning($"SoundManager: clip not found at Sounds/{path}");
        }
        return clip;
    }

    public void Clear()
    {
        _bgmSource.Stop();
        _sfxSource.Stop();
        _clipCache.Clear();
    }
}
