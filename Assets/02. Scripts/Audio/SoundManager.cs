using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum VolumeChannel
{
    Master,
    BGM,
    SFX,
}

// 모든 사운드 재생/볼륨 관리. (파일명 AudioManager.cs는 Unity 내장 AudioManager와 충돌해 컴파일에서 제외되므로 SoundManager로 명명)
// 클립은 AudioDataSO에서 받고, 볼륨은 AudioMixer 그룹으로 조절한다
// (BGM 페이드는 AudioSource.volume을 쓰므로 믹서 볼륨과 충돌하지 않음)
public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioDataSO data;

    [Header("Volume (0~1, 플레이 중 변경 즉시 반영, PlayerPrefs 저장)")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("BGM Transition")]
    [SerializeField] private float fadeOut = 1f;
    [SerializeField] private float fadeIn = 1f;

    // 믹서에 노출된 파라미터 이름 (VolumeChannel 순서)
    private static readonly string[] MixerParams = { "MasterVolume", "BGMVolume", "SFXVolume" };
    private const string PrefsKey = "Volume.";

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private int cycleIndex;
    private Coroutine bgmCor;

    protected override void Awake()
    {
        dontDestroyOnLoad = false; // SampleScene 전용. 재로드 시 함께 재생성
        base.Awake();

        bgmSource = CreateSource("BGM", bgmGroup);
        bgmSource.loop = true;
        sfxSource = CreateSource("SFX", sfxGroup);
    }

    private AudioSource CreateSource(string name, AudioMixerGroup group)
    {
        var source = new GameObject(name).AddComponent<AudioSource>();
        source.transform.SetParent(transform, false);
        source.outputAudioMixerGroup = group;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D
        return source;
    }

    private void Start()
    {
        // 믹서 SetFloat은 Awake에서는 반영되지 않으므로 Start에서 적용
        for (int i = 0; i < MixerParams.Length; i++)
        {
            var channel = (VolumeChannel)i;
            SetVolume(channel, PlayerPrefs.GetFloat(PrefsKey + channel, GetVolume(channel)));
        }

        if (data == null)
            return;

        // 클립들이 preloadAudioData=false라 로드 전 Play/PlayOneShot은 무음이 됨 → 시작 시 전부 로드
        foreach (var sfx in data.sfx)
            if (sfx.clip != null)
                sfx.clip.LoadAudioData();
        foreach (var clip in data.bgmCycle)
            if (clip != null)
                clip.LoadAudioData();

        if (data.bgmStart != null)
            bgmCor = StartCoroutine(PlayWhenLoaded(data.bgmStart));
    }

    private IEnumerator PlayWhenLoaded(AudioClip clip)
    {
        bgmSource.clip = clip;
        clip.LoadAudioData();
        yield return new WaitUntil(() => clip.loadState != AudioDataLoadState.Loading && clip.loadState != AudioDataLoadState.Unloaded);
        bgmSource.Play();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying || mixer == null)
            return;
        for (int i = 0; i < MixerParams.Length; i++)
            ApplyToMixer((VolumeChannel)i);
    }

    #region Volume

    public float GetVolume(VolumeChannel channel) => channel switch
    {
        VolumeChannel.Master => masterVolume,
        VolumeChannel.BGM => bgmVolume,
        _ => sfxVolume,
    };

    public void SetVolume(VolumeChannel channel, float volume)
    {
        volume = Mathf.Clamp01(volume);
        switch (channel)
        {
            case VolumeChannel.Master: masterVolume = volume; break;
            case VolumeChannel.BGM: bgmVolume = volume; break;
            default: sfxVolume = volume; break;
        }
        ApplyToMixer(channel);
        PlayerPrefs.SetFloat(PrefsKey + channel, volume);
    }

    private void ApplyToMixer(VolumeChannel channel)
    {
        if (mixer == null)
            return;
        float v = GetVolume(channel);
        // 0~1 선형 → dB. 0이면 -80dB(무음)
        mixer.SetFloat(MixerParams[(int)channel], v > 0.0001f ? 20f * Mathf.Log10(v) : -80f);
    }

    #endregion

    #region SFX

    public void PlaySFX(SfxType type)
    {
        if (TryGetSfx(type, out var sfx))
            sfxSource.PlayOneShot(sfx.clip, sfx.volume);
    }

    private bool TryGetSfx(SfxType type, out SfxData result)
    {
        result = default;
        if (type == SfxType.NONE || data == null)
            return false;

        foreach (var sfx in data.sfx)
        {
            if (sfx.type == type && sfx.clip != null)
            {
                result = sfx;
                return true;
            }
        }
        Debug.LogWarning($"AudioData에 SfxType {type} 클립이 등록되지 않았습니다.");
        return false;
    }

    #endregion

    #region BGM

    // 2D/3D 전환 시 호출. 전환 효과음 → 현재 BGM 페이드아웃 → 다음 BGM 페이드인
    public void NextBgm()
    {
        if (data == null || data.bgmCycle == null || data.bgmCycle.Length == 0)
            return;

        if (bgmCor != null)
            StopCoroutine(bgmCor);
        bgmCor = StartCoroutine(NextBgmCoroutine(data.bgmCycle[cycleIndex]));
        cycleIndex = (cycleIndex + 1) % data.bgmCycle.Length;
    }

    private IEnumerator NextBgmCoroutine(AudioClip next)
    {
        // 전환 효과음은 BGM 소스에서 재생해 페이드아웃에 함께 묻히도록 (기존 동작)
        if (TryGetSfx(SfxType.BGM_TRANSITION, out var transition))
            bgmSource.PlayOneShot(transition.clip, transition.volume);

        float start = bgmSource.volume;
        for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(start, 0f, t / fadeOut);
            yield return null;
        }
        bgmSource.Stop();

        bgmSource.volume = 0f;
        yield return PlayWhenLoaded(next);
        for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, 1f, t / fadeIn);
            yield return null;
        }
        bgmSource.volume = 1f;
        bgmCor = null;
    }

    #endregion
}
