using System.Collections.Generic;
using UnityEngine;

// 타입별 파티클 이펙트 풀. InteractionObjectPool과 같은 구조 (Pool<PooledEffect>.Instance as EffectPool)
public class EffectPool : Pool<PooledEffect>
{
    [SerializeField] private EffectDataSO datas;
    [SerializeField] private EffectType defaultEffectType = EffectType.NONE;
    public override GameObject Prefab => GetPrefabByType(defaultEffectType);

    private Dictionary<EffectType, GameObject> prefabDictionary;

    protected override void Awake()
    {
        base.Awake();
        InitializeEffectDict();
    }

    private void InitializeEffectDict()
    {
        prefabDictionary = new Dictionary<EffectType, GameObject>();
        if (datas == null)
        {
            Debug.LogWarning("EffectPool에 EffectDataSO가 연결되지 않았습니다. 씬에 EffectPool을 배치하고 데이터를 연결하세요.");
            return;
        }

        foreach (var data in datas.effectDatas)
        {
            if (data.prefab != null && !prefabDictionary.ContainsKey(data.type))
            {
                prefabDictionary.Add(data.type, data.prefab);
            }
        }
    }

    public GameObject GetPrefabByType(EffectType effectType)
    {
        if (prefabDictionary != null && prefabDictionary.TryGetValue(effectType, out var prefab))
            return prefab;

        if (datas == null)
            return null;

        // Dictionary가 초기화되지 않았을 때 직접 찾기
        foreach (var data in datas.effectDatas)
        {
            if (data.type == effectType && data.prefab != null)
                return data.prefab;
        }

        return null;
    }

    public PooledEffect Get(EffectType effectType, Vector3 position) => Get(effectType, position, Quaternion.identity);

    public PooledEffect Get(EffectType effectType, Vector3 position, Quaternion rotation)
    {
        var effect = Take(effectType);
        if (effect == null)
            return null;

        effect.transform.SetPositionAndRotation(position, rotation);
        effect.gameObject.SetActive(true);
        return effect;
    }

    // 비활성 상태로 꺼내 온다. 활성화는 위치를 정한 뒤에 해야 한다 (PooledEffect가 OnEnable에서 재생을 시작하므로)
    private PooledEffect Take(EffectType effectType)
    {
        if (effectType == EffectType.NONE)
            return null;

        var prefab = GetPrefabByType(effectType);
        if (prefab == null)
        {
            Debug.LogWarning($"EffectType {effectType}에 대한 prefab이 등록되지 않았습니다.");
            return null;
        }

        foreach (var child in GetChildList(true))
        {
            if (!child.gameObject.activeSelf && child.effectType == effectType)
                return child;
        }

        var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, pool).GetComponent<PooledEffect>();
        instance.effectType = effectType;
        instance.gameObject.SetActive(false);
        return instance;
    }
}
