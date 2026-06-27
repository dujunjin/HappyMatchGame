using UnityEngine;

/// <summary>
/// Phase F: runtime screenshot helper. Press F12 during play to capture the
/// current frame to Application.persistentDataPath (file name has a timestamp
/// so repeated captures don't overwrite). For the spec's 820×1022 reference
/// resolution, set the Game view's Fixed Resolution to 820×1022 before
/// capturing (see Document/ACCEPTANCE_CHECKLIST.md §8).
/// </summary>
public class ScreenshotCapture : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string path = System.IO.Path.Combine(Application.persistentDataPath, "hm_" + stamp + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[ScreenshotCapture] saved: " + path);
        }
    }
}
