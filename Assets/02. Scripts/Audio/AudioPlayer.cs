using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPlayer : MonoBehaviour
{
    public AudioClip[] audioClips;
    public float fadeOut = 1f;
    public float fadeIn = 1f;
    public bool trigger = false;
    
    private AudioSource audioSource;
    public int clipNumber = 2;
    private bool loop = true;
    void Start()
    {
        
        audioSource = GetComponent<AudioSource>();
        // 오디오소스 다 들어있는지 확인
        for (int i = 0; i < audioClips.Length; i++)
        {
            if (audioClips[i] == null)
            {
                loop = false;
                Debug.Log("음악이 없는 배열이 있습니다.");
            }
        }
        //오디오 소스가 다 있으면 시작
        if (loop)
        {
            audioSource.clip = audioClips[1];
            audioSource.Play();
            StartCoroutine(PlayMusic());
        }
    }

    IEnumerator PlayMusic()
    {
            while (true)
        {
            //트리거가 true면 페이드인/아웃후 다음음악 재생
            yield return new WaitUntil(() => trigger);
            
            trigger = false;
            
            StartCoroutine(FadeOutIn(audioSource, fadeOut, fadeIn, audioClips, clipNumber));
            yield return new WaitForSeconds(fadeOut + fadeIn);
            clipNumber++;

            //다음 음악이 없으면 루프 종료
            if (clipNumber > audioClips.Length-1)
                clipNumber = 2;
        }
        
    }

    public static IEnumerator FadeOutIn(AudioSource audioSource, float fadeIn, float fadeout, AudioClip[] audioClips, int i)
    {
        float startVolume = audioSource.volume;
        audioSource.PlayOneShot(audioClips[0], 0.7f);
        // 볼륨이 0보다 큰 동안 반복
        while (audioSource.volume > 0)
        {
            // 볼륨을 점진적으로 감소 (Time.deltaTime을 이용해 프레임과 무관하게 작동)
            audioSource.volume -= startVolume * Time.deltaTime / fadeout;
            yield return null; // 다음 프레임까지 대기
        }

        audioSource.Stop(); // 재생 종료
        //audioSource.volume = startVolume; // 원래 볼륨으로 복원 (다음 재생을 위해)

        while (audioSource.volume < 1)
        {
            // 볼륨을 점진적으로 증가
            audioSource.volume += Time.deltaTime / fadeIn;
            yield return null; // 다음 프레임까지 대기
        }
        audioSource.volume = 1f; // 혹시 1에 도달하지 못했을 경우를 대비해 최종적으로 1로 설정

        audioSource.clip = audioClips[i]; // 새로운 오디오 클립 설정
        audioSource.Play(); // 재생 재개
    }

}
