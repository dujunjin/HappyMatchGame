using UnityEngine;

/// <summary>
/// Stable scene entry point. Place a single Bootstrap GameObject in the
/// SampleScene; on Awake it guarantees a GameManager exists so the scene
/// plays correctly straight from disk without relying on unsaved Hierarchy
/// objects. Runs before GameManager.Start via [DefaultExecutionOrder].
///
/// If a GameManager is already present (e.g. the user kept one in the
/// scene), Bootstrap leaves it alone. Otherwise it creates one on a fresh
/// GameObject so the rest of the bootstrap chain (BoardController, systems,
/// UI) assembles exactly as it does in GameManager.Start.
/// </summary>
[DefaultExecutionOrder(-100)]
public class Bootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Ensure an orthographic 2D camera exists; the SampleScene ships with
        // one tagged MainCamera, but a freshly-created scene might not.
        if (Camera.main == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.102f, 0.102f, 0.18f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGO.AddComponent<AudioListener>();
        }

        // Guarantee GameManager.
        if (FindObjectOfType<GameManager>() == null)
        {
            GameObject go = new GameObject("GameManager");
            GameManager gm = go.AddComponent<GameManager>();
            // GameManager.Start() wires every other system from here.
            gm.levelConfig = LevelConfig.Default;
        }
    }
}
