using UnityEngine;
using System.Collections;

public class AudioHelper : MonoBehaviour
{
    // 페이드 아웃 (볼륨 감소)
    public static IEnumerator FadeOutIn(AudioSource audioSource, float fadeIn, float fadeout, AudioClip[] audioClips, int i)
    {
        float startVolume = audioSource.volume;

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
    /*
    // 페이드 인 (볼륨 증가)
    public static IEnumerator FadeIn(AudioSource audioSource, float fadeTime)
    {
        audioSource.Play(); // 먼저 재생 시작
        audioSource.volume = 0f; // 초기 볼륨은 0으로 설정

        // 볼륨이 1보다 작은 동안 반복
        while (audioSource.volume < 1)
        {
            // 볼륨을 점진적으로 증가
            audioSource.volume += Time.deltaTime / fadeTime;
            yield return null; // 다음 프레임까지 대기
        }
        audioSource.volume = 1f; // 혹시 1에 도달하지 못했을 경우를 대비해 최종적으로 1로 설정
    }*/
}
