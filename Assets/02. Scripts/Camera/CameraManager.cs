using System.Collections;
using Unity.Cinemachine;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Splines;

public enum CameraShakeMode
{
    CONSTANT = 0,
    DECREMENT,
    INCREMENT,
}
public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private CinemachineCamera vCam3D;
    [SerializeField] private CinemachineCamera vCam2D;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    private Coroutine cameraShakeCor;
    private CinemachineCamera cam => cinemachineBrain.ActiveVirtualCamera as CinemachineCamera;

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

    public void CameraShake(float amplitude, float frequency, float duration, CameraShakeMode mode)
    {
        if (cameraShakeCor != null)
            StopCoroutine(cameraShakeCor);
        cameraShakeCor = StartCoroutine(LinearCameraShakeCor(amplitude, frequency, duration, mode));
    }

    private IEnumerator LinearCameraShakeCor(float amplitude, float frequency, float duration, CameraShakeMode mode)
    {
        if (cam == null)
        {
            Debug.LogWarning("No CinemachineCamera found.");
            yield break;
        }

        // Stage.Noise에서 CinemachineComponentBase 가져오기
        var component = cam.GetCinemachineComponent(CinemachineCore.Stage.Noise);

        // CinemachineBasicMultiChannelPerlin으로 캐스팅
        if (component is CinemachineBasicMultiChannelPerlin noise)
        {
            noise.AmplitudeGain = amplitude;
            noise.FrequencyGain = frequency;
            switch (mode)
            {
                case CameraShakeMode.CONSTANT:
                    yield return new WaitForSeconds(duration);
                    break;
                case CameraShakeMode.DECREMENT:
                    for (float elapsedTime = 0; elapsedTime < duration; elapsedTime += Time.deltaTime)
                    {
                        noise.AmplitudeGain = Mathf.Lerp(amplitude, 0, elapsedTime / duration);
                        yield return null;
                    }
                    break;
                case CameraShakeMode.INCREMENT:
                    for (float elapsedTime = 0; elapsedTime < duration; elapsedTime += Time.deltaTime)
                    {
                        noise.AmplitudeGain = Mathf.Lerp(0, amplitude, elapsedTime / duration);
                        yield return null;
                    }
                    break;
                default:
                    break;
            }
            noise.AmplitudeGain = 0;
            noise.FrequencyGain = 0;
            cameraShakeCor = null;
        }
        else
        {
            Debug.LogWarning("There's no CinemachineBasicMultiChannelPerlin component.");
        }
    }

}
