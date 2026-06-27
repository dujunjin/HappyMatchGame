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
}
