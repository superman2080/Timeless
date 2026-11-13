using UnityEngine;
using System.Collections.Generic;

public abstract class Pool<T> : Singleton<Pool<T>> where T : MonoBehaviour
{
    public Transform pool => transform;
    public abstract GameObject Prefab { get; }

    // 풀이 가진 모든 인스턴스(비활성 포함). 반환 = SetActive(false)이므로 활성 여부는 activeSelf로 판별
    private readonly List<T> items = new();
    public IReadOnlyList<T> Items => items;

    protected override void Awake()
    {
        base.Awake();
        GetComponentsInChildren(true, items); // 씬에 미리 배치된 자식만 한 번 수집
    }

    public virtual T Get()
    {
        foreach (var item in items)
        {
            if (item != null && !item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(true);
                return item;
            }
        }

        return Create(Prefab);
    }

    // 프리팹을 풀 자식으로 생성하고 목록에 등록한다 (활성 상태로 반환됨)
    protected T Create(GameObject prefab)
    {
        var result = Instantiate(prefab, Vector3.zero, Quaternion.identity, pool).GetComponent<T>();
        items.Add(result);
        return result;
    }

    public virtual void Return(T obj)
    {
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        obj.gameObject.SetActive(false);
    }
}
