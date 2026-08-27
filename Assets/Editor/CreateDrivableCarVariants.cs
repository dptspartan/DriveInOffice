using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CreateDrivableCarVariants
{
    private const string SourcePrefabPath = "Assets/Prefabs/Cars/DrivableCar.prefab";
    private const string SedanPrefabPath = "Assets/Prefabs/Cars/DrivableCar_SedanSports.prefab";
    private const string SedanModelPath = "Assets/Models/Cars/OBJ format/sedan-sports.obj";

    [MenuItem("DriveInOffice/Cars/Create Sedan Sports Drivable Car")]
    public static void CreateSedanSportsDrivableCar()
    {
        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError("Missing source prefab: " + SourcePrefabPath);
            return;
        }

        Object[] sedanAssets = AssetDatabase.LoadAllAssetsAtPath(SedanModelPath);
        Dictionary<string, Mesh> meshesByName = new Dictionary<string, Mesh>();
        for (int i = 0; i < sedanAssets.Length; i++)
        {
            if (sedanAssets[i] is Mesh mesh && !string.IsNullOrEmpty(mesh.name))
                meshesByName[mesh.name] = mesh;
        }

        if (!meshesByName.ContainsKey("body"))
        {
            Debug.LogError("Could not find sedan-sports body mesh in " + SedanModelPath);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("Failed to instantiate " + SourcePrefabPath);
            return;
        }

        try
        {
            instance.name = "DrivableCar_SedanSports";
            Transform raceRoot = instance.transform.Find("race");
            if (raceRoot == null)
            {
                Debug.LogError("Could not find race root on DrivableCar prefab.");
                return;
            }

            raceRoot.name = "sedan-sports";

            SwapMesh(raceRoot.Find("body"), meshesByName, "body");
            SwapMesh(raceRoot.Find("Pivot_FL/wheel-front-left"), meshesByName, "wheel-front-left");
            SwapMesh(raceRoot.Find("Pivot_FR/wheel-front-right"), meshesByName, "wheel-front-right");
            SwapMesh(raceRoot.Find("Pivot_RL/wheel-back-left"), meshesByName, "wheel-back-left");
            SwapMesh(raceRoot.Find("Pivot_RR/wheel-back-right"), meshesByName, "wheel-back-right");

            if (meshesByName.TryGetValue("spoiler", out Mesh spoilerMesh))
            {
                Transform body = raceRoot.Find("body");
                if (body != null)
                {
                    GameObject spoiler = new GameObject("spoiler");
                    spoiler.transform.SetParent(body, false);
                    spoiler.AddComponent<MeshFilter>().sharedMesh = spoilerMesh;
                    MeshRenderer renderer = spoiler.AddComponent<MeshRenderer>();
                    MeshRenderer bodyRenderer = body.GetComponent<MeshRenderer>();
                    if (bodyRenderer != null && bodyRenderer.sharedMaterials.Length > 0)
                        renderer.sharedMaterials = bodyRenderer.sharedMaterials;
                }
            }

            CarPhysicsTier tier = raceRoot.GetComponent<CarPhysicsTier>();
            if (tier != null)
                tier.SetTier(CarTier.Sport);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, SedanPrefabPath);
            Debug.Log("Created drivable sedan prefab at " + SedanPrefabPath, saved);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [MenuItem("DriveInOffice/Map/Disable Light Pole Colliders In Prefabs")]
    public static void DisableLightPoleCollidersInPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("Prop_Light Prop_Electricity t:Prefab", new[] { "Assets/Prefabs/Map/Props" });
        int changed = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.Contains("Prop_Light") && !path.Contains("Prop_Electricity"))
                continue;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            Collider[] colliders = prefabRoot.GetComponentsInChildren<Collider>(true);
            bool touched = false;
            for (int c = 0; c < colliders.Length; c++)
            {
                if (colliders[c].enabled)
                {
                    colliders[c].enabled = false;
                    touched = true;
                }
            }

            if (touched)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                changed++;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log("Disabled colliders on " + changed + " light/electricity prop prefabs.");
    }

    private static void SwapMesh(Transform target, Dictionary<string, Mesh> meshesByName, string meshName)
    {
        if (target == null || !meshesByName.TryGetValue(meshName, out Mesh mesh))
            return;

        MeshFilter filter = target.GetComponent<MeshFilter>();
        if (filter != null)
            filter.sharedMesh = mesh;
    }
}
