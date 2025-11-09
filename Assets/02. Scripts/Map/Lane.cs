using System.Collections.Generic;
using UnityEngine;



public class Lane : MonoBehaviour
{
    public int LaneIndex { get; private set; }

    public float LaneX => transform.position.x;

    public void GenerateObstacle(ObjectType type)
    {
        var obj = (Pool<InteractionObject>.Instance as InteractionObjectPool).Get(type, transform.position, Quaternion.identity);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.back * 1000);
    }

#if UNITY_EDITOR

    [ContextMenu("Create Object")]

    private void CreateObject()
    {
        GenerateObstacle(ObjectType.ROBOT_ARM);
    }
#endif
}
