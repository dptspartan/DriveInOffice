using UnityEditor;
using UnityEngine;

public static class ApplySolidColorPropsMenu
{
    [MenuItem("DriveInOffice/Apply Solid Colors To Kenny Props")]
    public static void Apply()
    {
        Material metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SolidColors/Mat_Metal.mat");
        Material glow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SolidColors/Mat_LampGlow.mat");
        Material barrier = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SolidColors/Mat_Barrier.mat");
        if (metal == null || barrier == null)
        {
            Debug.LogError("Solid color materials missing under Assets/Materials/SolidColors/");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Map/Props" });
        int changed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            string n = prefab.name.ToLowerInvariant();
            Material primary = metal;
            Material secondary = null;
            if (n.Contains("barrier") || n.Contains("cone"))
                primary = barrier;
            else if (n.Contains("light"))
            {
                primary = metal;
                secondary = glow;
            }

            bool dirty = false;
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (secondary != null && r.sharedMaterials.Length > 1)
                {
                    Material[] mats = r.sharedMaterials;
                    mats[0] = primary;
                    for (int i = 1; i < mats.Length; i++)
                        mats[i] = secondary;
                    r.sharedMaterials = mats;
                }
                else
                {
                    r.sharedMaterial = primary;
                }
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                dirty = true;
            }

            if (dirty)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                changed++;
            }
        }

        Debug.Log($"Applied solid colors to {changed} Kenny prop prefabs.");
    }
}
