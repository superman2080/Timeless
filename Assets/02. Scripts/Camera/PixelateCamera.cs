using UnityEngine;

public class PixelateCamera : MonoBehaviour
{
    [SerializeField] private int pixelScale = 4;

    private RenderTexture renderTexture;
    private int lastWidth, lastHeight;

    private void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        CreateRenderTexture();
    }

    private void CreateRenderTexture()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        int width = Screen.width / pixelScale;
        int height = Screen.height / pixelScale;

        renderTexture = new RenderTexture(width, height, 24);
        renderTexture.filterMode = FilterMode.Point; // 픽셀 느낌을 위해 Point 필수
    }

    private void Update()
    {
        // 해상도 변경 감지
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            CreateRenderTexture();
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // 저해상도 RenderTexture에 먼저 렌더링
        Graphics.Blit(source, renderTexture);

        // 그걸 다시 화면 크기로 확대해서 출력
        Graphics.Blit(renderTexture, destination);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}