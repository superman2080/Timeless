using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// 풀링되는 AudioSource 하나. 클립 로드를 기다렸다가 재생하고, loop가 아니면 끝난 뒤 스스로 비활성화되어 풀로 돌아간다
// loop 핸들은 자동 반환되지 않으므로 소유자가 Stop()해야 한다
[RequireComponent(typeof(AudioSource))]
public class PooledAudio : MonoBehaviour
{
    private AudioSource source;
    public AudioSource Source => source != null ? source : source = GetComponent<AudioSource>();

    // 소스가 모자랄 때 가장 오래된 one-shot을 고르는 기준 (unscaled)
    public float StartTime { get; private set; }
    public bool IsLooping => Source.loop;
    public bool IsPaused { get; private set; }

    private Coroutine playCor;

    // 활성화된 상태에서 호출해야 한다 (AudioPool.Play가 담당)
    public void Play(AudioClip clip, AudioMixerGroup group, float volume, bool loop, bool paused)
    {
        Source.clip = clip;
        Source.outputAudioMixerGroup = group;
        Source.volume = volume;
        Source.loop = loop;
        IsPaused = paused;
        StartTime = Time.unscaledTime;

        if (playCor != null)
            StopCoroutine(playCor);
        playCor = StartCoroutine(PlayCoroutine(clip));
    }

    // 매 프레임 확인 방식이라 timeScale = 0에서도 멈추지 않는다
    private IEnumerator PlayCoroutine(AudioClip clip)
    {
        // 클립들이 preloadAudioData=false라 로드 전 Play는 무음이 됨
        clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading || clip.loadState == AudioDataLoadState.Unloaded)
            yield return null;

        Source.Play();
        if (IsPaused)
            Source.Pause();

        if (Source.loop)
        {
            playCor = null;
            yield break;
        }

        // 일시정지 중에는 isPlaying이 false가 되므로 반환 판정에서 제외
        while (IsPaused || Source.isPlaying)
            yield return null;

        playCor = null;
        gameObject.SetActive(false);
    }

    public void Pause()
    {
        IsPaused = true;
        Source.Pause();
    }

    public void Resume()
    {
        IsPaused = false;
        Source.UnPause();
    }

    // 풀에 반환
    public void Stop() => gameObject.SetActive(false);

    private void OnDisable()
    {
        if (playCor != null)
        {
            StopCoroutine(playCor);
            playCor = null;
        }
        Source.Stop();
        Source.clip = null;
        Source.loop = false;
        Source.volume = 1f;
        Source.pitch = 1f;
        IsPaused = false;
    }
}
