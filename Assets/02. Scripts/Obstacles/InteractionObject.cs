using System.Collections.Generic;
using System;
using UnityEngine;

public enum ObjectType
{
    NONE,

    LASER,      // ∑π¿Ã¿˙
    BARREL,     // πË∑≤
    BLOCKADE,   // ∫Ì∑œ
    FAN,        // ∆“
    ROBOT_ARM,  // ∑Œ∫ø∆»

    HP,
    KEY_CARD,
}

[Serializable]
public struct ObjectData
{
    public ObjectType type;
    public GameObject prefab;
}

[RequireComponent(typeof(Collider))]
public abstract class InteractionObject : MonoBehaviour, ICollisionable
{
    public int currentLane;
    public ObjectData objectData;
    public Action<ICollisionable> onTargetHitEvent;
    public Action<ICollisionable> onTakeHitEvent;

    private List<ICollisionable> collisionables = new();
    public Collider Col { get; private set; }

    private void Start()
    {
        Col = gameObject.GetComponent<Collider>();
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
