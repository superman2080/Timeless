using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;



public class Lane : MonoBehaviour
{
    public int LaneIndex { get; private set; }

    public float LaneX => transform.position.x;

    public List<InteractionObject> laneObjectList = new();
    [HideInInspector] public List<InteractionObject> currentLaneObjects = new();

    public void GenerateObject(InteractionObject obj)
    {
        
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.back * 1000);
    }
}
