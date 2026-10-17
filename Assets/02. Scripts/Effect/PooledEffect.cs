using System.Collections;
using UnityEngine;

// 풀링되는 1회성 파티클 이펙트. 활성화되면 재생하고, 수명이 지나면 스스로 비활성화되어 풀로 돌아간다
public class PooledEffect : MonoBehaviour
{
    public EffectType effectType;

    [Tooltip("재생 시간(초). 0이면 파티클 duration + 최대 startLifetime으로 자동 계산")]
    [SerializeField] private float lifetime = 0f;

    private ParticleSystem[] systems;
    private Coroutine disableCor;

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        if (lifetime <= 0f)
            lifetime = CalculateLifetime();
    }

    private float CalculateLifetime()
    {
        float longest = 1f;
        foreach (var system in systems)
        {
            var main = system.main;
            longest = Mathf.Max(longest, main.duration + main.startLifetime.constantMax);
        }
        return longest;
    }

    private void OnEnable()
    {
        foreach (var system in systems)
        {
            system.Clear(true);
            system.Play(true);
        }
        disableCor = StartCoroutine(DisableCoroutine());
    }

    private void OnDisable()
    {
        if (disableCor != null)
        {
            StopCoroutine(disableCor);
            disableCor = null;
        }
    }

    private IEnumerator DisableCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        disableCor = null;
        gameObject.SetActive(false);
    }
}
