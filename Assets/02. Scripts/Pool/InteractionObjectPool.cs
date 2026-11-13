using System;
using System.Collections.Generic;
using UnityEngine;




public class InteractionObjectPool : Pool<InteractionObject>
{
    [SerializeField] private ObjectDataSO datas;
    [SerializeField] private ObjectType defaultParticleType = ObjectType.NONE;
    public override GameObject Prefab => GetPrefabByType(defaultParticleType);

    [Serializable]
    public struct PrewarmData
    {
        public ObjectType type;
        [Min(0)] public int count;
    }

    private const int DefaultPrewarmCount = 3;
    [Tooltip("타입별 미리 생성 개수. 목록에 없는 타입은 3개")]
    [SerializeField] private List<PrewarmData> prewarm = new();

    private Dictionary<ObjectType, GameObject> prefabDictionary;
    // 타입별 인스턴스 목록. Take가 해당 타입만 순회하도록 (풀 전체 스캔/할당 방지)
    private readonly Dictionary<ObjectType, List<InteractionObject>> instancesByType = new();

    protected override void Awake()
    {
        base.Awake();
        InitializeObstacleDict();
        foreach (var item in Items)
            GetInstanceList(item.objectData.type).Add(item);
    }

    // GameManager(-100) 등 다른 매니저 초기화 뒤에 생성해야 OnEnable 쪽 참조가 안전하다
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
                CreateInactive(type);
        }
    }

    private List<InteractionObject> GetInstanceList(ObjectType type)
    {
        if (!instancesByType.TryGetValue(type, out var list))
        {
            list = new List<InteractionObject>();
            instancesByType.Add(type, list);
        }
        return list;
    }

    private InteractionObject CreateInactive(ObjectType type)
    {
        var instance = Create(prefabDictionary[type]);
        instance.objectData.type = type;
        instance.gameObject.SetActive(false);
        GetInstanceList(type).Add(instance);
        return instance;
    }

    private void InitializeObstacleDict()
    {
        prefabDictionary = new Dictionary<ObjectType, GameObject>();
        foreach (var data in datas.objectDatas)
        {
            if (data.prefab != null && !prefabDictionary.ContainsKey(data.type))
            {
                prefabDictionary.Add(data.type, data.prefab.gameObject);
            }
        }
    }

    public GameObject GetPrefabByType(ObjectType obstacleType)
    {
        if (prefabDictionary != null && prefabDictionary.ContainsKey(obstacleType))
        {
            return prefabDictionary[obstacleType];
        }

        // Dictionary가 초기화되지 않았을 때 직접 찾기
        foreach (var data in datas.objectDatas)
        {
            if (data.type == obstacleType && data.prefab != null)
            {
                return data.prefab;
            }
        }

        return null;
    }

    public InteractionObject Get(ObjectType objectType)
    {
        return Get(objectType, Vector3.zero, Quaternion.identity);
    }

    public InteractionObject Get(ObjectType obstacleType, Vector3 position)
    {
        return Get(obstacleType, position, Quaternion.identity);
    }

    public InteractionObject Get(ObjectType obstacleType, Vector3 position, Quaternion rotation)
    {
        var obstacle = Take(obstacleType);
        if (obstacle == null)
            return null;

        obstacle.transform.SetPositionAndRotation(position, rotation);
        obstacle.gameObject.SetActive(true);
        return obstacle;
    }

    // 비활성 상태로 꺼내 온다. 활성화는 호출자가 위치를 세팅한 뒤에 해야 함
    // (활성화가 먼저면 OnEnable의 폐기 체크가 재사용 전 옛 위치에서 돌아 즉시 다시 비활성화됨)
    private InteractionObject Take(ObjectType objectType)
    {
        if (!prefabDictionary.ContainsKey(objectType))
        {
            Debug.LogWarning($"ParticleType {objectType}에 대한 prefab이 설정되지 않았습니다.");
            return null;
        }

        // 해당 타입의 비활성화된 오브젝트 찾기
        foreach (var child in GetInstanceList(objectType))
        {
            if (child != null && !child.gameObject.activeSelf)
                return child;
        }

        // 없으면 새로 생성 (프리팹이 활성이므로 곧바로 비활성으로 되돌린다)
        return CreateInactive(objectType);
    }
}
