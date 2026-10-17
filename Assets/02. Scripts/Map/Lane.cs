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
        obj.currentLane = LaneIndex;
        obj.isUpper = isUpper;
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
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = isUpper ? Color.blue : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.back * 1000);
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
