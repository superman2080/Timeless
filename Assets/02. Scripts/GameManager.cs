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


    private void Start()
    {
        player ??= FindAnyObjectByType<Player>();
        laneManager ??= FindAnyObjectByType<LaneManager>();
    }


    public void ChangeViewMode(ViewMode viewMode, float changeTime = 0.5f)
    {
        if (viewMode == nowViewMode)
            return;
    }
}
