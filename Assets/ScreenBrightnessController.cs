using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Applies the brightness slider to the rendered image through URP exposure.</summary>
public class ScreenBrightnessController : MonoBehaviour
{
    private const float MinimumExposure = -2f;
    private const float MaximumExposure = 2f;
    private static ScreenBrightnessController instance;

    private Volume brightnessVolume;
    private VolumeProfile runtimeProfile;
    private ColorAdjustments colorAdjustments;
    private Camera configuredCamera;

    public static void Apply(float normalizedBrightness)
    {
        if (instance == null)
        {
            GameObject controllerObject = new GameObject("Screen Brightness Controller");
            instance = controllerObject.AddComponent<ScreenBrightnessController>();
        }

        instance.ApplyExposure(normalizedBrightness);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        CreateBrightnessVolume();
        ConfigureMainCamera();
    }

    private void CreateBrightnessVolume()
    {
        brightnessVolume = gameObject.AddComponent<Volume>();
        brightnessVolume.isGlobal = true;
        brightnessVolume.priority = 10000f;
        brightnessVolume.weight = 1f;

        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.name = "Runtime Brightness Profile";
        runtimeProfile.hideFlags = HideFlags.DontSave;
        brightnessVolume.sharedProfile = runtimeProfile;

        colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
    }

    private void ApplyExposure(float normalizedBrightness)
    {
        if (colorAdjustments == null)
            CreateBrightnessVolume();

        float value = Mathf.Clamp01(normalizedBrightness);
        colorAdjustments.postExposure.value = Mathf.Lerp(
            MinimumExposure, MaximumExposure, value);

        ConfigureMainCamera();
    }

    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != configuredCamera)
            ConfigureMainCamera(mainCamera);
    }

    private void ConfigureMainCamera()
    {
        ConfigureMainCamera(Camera.main);
    }

    private void ConfigureMainCamera(Camera mainCamera)
    {
        if (mainCamera == null || mainCamera == configuredCamera)
            return;

        UniversalAdditionalCameraData cameraData =
            mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
            cameraData = mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();

        cameraData.renderPostProcessing = true;
        cameraData.volumeLayerMask = cameraData.volumeLayerMask.value |
                                     (1 << brightnessVolume.gameObject.layer);
        configuredCamera = mainCamera;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureMainCamera();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (runtimeProfile != null)
            Destroy(runtimeProfile);

        if (instance == this)
            instance = null;
    }
}