using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Splines;

public enum CameraShakeMode
{
    CONSTANT = 0,
    DECREMENT,
    INCREMENT,
}

//
public class CameraManager : Singleton<CameraManager>
{
    [SerializeField] private CinemachineCamera vCam3D;
    [SerializeField] private CinemachineCamera vCam2D;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    private Coroutine cameraShakeCor;
    private CinemachineCamera cam => cinemachineBrain.ActiveVirtualCamera as CinemachineCamera;

    #region PostProcessing Feature
    private Queue<IEnumerator> cameraAction = new();
    public FullScreenPassRendererFeature glitchPassFeature;
    public PixelateRendererFeature pixelateRendererFeature;
    private Coroutine glitchFadeCor;
    #endregion

    void Start()
    {
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        StartCoroutine(CameraActionCoroutine());

        
    }

    #region PostProcessing Feature

    public void SetGlitch(float value)
    {
        glitchPassFeature.passMaterial.SetFloat("_Alpha", Mathf.Clamp01(value));
    }

    public void SetPixelateIntensity(int intensity)
    {
        var settings = pixelateRendererFeature.settings;
        settings.pixelScale = Mathf.Clamp(intensity, 1, 16);
        pixelateRendererFeature.settings = settings;

#if UNITY_EDITOR
        EditorUtility.SetDirty(pixelateRendererFeature);
#endif

        // 또는 URP 렌더러 데이터 갱신
        var renderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (renderPipelineAsset != null)
        {
            // 렌더러 파이프라인 재초기화
            QualitySettings.renderPipeline = renderPipelineAsset;
        }
    }

    public void FadeGlitch(float startValue, float targetValue, float duration)
    {
        if(glitchFadeCor != null)
            StopCoroutine(glitchFadeCor);

        glitchFadeCor = StartCoroutine(FadeGlitchCoroutine(startValue, targetValue, duration));
    }

    private IEnumerator FadeGlitchCoroutine(float startValue, float targetValue, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float currentValue = Mathf.Clamp01(Mathf.Lerp(startValue, targetValue, elapsedTime / duration));
            SetGlitch(currentValue);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        SetGlitch(targetValue);
        glitchFadeCor = null;
    }

    #endregion

    private IEnumerator CameraActionCoroutine()
    {
        while (true)
        {
            yield return new WaitUntil(() => cameraAction.Count > 0);
            yield return StartCoroutine(cameraAction.Dequeue());
            yield return null;
        }
    }

    public void SwitchCamera(ViewMode mode, float switchTime = 1.5f)
    {
        cameraAction.Enqueue(SwitchCameraCoroutine(mode, switchTime));
    }

    private IEnumerator SwitchCameraCoroutine(ViewMode mode, float switchTime)
    {
        if (vCam2D == null || vCam3D == null)
        {
            Debug.LogError("Cinemachine Cameras are not assigned.");
            yield break;
        }

        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseOut, switchTime);

        switch (mode)
        {
            case ViewMode.View3D:
                Camera.main.cullingMask = ~0;
                vCam3D.Priority = 10;
                vCam2D.Priority = 5;
                break;
            case ViewMode.View2D:
                Camera.main.cullingMask &= ~LayerMask.GetMask("Right");
                vCam3D.Priority = 5;
                vCam2D.Priority = 10;
                break;
        }
        yield return new WaitForSeconds(switchTime);
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

    private void OnApplicationQuit()
    {
        SetGlitch(0.002f);
        SetPixelateIntensity(1);
    }
}
