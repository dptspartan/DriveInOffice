using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;

/// <summary>
/// CI entry point for GitHub Actions WebGL builds.
/// </summary>
public static class WebGLBuild
{
    private static readonly string[] Scenes = { "Assets/Scenes/City-Drive.unity" };
    private const string OutputPath = "build/WebGL";

    public static void PerformBuild()
    {
        ApplyWebGLPlayerSettings();

        BuildReport report = BuildPipeline.BuildPlayer(
            Scenes,
            OutputPath,
            BuildTarget.WebGL,
            BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");

        File.WriteAllText(Path.Combine(OutputPath, ".nojekyll"), string.Empty);
    }

    private static void ApplyWebGLPlayerSettings()
    {
        // GitHub Pages serves gzip reliably; brotli (.br) breaks without Content-Encoding: br.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        const string templatePath = "Assets/WebGLTemplates/Fullscreen";
        if (AssetDatabase.IsValidFolder(templatePath))
            PlayerSettings.WebGL.template = "PROJECT:Fullscreen";
    }
}
