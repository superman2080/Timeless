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

    void Start()
    {

        // 시작 시 화면을 타일로 채우기
        InitializeTrackFill();

        StartCoroutine(GenerateMapCoroutine());
    }

    private void InitializeTrackFill()
    {
        Vector3 currentPos = GameManager.Instance.generatePos;
        float totalDistance = GameManager.Instance.generatePos.z - GameManager.Instance.disposePos.z;
        float filledDistance = 0f;

        // disposePos부터 generatePos까지 타일로 채우기
        while (filledDistance < totalDistance)
        {
            var tile = GenerateTile(nowTrack, currentPos, Quaternion.identity);
            float tileSize = tile.TrackSize.z;

            // 다음 타일 위치 계산
            currentPos = new Vector3(currentPos.x, currentPos.y, currentPos.z - tileSize);
            filledDistance += tileSize;
        }
    }

    private IEnumerator GenerateMapCoroutine()
    {
        while (true)
        {
            var lastTile = GenerateTile(nowTrack, GameManager.Instance.generatePos, Quaternion.identity);
            // 다음 타일이 생성될 위치 계산 (현재 타일의 끝 지점)
            float nextGenerateThreshold = GameManager.Instance.generatePos.z - lastTile.TrackSize.z;
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
                return child;
            }
        }

        var temp = Instantiate(trackPrefabs[(int)type], Pool);
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        var track = temp.GetComponent<Track>();
        track.trackType = type;
        return track;
    }

    private Track[] GetChildTiles() => Pool.GetComponentsInChildren<Track>(true);
}