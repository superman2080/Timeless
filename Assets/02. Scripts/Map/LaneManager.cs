using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public PatternSetSO patternSet;
    public Lane[] lanes;
    public Lane[] upperLanes;
    public int laneLength => lanes.Length;
    public float upperLaneHeight = 2.5f;
    public float laneInterval = 1f;
    [Tooltip("Lane/object spawn Z position")]
    public float spawnZ = 20f;
    [Tooltip("패턴 사이 거리 (유닛)")]
    public float patternDistance = 9f;
    private float distanceRemain; // 남은 대기 거리. 음수 = 직전 대기에서 초과 이동한 거리(이월)
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

        Vector3 basePosition = new Vector3(0f, 0f, spawnZ);
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
            var stat = GameManager.Instance.player.stat;
            CreateObjectSO currentDataSet = patternSet.Select(GameManager.Instance.currentViewMode, stat != null ? stat.HP / stat.MaxHP : 1f); // stat은 Player.Start에서 생성됨

            yield return StartCoroutine(GeneratePatternCoroutine(currentDataSet));
            yield return WaitDistance(patternDistance);
        }
    }

    // LaneManager.cs 수정
    private IEnumerator GeneratePatternCoroutine(CreateObjectSO dataSet)
    {
        var pattern = dataSet != null ? dataSet.PickRandom() : null;
        if (pattern == null)
            yield break;

        // 각 행(줄)을 순회
        for (int row = 0; row < pattern.rows.Count; row++)
        {
            GenerateRow(pattern.rows[row]);

            // 마지막 줄이 아니면 대기
            if (row < pattern.rows.Count - 1)
            {
                yield return WaitDistance(dataSet.rowDistance);
            }
        }
    }

    // 오브젝트가 FixedUpdate로 이동하므로 같은 단위로 누적해야 실제 간격과 일치
    private IEnumerator WaitDistance(float distance)
    {
        distanceRemain += distance;
        while (distanceRemain > 0f)
        {
            yield return new WaitForFixedUpdate();
            distanceRemain -= GameManager.Instance.mapSpeed * Time.fixedDeltaTime;
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

    void GenerateRow(PatternRow row)
    {
        // 상단 먼저: 같은 행에서 킬러가 생성되면 그 행의 하단도 막힌다
        GenerateUpperRow(row.upper);
        // 킬러가 살아 있는 동안(경고 포함)은 하단을 새로 생성하지 않는다
        if (BulletBill.ActiveCount == 0)
            GenerateRow(row.lower, lanes);
    }

    void GenerateUpperRow(ObjectType[] types)
    {
        // 킬러는 상단 전체를 덮는 1개 → 셀이 여러 개여도 가운데 상단 레인에 한 번만. 동시 1개, 3D에서만
        if (System.Array.IndexOf(types, ObjectType.BULLET_BILL) >= 0 && upperLanes.Length > 0
            && BulletBill.ActiveCount == 0 && GameManager.Instance.currentViewMode == ViewMode.View3D)
            upperLanes[upperLanes.Length / 2].GenerateObstacle(ObjectType.BULLET_BILL);
        GenerateRow(types, upperLanes);
    }

    void GenerateRow(ObjectType[] types, Lane[] targets)
    {
        for (int i = 0; i < types.Length && i < targets.Length; i++)
        {
            // Lakitu는 동시 1마리, 3D에서만 생성
            if (types[i] == ObjectType.LAKITU && (Lakitu.ActiveCount > 0 || GameManager.Instance.currentViewMode == ViewMode.View2D))
                continue;
            // Whomp도 동시 1마리, 3D에서만 생성
            if (types[i] == ObjectType.WHOMP && (Whomp.ActiveCount > 0 || GameManager.Instance.currentViewMode == ViewMode.View2D))
                continue;
            // 멍멍이는 2D에서 피할 수 없으므로 3D에서만 생성
            if (types[i] == ObjectType.CHAIN_CHOMP && GameManager.Instance.currentViewMode == ViewMode.View2D)
                continue;
            // 킬러는 GenerateUpperRow에서 따로 생성
            if (types[i] == ObjectType.BULLET_BILL)
                continue;
            targets[i].GenerateObstacle(types[i]);
        }
    }
}