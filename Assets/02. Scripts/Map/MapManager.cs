using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --- 트랙 Enum ---
public enum TrackType
{
    Tile1 = 0,
    Tile2,
    Tile3,
}

// --- (추가) 배경 Enum ---
// BackgroundManager.cs가 아닌 이곳에 정의하는 것을 권장합니다.
public enum BackgroundType
{
    Background1 = 0,
    Background2,
    Background3,
    LavaCorridor,
    DungeonCorridor,
}


public class MapManager : MonoBehaviour
{
    [Header("트랙 설정")]
    public TrackType nowTrack;
    [SerializeField] private GameObject[] trackPrefabs;

    [Header("배경 설정 (추가됨)")]
    public BackgroundType nowBackground;
    [SerializeField] private GameObject[] backgroundPrefabs;

    [Header("특수 배경 (N번째 생성마다 1타일)")]
    [SerializeField] private BackgroundType specialBackground = BackgroundType.LavaCorridor;
    [Tooltip("0이면 특수 배경을 생성하지 않음")]
    [SerializeField, Min(0)] private int specialInterval = 20;
    private int bgSpawnCount;

    [Header("트랙 생성 여부 (코리도 배경은 자체 바닥이 있어 끔)")]
    [SerializeField] private bool generateTrack = true;

    // 배경(맵) 기본 타입 전환 시 발행
    public event System.Action<BackgroundType> onBackgroundChanged;

    // 생성된 배경 타일 전체(비활성 포함). MapLighting이 활성 타일 위치를 조회
    private readonly List<BackgroundManager> backgrounds = new List<BackgroundManager>();
    public IReadOnlyList<BackgroundManager> Backgrounds => backgrounds;


    public Transform Pool => transform;

    /// <summary>
    /// 배경(맵) 전환. 이미 생성된 타일은 그대로 흘러가고 다음 스폰부터 새 타입이 나온다.
    /// </summary>
    public void ChangeBackground(BackgroundType type)
    {
        if (nowBackground == type)
            return;
        nowBackground = type;
        onBackgroundChanged?.Invoke(type);
    }

    [ContextMenu("Test/Change To LavaCorridor")]
    private void TestChangeToLava() => ChangeBackground(BackgroundType.LavaCorridor);

    [ContextMenu("Test/Change To Background2")]
    private void TestChangeToBackground2() => ChangeBackground(BackgroundType.Background2);

    void Start()
    {
        // --- 트랙 초기화 (기존) ---
        if (generateTrack)
        {
            InitializeTrackFill();
            StartCoroutine(GenerateMapCoroutine());
        }

        // --- (추가) 배경 초기화 ---
        var frontBg = InitializeBackgroundFill();
        StartCoroutine(GenerateBackgroundCoroutine(frontBg));
    }

    // ------------------------------------
    // 1. 트랙 생성 로직 (기존)
    // (Track.cs의 변수명 GeneratePos/DisposePos와 일치하는지 확인)
    // ------------------------------------
    private void InitializeTrackFill()
    {
        Vector3 currentPos = GameManager.Instance.GeneratePos;
        float totalDistance = GameManager.Instance.GeneratePos.z - GameManager.Instance.DisposePos.z;
        float filledDistance = 0f;

        while (filledDistance < totalDistance)
        {
            var tile = GenerateTile(nowTrack, currentPos, Quaternion.identity);
            float tileSize = tile.TrackSize.z;

            // (참고) Track.cs에서 bounds 대신 수동 Z길이를 쓰도록 수정하는 것을 권장합니다.
            // currentPos = new Vector3(currentPos.x, currentPos.y, currentPos.z - tileSize);

            // BackgroundManager.cs의 수동 길이 방식(권장)
            currentPos.z -= tileSize;
            filledDistance += tileSize;
        }
    }

