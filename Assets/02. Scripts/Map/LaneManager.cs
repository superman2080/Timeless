using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public Lane[] lanes;
    public int laneLength => lanes.Length;
    public float laneInterval = 1f;
    
    void Start()
    {
        InitLanes();
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


}
