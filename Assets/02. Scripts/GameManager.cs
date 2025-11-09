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

    public ViewMode currentViewMode { get; private set; } = ViewMode.View3D;

    public LaneManager laneManager { get; private set; }
    public int laneLength => laneManager.laneLength;

    public float mapSpeed = 5f;

    [SerializeField] private Vector3 generatePos = Vector3.forward * 20;
    [SerializeField] private Vector3 disposePos = Vector3.back * 20;
    public Vector3 GeneratePos => generatePos;
    public Vector3 DisposePos => disposePos;

    private void Start()
    {
        player ??= FindAnyObjectByType<Player>();
        laneManager ??= FindAnyObjectByType<LaneManager>();
    }


    public void ChangeViewMode(ViewMode viewMode, float changeTime = 0.5f)
    {
        if(viewMode == currentViewMode)
            return;

        if (viewMode == ViewMode.View2D)
        {
            CameraManager.Instance.FadeGlitch(1f, 0.002f, changeTime);
            CameraManager.Instance.SetPixelateIntensity(4);
            player.ChangeLane(laneLength - 1);
        }
        else
        {
            CameraManager.Instance.SetPixelateIntensity(1);
        }

        CameraManager.Instance.SwitchCamera(viewMode, changeTime);
        currentViewMode = viewMode;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(generatePos, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(disposePos, 0.5f);
    }

}