    private IEnumerator GenerateMapCoroutine()
    {
        while (true)
        {
            var lastTile = GenerateTile(nowTrack, GameManager.Instance.GeneratePos, Quaternion.identity);
            float nextGenerateThreshold = GameManager.Instance.GeneratePos.z - lastTile.TrackSize.z;
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


    // ------------------------------------
    // 2. (추가) 배경 생성 로직 (복사 + 이름 변경)
    // ------------------------------------

    // N번째 생성마다 특수 배경, 나머지는 기본 배경
    private BackgroundType NextBackgroundType()
    {
        bgSpawnCount++;
        if (specialInterval > 0 && bgSpawnCount % specialInterval == 0)
            return specialBackground;
        return nowBackground;
    }

    // 'InitializeTrackFill'을 복사해서 'Background'로 이름 변경
    // 맨 앞(GeneratePos) 타일을 반환. 생성 코루틴이 그 뒤에 이어 붙인다
    private BackgroundManager InitializeBackgroundFill()
    {
        Vector3 currentPos = GameManager.Instance.GeneratePos;
        float totalDistance = GameManager.Instance.GeneratePos.z - GameManager.Instance.DisposePos.z;
        float filledDistance = 0f;
        BackgroundManager frontBg = null;

        while (filledDistance < totalDistance)
        {
            // GenerateTile -> GenerateBackground 호출
            var bg = GenerateBackground(NextBackgroundType(), currentPos, Quaternion.identity);
            if (frontBg == null)
                frontBg = bg;
            // .TrackSize -> .BackgroundSize
            float bgSize = bg.BackgroundSize.z;

            currentPos.z -= bgSize;
            filledDistance += bgSize;
        }
        return frontBg;
    }

    // 'GenerateMapCoroutine'을 복사해서 'Background'로 이름 변경
    // lastBg: Initialize가 채운 맨 앞 타일. 같은 위치에 겹쳐 생성하지 않도록 대기부터 한다
    private IEnumerator GenerateBackgroundCoroutine(BackgroundManager lastBg)
    {
        while (true)
        {
            if (lastBg != null)
            {
                // .TrackSize -> .BackgroundSize
                float nextGenerateThreshold = GameManager.Instance.GeneratePos.z - lastBg.BackgroundSize.z;

                // lastTile -> lastBg
                yield return new WaitUntil(() => lastBg.transform.position.z <= nextGenerateThreshold);
            }

            // 프레임 단위 대기로 생기는 틈을 막기 위해 직전 타일 바로 뒤에 붙여 생성
            Vector3 pos = GameManager.Instance.GeneratePos;
            if (lastBg != null)
                pos.z = lastBg.transform.position.z + lastBg.BackgroundSize.z;
            lastBg = GenerateBackground(NextBackgroundType(), pos, Quaternion.identity);
        }
    }

    // 'GenerateTile'을 복사해서 'BackgroundManager'로 이름 변경
    private BackgroundManager GenerateBackground(BackgroundType type, Vector3 pos, Quaternion rot)
    {
        // GetChildTiles -> GetChildBackgrounds
        foreach (var child in GetChildBackgrounds())
        {
            // child.trackType -> child.backgroundType
            if (child.gameObject.activeSelf == false && child.backgroundType == type)
            {
                child.gameObject.SetActive(true);
                child.transform.position = pos;
                child.transform.rotation = rot;
                return child;
            }
        }

        // trackPrefabs -> backgroundPrefabs
        var temp = Instantiate(backgroundPrefabs[(int)type], Pool);
        temp.transform.position = pos;
        temp.transform.rotation = rot;
        // GetComponent<Track> -> GetComponent<BackgroundManager>
        var bg = temp.GetComponent<BackgroundManager>();
        // .trackType -> .backgroundType
        bg.backgroundType = type;
        backgrounds.Add(bg);
        return bg;
    }

    // 'GetChildTiles'을 복사해서 'BackgroundManager'로 이름 변경
    private BackgroundManager[] GetChildBackgrounds() => Pool.GetComponentsInChildren<BackgroundManager>(true);
}