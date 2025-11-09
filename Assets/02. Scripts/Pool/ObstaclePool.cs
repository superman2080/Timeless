using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObstacleType
{
    LASER,      // 레이저
    BARREL,     // 배럴
    BLOCKADE,   // 블록
    FAN,        // 팬
    ROBOT_ARM,  // 로봇팔
}

[Serializable]
public struct ObstacleData
{
    public ObstacleType type;
    public GameObject prefab;
}

public class ObstaclePool : Pool<Obstacle>
{
    [SerializeField] private ObstacleData[] obstacleDatas;
    [SerializeField] private ObstacleType defaultParticleType = ObstacleType.LASER;
    public override GameObject Prefab => GetPrefabByType(defaultParticleType);

    private Dictionary<ObstacleType, GameObject> prefabDictionary;

    protected override void Awake()
    {
        base.Awake();
        InitializeObstacleDict();
    }

    private void InitializeObstacleDict()
    {
        prefabDictionary = new Dictionary<ObstacleType, GameObject>();
        foreach (var data in obstacleDatas)
        {
            if (data.prefab != null && !prefabDictionary.ContainsKey(data.type))
            {
                prefabDictionary.Add(data.type, data.prefab);
            }
        }
    }

    public GameObject GetPrefabByType(ObstacleType obstacleType)
    {
        if (prefabDictionary != null && prefabDictionary.ContainsKey(obstacleType))
        {
            return prefabDictionary[obstacleType];
        }

        // Dictionary가 초기화되지 않았을 때 직접 찾기
        foreach (var data in obstacleDatas)
        {
            if (data.type == obstacleType && data.prefab != null)
            {
                return data.prefab;
            }
        }

        return null;
    }

    public Obstacle Get(ObstacleType obstacleType)
    {
        if (!prefabDictionary.ContainsKey(obstacleType))
        {
            Debug.LogWarning($"ParticleType {obstacleType}에 대한 prefab이 설정되지 않았습니다.");
            return null;
        }

        // 해당 타입의 비활성화된 파티클 찾기
        foreach (var child in GetChildList(true))
        {
            if (!child.gameObject.activeSelf && child.obstacleType == obstacleType)
            {
                child.gameObject.SetActive(true);
                return child;
            }
        }

        // 없으면 새로 생성
        var prefab = prefabDictionary[obstacleType];
        var newParticle = Instantiate(prefab, Vector3.zero, Quaternion.identity, pool).GetComponent<Obstacle>();
        newParticle.obstacleType = obstacleType;

        return newParticle;
    }

    public Obstacle Get(ObstacleType obstacleType, Vector3 position)
    {
        return Get(obstacleType, position, Quaternion.identity);
    }

    public Obstacle Get(ObstacleType obstacleType, Vector3 position, Quaternion rotation)
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
