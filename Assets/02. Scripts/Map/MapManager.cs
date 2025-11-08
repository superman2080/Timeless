using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackType
{
    Tile1 = 0,
    Tile2,
    Tile3,
}

public class MapManager : MonoBehaviour
{
    public TrackType nowTrack;
    [SerializeField] private GameObject[] trackPrefabs;
    public Transform Pool => transform;
    [SerializeField] private float speed = 5f;
    private float previousSpeed;
    private Vector3 generatePos;
    private Vector3 disposePos;

    void Start()
    {
        generatePos = Utils.GetTopViewportPosition(0f);
        disposePos = Utils.GetBottomViewportPosition(5f);
        previousSpeed = speed;
        StartCoroutine(GenerateMapCoroutine());
    }

    private void Update()
    {
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

            // 다음 타일이 생성될 위치 계산 (현재 타일의 끝 지점)
            float nextGenerateThreshold = generatePos.z - lastTile.TrackSize.z;

            // 타일의 뒷부분이 생성 위치를 지나갈 때까지 대기
            yield return new WaitUntil(() => lastTile.transform.position.z <= nextGenerateThreshold);
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
                child.speed = speed;
                return child;
            }
        }

        var temp = Instantiate(trackPrefabs[(int)type], Pool);
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        var track = temp.GetComponent<Track>();
        track.trackType = type;
        track.disposePos = disposePos;
        track.speed = speed;
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