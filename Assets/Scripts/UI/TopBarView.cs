using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top bar UI showing target (suitcase) count and remaining steps.
/// </summary>
public class TopBarView : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI stepsText;
    public GameObject targetIcon;
    public GameObject stepsIcon;

    private GameManager _gameManager;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;

        // Create Canvas if not exists
        if (GetComponentInParent<Canvas>() == null)
        {
            CreateUI();
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("TopBarCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        // Background panel
        GameObject panel = new GameObject("TopBarPanel");
        panel.transform.SetParent(canvasGO.transform);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -10f);
        panelRT.sizeDelta = new Vector2(0f, 100f);
        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.5f);

        // Target text
        GameObject targetGO = new GameObject("TargetText");
        targetGO.transform.SetParent(panel.transform);
        RectTransform targetRT = targetGO.AddComponent<RectTransform>();
        targetRT.anchorMin = new Vector2(0.5f, 0.5f);
        targetRT.anchorMax = new Vector2(0.5f, 0.5f);
        targetRT.pivot = new Vector2(0.5f, 0.5f);
        targetRT.anchoredPosition = new Vector2(-150f, -20f);
        targetRT.sizeDelta = new Vector2(200f, 60f);
        targetText = targetGO.AddComponent<TextMeshProUGUI>();
        targetText.fontSize = 36;
        targetText.alignment = TextAlignmentOptions.Center;
        targetText.color = Color.white;
        targetText.text = "Suitcase: 33";

        // Steps text
        GameObject stepsGO = new GameObject("StepsText");
        stepsGO.transform.SetParent(panel.transform);
        RectTransform stepsRT = stepsGO.AddComponent<RectTransform>();
        stepsRT.anchorMin = new Vector2(0.5f, 0.5f);
        stepsRT.anchorMax = new Vector2(0.5f, 0.5f);
        stepsRT.pivot = new Vector2(0.5f, 0.5f);
        stepsRT.anchoredPosition = new Vector2(150f, -20f);
        stepsRT.sizeDelta = new Vector2(200f, 60f);
        stepsText = stepsGO.AddComponent<TextMeshProUGUI>();
        stepsText.fontSize = 36;
        stepsText.alignment = TextAlignmentOptions.Center;
        stepsText.color = Color.white;
        stepsText.text = "Steps: 25";

        // Store references
        targetIcon = targetGO;
        stepsIcon = stepsGO;
    }

    public void UpdateTopBar(int suitcases, int steps)
    {
        if (targetText != null)
            targetText.text = $"Suitcase: {suitcases}";
        if (stepsText != null)
            stepsText.text = $"Steps: {steps}";
    }

    /// <summary>
    /// World-space position of the target icon, for flyers to home in on.
    /// The TopBar lives on a ScreenSpaceOverlay canvas whose transform
    /// positions are in screen pixels, so convert through the camera.
    /// </summary>
    public Vector3 GetTargetWorldPosition()
    {
        if (targetIcon == null) return Vector3.zero;
        Vector3 screenPos = targetIcon.transform.position;
        // Distance from camera (at z=-10) to the z=0 plane is 10.
        screenPos.z = -Camera.main.transform.position.z;
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
        world.z = 0;
        return world;
    }

    /// <summary>
    /// Brief 0.16s scale pop on the target icon + number when a flyer lands,
    /// per spec §5.2.4. Runs as a coroutine; re-triggering restarts it.
    /// </summary>
    public void Bounce()
    {
        if (targetIcon == null) return;
        StopAllCoroutines();
        StartCoroutine(BounceRoutine());
    }

    private System.Collections.IEnumerator BounceRoutine()
    {
        const float duration = 0.16f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 1 -> 1.25 -> 1 via sine.
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            if (targetIcon != null) targetIcon.transform.localScale = Vector3.one * s;
            if (targetText != null) targetText.transform.localScale = Vector3.one * s;
            yield return null;
        }
        if (targetIcon != null) targetIcon.transform.localScale = Vector3.one;
        if (targetText != null) targetText.transform.localScale = Vector3.one;
    }
}
