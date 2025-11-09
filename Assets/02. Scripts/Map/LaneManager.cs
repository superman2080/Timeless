using System.Collections;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public CreateObjectSO objectDataSet3D;
    public CreateObjectSO objectDataSet2D;
    public Lane[] lanes;
    public int laneLength => lanes.Length;
    public float laneInterval = 1f;
    public float createInterval = 2f;
    private Coroutine generateCoroutine;

    void Start()
    {
        InitLanes();
        generateCoroutine = StartCoroutine(GenerateObjectCoroutine());
    }

    void Reset()
    {
        InitLanes();
    }

    void InitLanes()
    {
        lanes = gameObject.GetComponentsInChildren<Lane>();
        if (lanes.Length == 0)
        {
            Debug.LogWarning("No lanes found!");
            return;
        }

        float centerOffset = (laneLength - 1) * 0.5f;
        for (int i = 0; i < laneLength; i++)
        {
            float xOffset = (i - centerOffset) * laneInterval;
            Vector3 basePosition = Utils.GetTopViewportPosition(0.05f);
            lanes[i].transform.position = new Vector3(xOffset, basePosition.y, basePosition.z);
        }
    }

    private IEnumerator GenerateObjectCoroutine()
    {
        while (true)
        {
            CreateObjectSO currentDataSet = GameManager.Instance.currentViewMode == ViewMode.View2D
                ? objectDataSet2D
                : objectDataSet3D;

            yield return StartCoroutine(GeneratePatternCoroutine(currentDataSet));
            yield return new WaitForSeconds(createInterval);
        }
    }

    private IEnumerator GeneratePatternCoroutine(CreateObjectSO dataSet)
    {
        int patternIndex = Random.Range(0, dataSet.objectSpawnDatas.Length);
        var pattern = dataSet.objectSpawnDatas[patternIndex];

        // 2차원 배열의 각 행(줄)을 순회
        for (int row = 0; row < pattern.objectSpawnDatas.Length; row++)
        {
            GenerateRow(pattern.objectSpawnDatas[row]);

            // 마지막 줄이 아니면 대기
            if (row < pattern.objectSpawnDatas.Length - 1)
            {
                yield return new WaitForSeconds(dataSet.patternIntervalTime);
            }
        }
    }

    void GenerateRow(ObjectSpawnData[] rowData)
    {
        foreach (var spawnData in rowData)
        {
            if (spawnData.laneIndex >= 0 && spawnData.laneIndex < laneLength)
            {
                lanes[spawnData.laneIndex].GenerateObstacle(spawnData.type);
            }
        }
    }
}