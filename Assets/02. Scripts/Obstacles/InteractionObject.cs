using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public abstract class InteractionObject : MonoBehaviour, ICollisionable
{
    [HideInInspector] public int currentLane;
    public ObjectData objectData;
    public Action<ICollisionable> onTargetHitEvent;
    public Action<ICollisionable> onTakeHitEvent;

    private List<ICollisionable> collisionables = new();
    public Collider Col { get; private set; }
    public Rigidbody Rb { get; private set; }

    private Coroutine disposeCor;

    protected virtual void Reset()
    {
        Col = gameObject.GetComponent<Collider>();
        Rb = gameObject.GetComponent<Rigidbody>();
    }   

    protected virtual void Start()
    {
        Col = gameObject.GetComponent<Collider>();
        Rb = gameObject.GetComponent<Rigidbody>();
    }

    protected virtual void OnEnable()
    {
        disposeCor = StartCoroutine(DisposeCoroutine());
    }

    protected virtual void OnDisable()
    {
        if (disposeCor != null)
        {
            collisionables.Clear();
            StopCoroutine(disposeCor);
            disposeCor = null;
        }
    }
    private IEnumerator DisposeCoroutine()
    {
        while (true)
        {
            if (transform.position.z < GameManager.Instance.DisposePos.z)
            {
                gameObject.SetActive(false);
                yield break;
            }
            yield return null;
        }
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
