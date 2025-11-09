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

        // Áß¾Ó ÀÎµ¦½º °è»ê (Â¦¼ö/È¦¼ö ¸ðµÎ Ã³¸®)
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
            GeneratePattern(GameManager.Instance.currentViewMode == ViewMode.View2D ? objectDataSet2D : objectDataSet3D);
            yield return new WaitForSeconds(createInterval);
        }
    }

    void GeneratePattern(CreateObjectSO dataSet)
    {
        int patternIndex = Random.Range(0, dataSet.objectSpawnDatas.Length);
        var pattern = dataSet.objectSpawnDatas[patternIndex];
        foreach (var spawnData in pattern.objectSpawnDatas)
        {
            if (spawnData.laneIndex >= 0 && spawnData.laneIndex < laneLength)
            {
                lanes[spawnData.laneIndex].GenerateObstacle(spawnData.type);
            }
        }
    }
}
