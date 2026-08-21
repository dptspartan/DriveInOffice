using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MapRuntimeQuality : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowDistance = 90f;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.lodBias = 1f;
        QualitySettings.particleRaycastBudget = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        RenderSettings.fog = false;
    }
}
