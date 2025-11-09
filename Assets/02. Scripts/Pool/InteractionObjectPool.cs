using System;
using System.Collections.Generic;
using UnityEngine;




public class InteractionObjectPool : Pool<InteractionObject>
{
    [SerializeField] private ObjectDataSO datas;
    [SerializeField] private ObjectType defaultParticleType = ObjectType.NONE;
    public override GameObject Prefab => GetPrefabByType(defaultParticleType);

    private Dictionary<ObjectType, GameObject> prefabDictionary;

    protected override void Awake()
    {
        base.Awake();
        InitializeObstacleDict();
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
        if (!prefabDictionary.ContainsKey(objectType))
        {
            Debug.LogWarning($"ParticleType {objectType}에 대한 prefab이 설정되지 않았습니다.");
            return null;
        }

        // 해당 타입의 비활성화된 파티클 찾기
        foreach (var child in GetChildList(true))
        {
            if (!child.gameObject.activeSelf && child.objectData.type == objectType)
            {
                child.gameObject.SetActive(true);
                return child;
            }
        }

        // 없으면 새로 생성
        var prefab = prefabDictionary[objectType];
        var newPrefab = Instantiate(prefab, Vector3.zero, Quaternion.identity, pool).GetComponent<InteractionObject>();
        newPrefab.objectData.type = objectType;

        return newPrefab;
    }

    public InteractionObject Get(ObjectType obstacleType, Vector3 position)
    {
        return Get(obstacleType, position, Quaternion.identity);
    }

    public InteractionObject Get(ObjectType obstacleType, Vector3 position, Quaternion rotation)
    {
        var obstacle = Get(obstacleType);
        if (obstacle != null)
        {
            obstacle.transform.position = position;
            obstacle.transform.rotation = rotation;
        }
        return obstacle;
    }
}
