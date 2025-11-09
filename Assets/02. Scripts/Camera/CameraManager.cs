using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Cinemachine;

public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private CinemachineCamera vCam3D;
    [SerializeField] private CinemachineCamera vCam2D;
    [SerializeField] private CinemachineBrain cinemachineBrain;

    private bool isSwitching = false;

    void Start()
    {
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
    }

    public void SwitchCamera(ViewMode mode, float switchTime = 1.5f)
    {
        if (vCam2D == null || vCam3D == null || isSwitching)
        {
            Debug.LogError("Cinemachine Cameras are not assigned.");
            return;
        }
        isSwitching = true;

        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseOut, switchTime);

        switch (mode)
        {
            case ViewMode.View3D:
                vCam3D.Priority = 10;
                vCam2D.Priority = 5;
                break;
            case ViewMode.View2D:
                vCam3D.Priority = 5;
                vCam2D.Priority = 10;
                break;
        }
        isSwitching = false;
    }

}
