using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum VolumeChannel
{
    Master,
    BGM,
    SFX,
}

// 볼륨과 BGM 순서 관리. (파일명 AudioManager.cs는 Unity 내장 AudioManager와 충돌해 컴파일에서 제외되므로 SoundManager로 명명)
// 실제 재생은 전부 AudioPool이 담당한다. 효과음은 각 시스템이 AudioPool.PlaySFX로 직접 요청하고, BGM은 여기서 풀 핸들로 재생한다
// 볼륨은 AudioMixer 그룹으로 조절한다 (BGM 페이드는 AudioSource.volume을 쓰므로 믹서 볼륨과 충돌하지 않음)
public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioMixer mixer;

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

    private AudioPool pool;
    private PooledAudio bgm; // 현재 BGM 핸들 (loop라 직접 Stop해야 반환됨)
    private int cycleIndex;
    private Coroutine bgmCor;

    protected override void Awake()
    {
        dontDestroyOnLoad = false; // 씬 전용. 재로드 시 함께 재생성
        base.Awake();
    }

    private void Start()
    {
        // 믹서 SetFloat은 Awake에서는 반영되지 않으므로 Start에서 적용
        for (int i = 0; i < MixerParams.Length; i++)
        {
            var channel = (VolumeChannel)i;
            SetVolume(channel, PlayerPrefs.GetFloat(PrefsKey + channel, GetVolume(channel)));
        }

        pool = Pool<PooledAudio>.Instance as AudioPool;
        if (pool != null && pool.Data != null && pool.Data.bgmStart != null)
            bgm = pool.PlayBGM(pool.Data.bgmStart);
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

    #region BGM

    // 2D/3D 전환 시 호출. 전환 효과음 → 현재 BGM 페이드아웃 → 다음 BGM 페이드인
    public void NextBgm()
    {
        if (pool == null || pool.Data == null || pool.Data.bgmCycle == null || pool.Data.bgmCycle.Length == 0)
            return;

        if (bgmCor != null)
            StopCoroutine(bgmCor);
        bgmCor = StartCoroutine(NextBgmCoroutine(pool.Data.bgmCycle[cycleIndex]));
        cycleIndex = (cycleIndex + 1) % pool.Data.bgmCycle.Length;
    }

    private IEnumerator NextBgmCoroutine(AudioClip next)
    {
        if (bgm != null && bgm.gameObject.activeSelf)
        {
            // 전환 효과음은 BGM 소스에서 재생해 페이드아웃에 함께 묻히도록 (기존 동작)
            if (pool.TryGetSfx(SfxType.BGM_TRANSITION, out var transition))
                bgm.Source.PlayOneShot(transition.clip, transition.volume);

            float start = bgm.Source.volume;
            for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                bgm.Source.volume = Mathf.Lerp(start, 0f, t / fadeOut);
                yield return null;
            }
            bgm.Stop();
        }

        bgm = pool.PlayBGM(next, 0f);
        if (bgm == null)
        {
            bgmCor = null;
            yield break;
        }

        // 로드가 끝나 실제로 재생되기 시작한 뒤 페이드인
        while (bgm.gameObject.activeSelf && !bgm.Source.isPlaying && !bgm.IsPaused)
            yield return null;
        for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            bgm.Source.volume = Mathf.Lerp(0f, 1f, t / fadeIn);
            yield return null;
        }
        bgm.Source.volume = 1f;
        bgmCor = null;
    }

    #endregion
}
