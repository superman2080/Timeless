using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Tile tile;
    public Transform Pool => transform;

    [SerializeField] private Vector3 generatePos = new Vector3(0, 0, 10);

    [SerializeField] private Vector3 disposePos = new Vector3(0, 0, -10);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(GenerateMapCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator GenerateMapCoroutine()
    {
        while (true) { 
            
        }
    }

    private Tile GenerateMap(Vector3 pos, Quaternion rot)
    {
        for (int i = 0; i < Pool.childCount; i++)
        {
            if(Pool.GetChild(i).gameObject.activeSelf == false)
            {
                var child = Pool.GetChild(i);
                child.gameObject.SetActive(true);
                child.transform.position = pos;
                child.transform.rotation = rot;
                return child.GetComponent<Tile>();
            }
        }

        var temp = Instantiate(tile, Pool);
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        return temp.GetComponent<Tile>();
    }
}
