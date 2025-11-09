using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class Pool<T> : Singleton<Pool<T>> where T : MonoBehaviour
{
    public Transform pool => transform;
    public abstract GameObject Prefab { get; }

    public virtual T Get()
    {
        foreach (var child in GetChildList(true))
        {
            if (child.gameObject.activeSelf == false && child.TryGetComponent(out T type))
            {
                child.gameObject.SetActive(true);
                return type;
            }
        }

        var result = Instantiate(Prefab, Vector3.zero, Quaternion.identity, pool).GetComponent<T>();
        return result;
    }


    public List<T> GetChildList(bool includeInactive = true)
    {
        return transform.GetComponentsInChildren<T>(includeInactive).ToList();
    }

    public virtual void Return(T obj)
    {
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.gameObject.SetActive(false);
    }
}
