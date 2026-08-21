using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapPrefabBuilder
{
    private const string RoadsSource = "Assets/Models/Roads/OBJ format";
    private const string CarsSource = "Assets/Models/Cars/OBJ format";
    private const string PrefabRoot = "Assets/Prefabs/Map";
    private const float MapScale = 10f;

    private static readonly string[] ExtraCarProps = { "cone", "cone-flat", "box" };
    private static bool generating;

    [InitializeOnLoadMethod]
    private static void AutoGenerateIfEmpty()
    {
        EditorApplication.delayCall += TryAutoGenerate;
    }

    private static void TryAutoGenerate()
    {
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += TryAutoGenerate;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (HasGeneratedPrefabs())
            return;
        Generate();
    }

    [MenuItem("DriveInOffice/Generate Map Prefabs")]
    public static void Generate()
    {
        if (generating)
            return;

        generating = true;
        try
        {
            EnsureFolders();
            int created = 0;

            string[] roadObjs = Directory.GetFiles(ToAbsolute(RoadsSource), "*.obj");
            foreach (string absolutePath in roadObjs)
            {
                if (CreatePrefabFromObj(ToAssetPath(absolutePath)))
                    created++;
            }

            foreach (string extra in ExtraCarProps)
            {
                string assetPath = $"{CarsSource}/{extra}.obj";
                if (File.Exists(ToAbsolute(assetPath)) && CreatePrefabFromObj(assetPath))
                    created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"MapPrefabBuilder created {created} prefabs at scale {MapScale} under {PrefabRoot}.");
        }
        finally
        {
            generating = false;
        }
    }

    private static bool HasGeneratedPrefabs()
    {
        string roadsFolder = ToAbsolute($"{PrefabRoot}/Roads");
        return Directory.Exists(roadsFolder) && Directory.GetFiles(roadsFolder, "*.prefab").Length > 0;
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets/Prefabs", "Map");
        CreateFolder(PrefabRoot, "Roads");
        CreateFolder(PrefabRoot, "Tiles");
        CreateFolder(PrefabRoot, "Props");
    }

    private static void CreateFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static bool CreatePrefabFromObj(string assetPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string category = Categorize(fileName);
        string prefabName = ToPrefabName(fileName, category);
        string folder = $"{PrefabRoot}/{category}";
        string prefabPath = $"{folder}/{prefabName}.prefab";

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null)
        {
            Debug.LogWarning($"MapPrefabBuilder skipped missing model: {assetPath}");
            return false;
        }

        GameObject instance = Object.Instantiate(source);
        instance.name = prefabName;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * MapScale;
        SetStaticRecursive(instance);
        AddColliders(instance, fileName, category);

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        return true;
    }

    private static void AddColliders(GameObject root, string fileName, string category)
    {
        if (fileName.StartsWith("electricity-wires"))
            return;

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null)
                continue;

            Collider existing = filter.GetComponent<Collider>();
            if (existing != null)
                Object.DestroyImmediate(existing);

            if (category == "Roads" || category == "Tiles")
            {
                MeshCollider meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
                meshCollider.convex = false;
            }
            else
            {
                filter.gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    private static void SetStaticRecursive(GameObject go)
    {
        go.isStatic = true;
        foreach (Transform child in go.transform)
            SetStaticRecursive(child.gameObject);
    }

    private static string Categorize(string fileName)
    {
        if (fileName.StartsWith("road-"))
            return "Roads";
        if (fileName.StartsWith("tile-"))
            return "Tiles";
        return "Props";
    }

    private static string ToPrefabName(string fileName, string category)
    {
        string titled = ToTitleUnderscore(fileName);
        if (category == "Roads" || category == "Tiles")
            return titled;
        if (titled.StartsWith("Prop_"))
            return titled;
        return "Prop_" + titled;
    }

    private static string ToTitleUnderscore(string fileName)
    {
        string[] parts = fileName.Split('-');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (string.IsNullOrEmpty(part))
                continue;
            parts[i] = char.ToUpperInvariant(part[0]) + part.Substring(1);
        }

        return string.Join("_", parts);
    }

    private static string ToAbsolute(string assetPath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
    }

    private static string ToAssetPath(string absolutePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar;
        return absolutePath.Replace(projectRoot, string.Empty).Replace('\\', '/');
    }
}
