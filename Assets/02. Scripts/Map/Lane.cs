using System.Collections.Generic;
using UnityEngine;



public class Lane : MonoBehaviour
{
    [SerializeField] private bool isUpper;
    public bool IsUpper => isUpper;

    public int LaneIndex { get; set; }

    public float LaneX => transform.position.x;

    private List<InteractionObject> spawnedObjects = new List<InteractionObject>();

    public InteractionObject GenerateObstacle(ObjectType type) => GenerateObstacle(type, transform.position);

    public InteractionObject GenerateObstacle(ObjectType type, Vector3 position)
    {
        if (type == ObjectType.NONE)
            return null;

        var obj = (Pool<InteractionObject>.Instance as InteractionObjectPool).Get(type, position, Quaternion.identity);
        if (obj == null)
            return null;
        obj.currentLane = LaneIndex;
        obj.isUpper = isUpper;
        // 풀로 돌아간(비활성) 항목을 정리해 목록이 무한히 커지거나 재사용 오브젝트가 중복으로 쌓이지 않게 한다
        spawnedObjects.RemoveAll(o => o == null || !o.gameObject.activeSelf);
        spawnedObjects.Add(obj);
        return obj;
    }

    public void RemoveAllObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Pool<InteractionObject>.Instance.Return(obj);
            }
        }
        spawnedObjects.Clear();
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = isUpper ? Color.blue : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.back * 1000);
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
