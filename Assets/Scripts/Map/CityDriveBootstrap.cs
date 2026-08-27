using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Low-spec helpers for City-Drive: solid prop colors, terrain tint, distance culling.
/// Does not touch the Sun or RenderSettings lighting — set those in the scene.
/// </summary>
[DefaultExecutionOrder(-120)]
public class CityDriveBootstrap : MonoBehaviour
{
    public Material propMetal;
    public Material propLampGlow;
    public Material propBarrier;
    public Material terrainGrass;
    public Material terrainAsphalt;

    private void Awake()
    {
        ApplyLowSpecQuality();
        ApplySolidColorsToProps();
        ApplyTerrainColors();
        EnsureCuller();
    }

    private static void ApplyLowSpecQuality()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowDistance = 55f;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.lodBias = 0.7f;
        QualitySettings.maximumLODLevel = 1;
        QualitySettings.particleRaycastBudget = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.antiAliasing = 0;
        QualitySettings.softParticles = false;
        QualitySettings.globalTextureMipmapLimit = 1;
    }

    private void ApplySolidColorsToProps()
    {
        ApplyToNamed("light", propMetal, propLampGlow);
        ApplyToNamed("lamp", propMetal, propLampGlow);
        ApplyToNamed("construction-barrier", propBarrier, null);
        ApplyToNamed("construction-cone", propBarrier, null);
        ApplyToNamed("cone", propBarrier, null);
        ApplyToNamed("electricity", propMetal, null);
        ApplyToNamed("traffic-light", propMetal, propLampGlow);
        ApplyToNamed("RoadsideDecor", propBarrier, null);
    }

    private void ApplyToNamed(string nameHint, Material primary, Material emissiveOrNull)
    {
        if (primary == null)
            return;

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.gameObject == null)
                continue;
            string n = r.gameObject.name;
            if (n.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) < 0
                && (r.transform.parent == null || r.transform.parent.name.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) < 0))
                continue;

            if (emissiveOrNull != null && n.IndexOf("light", System.StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("traffic", System.StringComparison.OrdinalIgnoreCase) < 0
                && r.sharedMaterials != null && r.sharedMaterials.Length > 1)
            {
                Material[] mats = r.sharedMaterials;
                mats[0] = primary;
                for (int m = 1; m < mats.Length; m++)
                    mats[m] = emissiveOrNull;
                r.sharedMaterials = mats;
            }
            else
            {
                r.sharedMaterial = primary;
            }

            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }

    private void ApplyTerrainColors()
    {
        Terrain terrain = FindAnyObjectByType<Terrain>();
        if (terrain == null)
            return;

        if (terrainGrass != null)
            terrain.materialTemplate = terrainGrass;

        TerrainLayer[] layers = terrain.terrainData != null ? terrain.terrainData.terrainLayers : null;
        if (layers == null || layers.Length == 0 || terrainGrass == null)
            return;

        Texture2D grassTex = terrainGrass.HasProperty("_BaseMap")
            ? terrainGrass.GetTexture("_BaseMap") as Texture2D
            : null;
        if (grassTex == null)
            return;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
                continue;
            layers[i].diffuseTexture = grassTex;
            layers[i].tileSize = new Vector2(24f, 24f);
        }
    }

    private void EnsureCuller()
    {
        CityDistanceCuller culler = GetComponent<CityDistanceCuller>();
        if (culler == null)
            culler = gameObject.AddComponent<CityDistanceCuller>();

        GameObject near = GameObject.Find("Near Geometry");
        if (near != null)
        {
            Transform houses = near.transform.Find("Houses");
            Transform map = near.transform.Find("Map");
            if (houses != null)
                culler.buildingsRoot = houses;
            if (map != null)
                culler.roadsRoot = map;
        }

        GameObject decor = GameObject.Find("RoadsideDecor");
        if (decor != null)
            culler.propsRoot = decor.transform;

        culler.buildingDistance = 160f;
        culler.propDistance = 80f;
        culler.roadDistance = 200f;
    }
}
