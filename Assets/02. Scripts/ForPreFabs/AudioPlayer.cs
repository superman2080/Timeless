using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;

public class AudioPlayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AudioClip[] audioClips;
    public float fadeOut = 1f;
    public float fadeIn = 1f;
    public bool test = false;
    public bool test2 = false;
    private AudioSource audioSource;
    public int clipNumber = 1;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClips[0];
        audioSource.Play();
        StartCoroutine(PlayMusic());
    }

    IEnumerator PlayMusic()
    {
        while (true)
        {
            yield return new WaitUntil(() => test||test2);
            StartCoroutine(AudioHelper.FadeOutIn(audioSource, fadeOut, fadeIn, audioClips, clipNumber));
            yield return new WaitForSeconds(fadeOut + fadeIn);
            clipNumber++;
            if (clipNumber > audioClips.Length)
                break;
        }
    }

    // Update is called once per frame

}
