using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class AutoResizeRenderTextureForCamera : MonoBehaviour
{
    public Camera avatarCamera;
	
    [Header("Physical Camera Settings")]
    [Tooltip("Focal length in mm (e.g., 50mm is standard, 85mm is great for portraits)")]
    public float focalLength = 50f;
    public Camera.GateFitMode gateFitMode = Camera.GateFitMode.Vertical;	
	
    private RawImage rawImage;
    private RenderTexture dynamicTexture;
    private RectTransform rectTransform;

    private Vector2 lastSize;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
		
		ConfigurePhysicalCamera();
        ResizeTexture();
    }

    void Update()
    {
        // If the UI panel size changes due to resolution scaling, update the texture
        if (rectTransform.rect.width != lastSize.x || rectTransform.rect.height != lastSize.y)
        {
            ResizeTexture();
        }
    }
	
    void ConfigurePhysicalCamera()
    {
        if (avatarCamera == null) return;

        // Force the camera to use physical properties
        avatarCamera.usePhysicalProperties = true;
        avatarCamera.focalLength = focalLength;
        avatarCamera.gateFit = gateFitMode;
    }

    void ResizeTexture()
    {
        int width = Mathf.RoundToInt(rectTransform.rect.width);
        int height = Mathf.RoundToInt(rectTransform.rect.height);

        // Prevent errors if UI is hidden or zero-sized
        if (width <= 0 || height <= 0) return;

        // Clean up old texture to prevent memory leaks
        if (dynamicTexture != null)
        {
            avatarCamera.targetTexture = null;
            dynamicTexture.Release();
        }

        // Create new texture matching the exact UI pixel dimensions
        dynamicTexture = new RenderTexture(width, height, 24);
        
        // Apply to Camera and UI
        avatarCamera.targetTexture = dynamicTexture;
        rawImage.texture = dynamicTexture;

        lastSize = new Vector2(width, height);
    }

    void OnDestroy()
    {
        if (dynamicTexture != null) dynamicTexture.Release();
    }
}