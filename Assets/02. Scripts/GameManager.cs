using UnityEngine;

public enum ViewMode
{
    View3D,
    View2D,
}

[DefaultExecutionOrder(-100)]

public class GameManager : Singleton<GameManager>
{
    public Player player { get; private set; }

    public ViewMode nowViewMode { get; private set; } = ViewMode.View3D;

    public LaneManager laneManager { get; private set; }
    public int laneLength => laneManager.laneLength;

    public float mapSpeed = 5f;

    [SerializeField] private Vector3 generatePos = Vector3.forward * 20;
    [SerializeField] private Vector3 disposePos = Vector3.back * 20;
    public Vector3 GeneratePos => generatePos;
    public Vector3 DisposePos => disposePos;

    //public (Vector3 generatePos, Vector3 disposePos) positionLimits
    //{
    //    get
    //    {
    //        //if (nowViewMode == ViewMode.View3D)
    //        //{
    //        Vector3 generatePos = Utils.GetTopViewportPosition(1f);
    //        Vector3 disposePos = Utils.GetBottomViewportPosition(7f);
    //        return (generatePos, disposePos);
    //        //}
    //        //else
    //        //{
    //        //    Vector3 generatePos = Utils.GetRightViewportPosition(0.1f);
    //        //    Vector3 disposePos = Utils.GetLeftViewportPosition(0.1f);
    //        //    return (generatePos, disposePos);
    //        //}
    //    }
    //}

    private void Start()
    {
        player ??= FindAnyObjectByType<Player>();
        laneManager ??= FindAnyObjectByType<LaneManager>();
    }


    public void ChangeViewMode(ViewMode viewMode, float changeTime = 0.5f)
    {
        if (viewMode == nowViewMode)
            return;

        CameraManager.Instance.SwitchCamera(viewMode, changeTime);
        nowViewMode = viewMode;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(generatePos, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(disposePos, 0.5f);
    }

}
