using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [SerializeField] private Tile tile;
    public Transform Pool => transform;
    [SerializeField] private float speed = 5f;
    public float Speed
    {
        get => speed;
        set
        {
            if (speed != value)
            {
                speed = value;
                ModifyTileSpeed();
            }
        }
    }

    [SerializeField] private Vector3 generatePos = new Vector3(0, 0, 10);

    [SerializeField] private Vector3 disposePos = new Vector3(0, 0, -10);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(GenerateMapCoroutine());
    }
    

    private IEnumerator GenerateMapCoroutine()
    {
        while (true) {
            var lastTile = GenerateTile(generatePos, Quaternion.identity);
            // 타일 간 간격 보정치
            float correctionVal = Speed * 0.01f;
            // 생성 임계값
            var threshold = lastTile.TileSize.z / 2 + correctionVal;
            yield return new WaitUntil(() => lastTile.transform.position.z - lastTile.TileSize.z / 2 <= threshold);
        }
    }

    private Tile GenerateTile(Vector3 pos, Quaternion rot)
    {
        foreach(var tile in GetChildTiles())
        {
            if(tile.gameObject.activeSelf == false)
            {
                tile.gameObject.SetActive(true);
                tile.transform.position = pos;
                tile.transform.rotation = rot;
                tile.GetComponent<Tile>().disposePos = disposePos;
                return tile.GetComponent<Tile>();
            }
        }

        var temp = Instantiate(tile.gameObject, Pool);
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        temp.GetComponent<Tile>().disposePos = disposePos;
        return temp.GetComponent<Tile>();
    }

    private void ModifyTileSpeed()
    {
        foreach (var tile in GetChildTiles())
        {
            tile.speed = Speed;
        }
    }

    private Tile[] GetChildTiles() => Pool.GetComponentsInChildren<Tile>(true);
}
