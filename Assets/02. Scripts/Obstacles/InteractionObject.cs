using System.Collections.Generic;
using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class InteractionObject : MonoBehaviour, ICollisionable
{
    public Action<ICollisionable> onTargetHitEvent;
    public Action<ICollisionable> onTakeHitEvent;

    private List<ICollisionable> collisionables;
    public Collider Col { get; private set; }


    private void Start()
    {
        Col = GetComponent<Collider>(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ICollisionable col) && collisionables.Contains(col) == false) 
        { 
            collisionables.Add(col);
            onTargetHitEvent?.Invoke(col);
            onTakeHitEvent?.Invoke(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent(out ICollisionable col) && collisionables.Contains(col))
        {
            collisionables.Remove(col);
        }
    }
}
