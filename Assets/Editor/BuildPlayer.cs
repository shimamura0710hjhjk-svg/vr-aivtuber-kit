using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildPlayer
{
    [MenuItem("Build/Build Windows Standalone")]
    public static void BuildWindows()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("BuildPlayer: ビルド設定にシーンが追加されていません。");
            return;
        }

        string outputPath = Path.Combine("Build", "Windows", "vr-aivtuber-kit.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"Windows EXE ビルド完了: {outputPath}");
    }

    [MenuItem("Build/Build Android APK")]
    public static void BuildAndroid()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("BuildPlayer: ビルド設定にシーンが追加されていません。");
            return;
        }

        string outputPath = Path.Combine("Build", "Android", "vr-aivtuber-kit.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.Android, BuildOptions.None);
        Debug.Log($"Android APK ビルド完了: {outputPath}");
    }

    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
    }
}
