using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-click editor helpers for City-Drive WebGL performance (occlusion bake, light bake, probes).
/// </summary>
public static class CityDriveWebGLPrep
{
    private const string ScenePath = "Assets/Scenes/City-Drive.unity";

    [MenuItem("DriveInOffice/City-Drive/Bake Occlusion Culling")]
    public static void BakeOcclusion()
    {
        if (!OpenCityDrive())
            return;

        StaticOcclusionCulling.GenerateInBackground();
        Debug.Log("Occlusion bake started. Wait for progress bar to finish, then save the scene.");
    }

    [MenuItem("DriveInOffice/City-Drive/Bake Lighting (Baked GI)")]
    public static void BakeLighting()
    {
        if (!OpenCityDrive())
            return;

        Lightmapping.BakeAsync();
        Debug.Log("Lighting bake started. When done, save the scene (File → Save).");
    }

    [MenuItem("DriveInOffice/City-Drive/Add Light Probe Grid (car area)")]
    public static void AddLightProbeGrid()
    {
        if (!OpenCityDrive())
            return;

        GameObject car = GameObject.Find("DrivableCar");
        Vector3 center = car != null ? car.transform.position : new Vector3(-140f, 2f, -150f);

        var existing = Object.FindFirstObjectByType<LightProbeGroup>();
        if (existing != null)
        {
            Debug.LogWarning("Light Probe Group already exists: " + existing.name);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject root = new GameObject("LightProbes_CarArea");
        LightProbeGroup group = root.AddComponent<LightProbeGroup>();
        group.probePositions = BuildProbeGrid(center, 18f, 4f, 3f);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log($"Added Light Probe Group with {group.probePositions.Length} probes near {center}.");
    }

    [MenuItem("DriveInOffice/City-Drive/Enable GPU Instancing On Materials")]
    public static void EnableGpuInstancingOnMaterials()
    {
        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.enableInstancing)
                continue;
            if (!mat.shader.name.Contains("Lit") && !mat.shader.name.Contains("Universal"))
                continue;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            changed++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Enabled GPU Instancing on {changed} materials.");
    }

    [MenuItem("DriveInOffice/City-Drive/Show WebGL Build Checklist")]
    public static void ShowChecklist()
    {
        EditorUtility.DisplayDialog(
            "City-Drive WebGL checklist",
            "1. Run: Bake Occlusion Culling\n" +
            "2. Run: Bake Lighting (Baked GI)\n" +
            "3. Run: Add Light Probe Grid\n" +
            "4. File → Build Settings → WebGL → City-Drive first scene\n" +
            "5. Player Settings → WebGL → Compression: Gzip or Brotli\n" +
            "6. After bakes: File → Save scene",
            "OK");
    }

    private static bool OpenCityDrive()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("Missing scene: " + ScenePath);
            return false;
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static Vector3[] BuildProbeGrid(Vector3 center, float halfExtent, float spacing, float height)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(halfExtent * 2f / spacing));
        var list = new System.Collections.Generic.List<Vector3>(steps * steps * 2);
        float startX = center.x - halfExtent;
        float startZ = center.z - halfExtent;
        for (int x = 0; x <= steps; x++)
        {
            for (int z = 0; z <= steps; z++)
            {
                float px = startX + x * spacing;
                float pz = startZ + z * spacing;
                list.Add(new Vector3(px, center.y + height * 0.5f, pz));
                list.Add(new Vector3(px, center.y + height, pz));
            }
        }
        return list.ToArray();
    }
}
