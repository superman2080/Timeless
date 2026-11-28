using UnityEngine;
using UnityEngine.Audio;

// 모든 오디오 재생을 담당하는 AudioSource 풀 (Pool<PooledAudio>.Instance as AudioPool)
// 효과음은 SfxType으로 요청하면 AudioDataSO에서 클립을 찾아 재생한다. 볼륨과 BGM 순서는 SoundManager 담당
public class AudioPool : Pool<PooledAudio>
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private AudioDataSO data;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Min(0)] [SerializeField] private int prewarmCount = 8;
    [Tooltip("동시 재생 최대 개수. 넘으면 가장 오래된 one-shot을 빼앗아 재사용")]
    [Min(1)] [SerializeField] private int maxCount = 24;

    public override GameObject Prefab => prefab;
    public AudioDataSO Data => data;
    // PauseAll 이후 새로 재생되는 소스도 일시정지 상태로 시작
    public bool IsPaused { get; private set; }

    protected override void Awake()
    {
        dontDestroyOnLoad = false; // SoundManager 오브젝트에 붙으므로 루트가 DDOL로 끌려가지 않도록
        base.Awake();
    }

    private void Start()
    {
        if (prefab != null)
        {
            for (int i = Items.Count; i < Mathf.Min(prewarmCount, maxCount); i++)
                Create(prefab).gameObject.SetActive(false);
        }

        if (data == null)
            return;

        // 클립들이 preloadAudioData=false라 시작 시 전부 로드해 첫 재생 지연을 줄인다
        foreach (var sfx in data.sfx)
            if (sfx.clip != null)
                sfx.clip.LoadAudioData();
        if (data.bgmStart != null)
            data.bgmStart.LoadAudioData();
        foreach (var clip in data.bgmCycle)
            if (clip != null)
                clip.LoadAudioData();
    }

    public PooledAudio PlaySFX(SfxType type)
        => TryGetSfx(type, out var sfx) ? Play(sfx.clip, sfxGroup, sfx.volume) : null;

    public PooledAudio PlayBGM(AudioClip clip, float volume = 1f) => Play(clip, bgmGroup, volume, true);

    public PooledAudio Play(AudioClip clip, AudioMixerGroup group, float volume = 1f, bool loop = false)
    {
        if (clip == null)
            return null;

        var audio = Take();
        if (audio == null)
            return null;

        audio.gameObject.SetActive(true);
        audio.Play(clip, group, volume, loop, IsPaused);
        return audio;
    }

    public bool TryGetSfx(SfxType type, out SfxData result)
    {
        result = default;
        if (type == SfxType.NONE || data == null || data.sfx == null)
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

    // 비활성 상태로 꺼내 온다
    private PooledAudio Take()
    {
        PooledAudio oldest = null;
        foreach (var item in Items)
        {
            if (item == null)
                continue;
            if (!item.gameObject.activeSelf)
                return item;
            if (!item.IsLooping && (oldest == null || item.StartTime < oldest.StartTime))
                oldest = item;
        }

        if (Items.Count < maxCount)
        {
            if (prefab == null)
            {
                Debug.LogWarning("AudioPool에 PooledAudio 프리팹이 연결되지 않았습니다.");
                return null;
            }
            var created = Create(prefab);
            created.gameObject.SetActive(false);
            return created;
        }

        if (oldest == null)
        {
            Debug.LogWarning($"AudioPool: 동시 재생 한도({maxCount})를 loop 사운드가 모두 쓰고 있어 재생하지 않습니다.");
            return null;
        }

        // 빼앗기: 반환(OnDisable 초기화) 후 다시 사용
        oldest.Stop();
        return oldest;
    }

    public void PauseAll()
    {
        IsPaused = true;
        foreach (var item in Items)
            if (item != null && item.gameObject.activeSelf)
                item.Pause();
    }

    public void ResumeAll()
    {
        IsPaused = false;
        foreach (var item in Items)
            if (item != null && item.gameObject.activeSelf)
                item.Resume();
    }

    public void StopAll()
    {
        foreach (var item in Items)
            if (item != null && item.gameObject.activeSelf)
                item.Stop();
    }
}
