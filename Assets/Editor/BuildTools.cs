using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Embers ▸ Build Windows Playtest. Also callable from the command line:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod BuildTools.BuildWindows
/// Produces Builds/EmbersOfSundermere/ and a zip next to it for copying to the laptop.
/// </summary>
public static class BuildTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string OutDir = "Builds/EmbersOfSundermere";

    [MenuItem("Embers/Build Windows Playtest")]
    public static void BuildWindows()
    {
        EnsureScene();
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;

        if (Directory.Exists(OutDir)) Directory.Delete(OutDir, true);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(OutDir, "Embers of Sundermere.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        string zip = "Builds/EmbersOfSundermere.zip";
        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(OutDir, zip);
        Debug.Log($"Build OK: {report.summary.totalSize / (1024 * 1024)} MB → {OutDir} (zipped: {zip})");
    }

    [MenuItem("Embers/Create Main Scene")]
    public static void EnsureScene()
    {
        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
