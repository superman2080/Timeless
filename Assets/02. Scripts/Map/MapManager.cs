using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackType
{
    TILE_1 = 0,
    TILE_2,
    TILE_3,
}

public class MapManager : MonoBehaviour
{
    public TrackType nowTrack;
    [SerializeField] private GameObject[] trackPrefabs;
    public Transform Pool => transform;

    [SerializeField] private float speed = 5f;
    private float previousSpeed;

    [SerializeField] private Vector3 generatePos = new Vector3(0, 0, 10);
    [SerializeField] private Vector3 disposePos = new Vector3(0, 0, -10);

    void Start()
    {
        previousSpeed = speed;
        StartCoroutine(GenerateMapCoroutine());
    }

    private void Update()
    {
        // Inspector에서 속도가 변경되었는지 확인
        if (previousSpeed != speed)
        {
            previousSpeed = speed;
            ModifyTileSpeed();
        }
    }

    private IEnumerator GenerateMapCoroutine()
    {
        while (true)
        {
            var lastTile = GenerateTile(nowTrack, generatePos, Quaternion.identity);
            float correctionVal = speed * 0.01f;
            var threshold = lastTile.TrackSize.z / 2 + correctionVal;
            yield return new WaitUntil(() => lastTile.transform.position.z - lastTile.TrackSize.z / 2 <= threshold);
        }
    }

    private Track GenerateTile(TrackType type, Vector3 pos, Quaternion rot)
    {
        foreach (var child in GetChildTiles())
        {
            if (child.gameObject.activeSelf == false && child.trackType == type)
            {
                child.gameObject.SetActive(true);
                child.transform.position = pos;
                child.transform.rotation = rot;
                child.disposePos = disposePos;
                child.speed = speed; // 생성 시 속도 설정
                return child;
            }
        }

        var temp = Instantiate(trackPrefabs[(int)type], Pool);
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        var track = temp.GetComponent<Track>();
        track.trackType = type;
        track.disposePos = disposePos;
        track.speed = speed; // 생성 시 속도 설정
        return track;
    }

    private void ModifyTileSpeed()
    {
        foreach (var tile in GetChildTiles())
        {
            tile.speed = speed;
        }
    }

    private Track[] GetChildTiles() => Pool.GetComponentsInChildren<Track>(true);
}