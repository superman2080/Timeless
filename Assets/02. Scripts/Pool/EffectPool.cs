using System.Collections.Generic;
using UnityEngine;

// 타입별 파티클 이펙트 풀. InteractionObjectPool과 같은 구조 (Pool<PooledEffect>.Instance as EffectPool)
public class EffectPool : Pool<PooledEffect>
{
    [SerializeField] private EffectDataSO datas;
    [SerializeField] private EffectType defaultEffectType = EffectType.NONE;
    public override GameObject Prefab => GetPrefabByType(defaultEffectType);

    [System.Serializable]
    public struct PrewarmData
    {
        public EffectType type;
        [Min(0)] public int count;
    }

    private const int DefaultPrewarmCount = 3;
    [Tooltip("타입별 미리 생성 개수. 목록에 없는 타입은 3개")]
    [SerializeField] private List<PrewarmData> prewarm = new();

    private Dictionary<EffectType, GameObject> prefabDictionary;
    // 타입별 인스턴스 목록. Take가 해당 타입만 순회하도록 (풀 전체 스캔/할당 방지)
    private readonly Dictionary<EffectType, List<PooledEffect>> instancesByType = new();

    protected override void Awake()
    {
        base.Awake();
        InitializeEffectDict();
        foreach (var item in Items)
            GetInstanceList(item.effectType).Add(item);
    }

    private void Start()
    {
        foreach (var type in prefabDictionary.Keys)
        {
            int count = DefaultPrewarmCount;
            foreach (var data in prewarm)
            {
                if (data.type == type)
                    count = data.count;
            }

            var list = GetInstanceList(type);
            for (int i = list.Count; i < count; i++)
                CreateInactive(type, prefabDictionary[type]);
        }
    }

    private List<PooledEffect> GetInstanceList(EffectType type)
    {
        if (!instancesByType.TryGetValue(type, out var list))
        {
            list = new List<PooledEffect>();
            instancesByType.Add(type, list);
        }
        return list;
    }

    private PooledEffect CreateInactive(EffectType type, GameObject prefab)
    {
        var instance = Create(prefab);
        instance.effectType = type;
        instance.gameObject.SetActive(false);
        GetInstanceList(type).Add(instance);
        return instance;
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

        foreach (var child in GetInstanceList(effectType))
        {
            if (child != null && !child.gameObject.activeSelf)
                return child;
        }

        return CreateInactive(effectType, prefab);
    }
}
