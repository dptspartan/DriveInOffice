using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// CI entry point for GitHub Actions WebGL builds.
/// </summary>
public static class WebGLBuild
{
    private static readonly string[] Scenes = { "Assets/Scenes/City-Drive.unity" };
    private const string OutputPath = "build/WebGL";

    public static void PerformBuild()
    {
        BuildReport report = BuildPipeline.BuildPlayer(
            Scenes,
            OutputPath,
            BuildTarget.WebGL,
            BuildOptions.None);

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");

        // GitHub Pages: skip Jekyll so .wasm / .data are served correctly.
        File.WriteAllText(Path.Combine(OutputPath, ".nojekyll"), string.Empty);
    }
}
