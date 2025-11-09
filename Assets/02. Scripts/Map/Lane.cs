using System.Collections.Generic;
using UnityEngine;



public class Lane : MonoBehaviour
{
    public int LaneIndex { get; private set; }

    public float LaneX => transform.position.x;

    private List<InteractionObject> spawnedObjects = new List<InteractionObject>();

    public void GenerateObstacle(ObjectType type)
    {
        if (type == ObjectType.NONE)
            return;

        var obj = (Pool<InteractionObject>.Instance as InteractionObjectPool).Get(type, transform.position, Quaternion.identity);
        obj.currentLane = LaneIndex;
        spawnedObjects.Add(obj);
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
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.back * 1000);
    }
}
