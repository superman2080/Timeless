using System.Collections.Generic;
using UnityEngine;

public class Lane : MonoBehaviour
{
    public int laneIndex;

    public float laneX => transform.position.x;

    public List<InteractionObject> laneObjectList = new();

    public void GenerateObject()
    {

    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.back * 1000);
    }
}
