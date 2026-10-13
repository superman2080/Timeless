using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public CreateObjectSO objectDataSet3D;
    public CreateObjectSO objectDataSet2D;
    public Lane[] lanes;
    public Lane[] upperLanes;
    public int laneLength => lanes.Length;
    public float upperLaneHeight = 2.5f;
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

#if UNITY_EDITOR
    // 에디터에서도 런타임과 같은 위치로 배치 → 씬 뷰 Gizmo가 실제 레인 위치와 일치
    // (OnValidate 안에서 Transform을 바로 바꾸면 경고가 나므로 delayCall로 미룸)
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= InitLanesInEditor;
        UnityEditor.EditorApplication.delayCall += InitLanesInEditor;
    }

    void InitLanesInEditor()
    {
        if (this != null && !Application.isPlaying)
            InitLanes();
    }
#endif

    void InitLanes()
    {
        var allLanes = gameObject.GetComponentsInChildren<Lane>();
        lanes = System.Array.FindAll(allLanes, lane => !lane.IsUpper);
        upperLanes = System.Array.FindAll(allLanes, lane => lane.IsUpper);
        if (lanes.Length == 0)
        {
            Debug.LogWarning("No lanes found!");
            return;
        }

        Vector3 basePosition = Utils.GetTopViewportPosition(0.05f);
        PlaceLanes(lanes, basePosition);
        PlaceLanes(upperLanes, basePosition + Vector3.up * upperLaneHeight);
    }

    void PlaceLanes(Lane[] targets, Vector3 basePosition)
    {
        float centerOffset = (targets.Length - 1) * 0.5f;
        for (int i = 0; i < targets.Length; i++)
        {
            float xOffset = (i - centerOffset) * laneInterval;
            targets[i].transform.position = new Vector3(xOffset, basePosition.y, basePosition.z);
            targets[i].LaneIndex = i;
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

    // LaneManager.cs 수정
    private IEnumerator GeneratePatternCoroutine(CreateObjectSO dataSet)
    {
        int patternIndex = Random.Range(0, dataSet.objectSpawnDatas.Count);
        var pattern = dataSet.objectSpawnDatas[patternIndex];

        // 각 행(줄)을 순회
        for (int row = 0; row < pattern.rows.Count; row++)
        {
            GenerateRow(pattern.rows[row].spawnDatas);

            // 마지막 줄이 아니면 대기
            if (row < pattern.rows.Count - 1)
            {
                yield return new WaitForSeconds(dataSet.patternIntervalTime);
            }
        }
    }

    public void RemoveAllLaneObjects()
    {
        foreach (var lane in lanes)
        {
            lane.RemoveAllObjects();
        }
        foreach (var lane in upperLanes)
        {
            lane.RemoveAllObjects();
        }
    }

    void GenerateRow(List<ObjectSpawnData> rowData)
    {
        foreach (var spawnData in rowData)
        {
            if (spawnData.type == ObjectType.NONE)
                continue;

            var targets = spawnData.upper ? upperLanes : lanes;
            if (spawnData.laneIndex >= 0 && spawnData.laneIndex < targets.Length)
            {
                targets[spawnData.laneIndex].GenerateObstacle(spawnData.type);
            }
        }
    }
}