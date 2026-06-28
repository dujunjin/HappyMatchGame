using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores the saved playable scene when batch tests leave the Editor in a
/// disposable untitled recovery scene. Runtime Bootstrap is the second line
/// of defence for Play Mode and standalone launches.
/// </summary>
[InitializeOnLoad]
public static class HappyMatchSceneGuard
{
    public const string PlayableScenePath = "Assets/Scenes/SampleScene.unity";

    static HappyMatchSceneGuard()
    {
        EditorApplication.delayCall += RestorePlayableSceneIfNeeded;
    }

    [MenuItem("HappyMatch/Open Playable Scene", priority = 0)]
    public static void OpenPlayableScene()
    {
        if (!File.Exists(PlayableScenePath))
        {
            Debug.LogError("[HappyMatchSceneGuard] Missing playable scene: " + PlayableScenePath);
            return;
        }

        EditorSceneManager.OpenScene(PlayableScenePath, OpenSceneMode.Single);
    }

    private static void RestorePlayableSceneIfNeeded()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsDisposableUntitledScene(activeScene)) return;

        OpenPlayableScene();
        Debug.Log("[HappyMatchSceneGuard] Restored SampleScene after empty editor recovery state.");
    }

    private static bool IsDisposableUntitledScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || !string.IsNullOrEmpty(scene.path)) return false;

        GameObject[] roots = scene.GetRootGameObjects();
        if (roots.Length == 0) return true;
        return roots.Length == 1 && roots[0] != null && roots[0].name == "Main Camera";
    }
}
