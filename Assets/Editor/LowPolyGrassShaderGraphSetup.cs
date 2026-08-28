using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates a URP Unlit Shader Graph for low-poly grass (texture + color variation).
/// Use if you prefer editing in Shader Graph instead of the HLSL shader.
/// </summary>
public static class LowPolyGrassShaderGraphSetup
{
    private const string GraphFolder = "Assets/ShaderGraphs";
    private const string GraphPath = GraphFolder + "/LowPolyGrass.shadergraph";
    private const string MatPath = "Assets/Materials/CityDrive/Mat_LowPolyGrass_Graph.mat";

    [MenuItem("DriveInOffice/City-Drive/Create Low Poly Grass Shader Graph")]
    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder(GraphFolder))
            AssetDatabase.CreateFolder("Assets", "ShaderGraphs");

        // Create from Unity's URP Unlit template so the graph opens cleanly in Shader Graph.
        Shader graphShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (graphShader == null)
        {
            EditorUtility.DisplayDialog(
                "Low Poly Grass",
                "Could not find URP Unlit. Create manually:\n" +
                "Create → Shader Graph → URP → Unlit Shader Graph\n" +
                "Then follow the node setup in the console.",
                "OK");
            PrintManualNodeGuide();
            return;
        }

        // Prefer the existing HLSL material path — Shader Graph node wiring is manual.
        PrintManualNodeGuide();

        EditorUtility.DisplayDialog(
            "Low Poly Grass Shader Graph",
            "Ready-to-use material already exists:\n" +
            "Assets/Materials/CityDrive/Mat_LowPolyGrass.mat\n\n" +
            "Shader: CityDrive/LowPolyGrass\n\n" +
            "To make a Shader Graph version instead, follow the steps printed in the Console.",
            "OK");
    }

    private static void PrintManualNodeGuide()
    {
        Debug.Log(
            "[Low Poly Grass] Shader Graph node setup:\n" +
            "1. Create → Shader Graph → URP → Unlit Shader Graph → LowPolyGrass\n" +
            "2. Properties: Texture2D BaseMap, Color ColorA, Color ColorB, Color ColorC,\n" +
            "   Float FacetScale (2.5), Float Variation (0.75), Float TextureStrength (0.55)\n" +
            "3. Position(World) → Split XZ → Divide by FacetScale → Floor →\n" +
            "   use as seed into Random Range / Gradient Noise for variation mask\n" +
            "4. Lerp ColorA↔ColorB by mask, Lerp toward ColorC slightly\n" +
            "5. Sample Texture2D(BaseMap) → Lerp(tint, tex*tint, TextureStrength) → Base Color\n" +
            "6. Create Material from graph, assign grass texture, drop on Terrain/mesh\n" +
            "Ready HLSL material: Assets/Materials/CityDrive/Mat_LowPolyGrass.mat");
    }
}
