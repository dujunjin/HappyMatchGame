using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class HappyMatchAcceptanceBuild
{
    public static void BuildMac()
    {
        string[] args = Environment.GetCommandLineArgs();
        string output = Path.Combine(Path.GetTempPath(), "HappyMatchAcceptance", "HappyMatchGame.app");
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == "-happyMatchBuildPath") output = args[i + 1];

        Directory.CreateDirectory(Path.GetDirectoryName(output));
        PlayerSettings.defaultScreenWidth = 820;
        PlayerSettings.defaultScreenHeight = 1022;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = false;

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Happy Match acceptance build failed: " + report.summary.result);

        Debug.Log("[HappyMatchAcceptanceBuild] built: " + output);
    }
}
