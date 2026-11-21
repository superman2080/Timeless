using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 보스 카메라 상태. Intro: 컷씬 카메라(뷰 무관), Chase: 3D면 보스 추격 카메라 / 2D면 기존 2D 카메라
public enum BossCamMode
{
    None,
    Intro,
    Chase,
}

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
    [SerializeField] private CinemachineCamera vCamBoss3D;
    [SerializeField] private CinemachineCamera vCamBossIntro;
    private BossCamMode bossCamMode = BossCamMode.None;
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

        if (mode == ViewMode.View3D)
            Camera.main.cullingMask = ~0;
        else
            Camera.main.cullingMask &= ~LayerMask.GetMask("Right");
        ApplyPriorities(mode);
        yield return new WaitForSeconds(switchTime);
    }

    // 보스 카메라 전환. 우선순위는 상태(뷰 모드 + 보스 모드)로 계산되므로 큐를 거치지 않고 즉시 적용
    // (큐의 WaitForSeconds는 컷씬 정지 중 멈추기 때문). blend <= 0이면 컷
    public void SetBossCamera(BossCamMode mode, float blend)
    {
        if (mode == bossCamMode)
            return;

        bossCamMode = mode;
        cinemachineBrain.DefaultBlend = blend <= 0f
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
            : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blend);
        ApplyPriorities(GameManager.Instance.currentViewMode);
    }

    private void ApplyPriorities(ViewMode mode)
    {
        bool is2D = mode == ViewMode.View2D;
        vCam3D.Priority = is2D ? 5 : 10;
        vCam2D.Priority = is2D ? 10 : 5;
        if (vCamBoss3D != null)
            vCamBoss3D.Priority = bossCamMode == BossCamMode.Chase && !is2D ? 20 : 0;
        if (vCamBossIntro != null)
            vCamBossIntro.Priority = bossCamMode == BossCamMode.Intro ? 30 : 0;
    }

    // unscaled: true면 Time.timeScale 0(컷씬 정지) 중에도 흔들림
    public void CameraShake(float amplitude, float frequency, float duration, CameraShakeMode mode, bool unscaled = false)
    {
        if (cameraShakeCor != null)
            StopCoroutine(cameraShakeCor);
        cameraShakeCor = StartCoroutine(LinearCameraShakeCor(amplitude, frequency, duration, mode, unscaled));
    }

    private IEnumerator LinearCameraShakeCor(float amplitude, float frequency, float duration, CameraShakeMode mode, bool unscaled)
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
                    yield return unscaled ? (object)new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);
                    break;
                case CameraShakeMode.DECREMENT:
                    for (float elapsedTime = 0; elapsedTime < duration; elapsedTime += unscaled ? Time.unscaledDeltaTime : Time.deltaTime)
                    {
                        noise.AmplitudeGain = Mathf.Lerp(amplitude, 0, elapsedTime / duration);
                        yield return null;
                    }
                    break;
                case CameraShakeMode.INCREMENT:
                    for (float elapsedTime = 0; elapsedTime < duration; elapsedTime += unscaled ? Time.unscaledDeltaTime : Time.deltaTime)
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
