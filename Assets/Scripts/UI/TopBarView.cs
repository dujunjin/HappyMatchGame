using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top bar UI showing target (suitcase) count and remaining steps.
/// Uses Apple-style glassmorphism panel with frosted glass appearance,
/// rounded corners, and typographic hierarchy.
/// Canvas reference resolution matches PhoneFrame (390×844) for consistent layout.
/// </summary>
public class TopBarView : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI stepsText;
    public GameObject targetIcon;
    public GameObject stepsIcon;

    [Header("Optional Icon Sprites (leave null for no icons)")]
    public Sprite giftIconSprite;
    public Sprite stepsIconSprite;

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
        // Create Canvas — same reference resolution as PhoneFrame for alignment
        GameObject canvasGO = new GameObject("TopBarCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight = 0f; // match width (portrait, same as PhoneFrame)
        GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        // ── Glassmorphism background panel ──
        // Frosted glass: semi-transparent white with gradient, border, rounded corners.
        // Uses rectangular texture at the TopBar's actual aspect ratio (~6.5:1)
        // so rounded corners are never stretched.
        // Position: anchored from top of screen, moved higher per user request.
        Sprite glassSprite = GlassPanelTexture.CreateRectGlassPanel(
            448, 80, 28f,
            new Color(1f, 1f, 1f, 0.15f),   // more transparent glass fill
            0.28f,   // border highlight
            0.12f,   // top glow
            0.030f   // stronger noise for fake blur
        );

        GameObject panel = new GameObject("TopBarPanel");
        panel.transform.SetParent(canvasGO.transform);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(1f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -12f); // moved up to clear board
        panelRT.sizeDelta = new Vector2(-24f, 56f); // 12px margin each side, 56px tall (matches HTML)
        UnityEngine.UI.Image panelImg = panel.AddComponent<UnityEngine.UI.Image>();
        panelImg.sprite = glassSprite;
        panelImg.type = Image.Type.Sliced;
        panelImg.color = Color.white;

        // ── Target side (left: icon + count + label) ──
        // Target gift icon
        if (giftIconSprite != null)
        {
            GameObject targetIconGO = new GameObject("TargetIcon");
            targetIconGO.transform.SetParent(panel.transform);
            RectTransform targetIconRT = targetIconGO.AddComponent<RectTransform>();
            targetIconRT.anchorMin = new Vector2(0f, 0.5f);
            targetIconRT.anchorMax = new Vector2(0f, 0.5f);
            targetIconRT.pivot = new Vector2(0.5f, 0.5f);
            targetIconRT.anchoredPosition = new Vector2(28f, 0f);
            targetIconRT.sizeDelta = new Vector2(28f, 28f);
            UnityEngine.UI.Image targetImg = targetIconGO.AddComponent<UnityEngine.UI.Image>();
            targetImg.sprite = giftIconSprite;
            targetImg.preserveAspect = true;
        }

        // Target count (big number)
        GameObject targetCountGO = new GameObject("TargetCount");
        targetCountGO.transform.SetParent(panel.transform);
        RectTransform targetCountRT = targetCountGO.AddComponent<RectTransform>();
        targetCountRT.anchorMin = new Vector2(0f, 0.5f);
        targetCountRT.anchorMax = new Vector2(0f, 0.5f);
        targetCountRT.pivot = new Vector2(0f, 0.5f);
        targetCountRT.anchoredPosition = new Vector2(48f, 2f);
        targetCountRT.sizeDelta = new Vector2(60f, 28f);
        targetText = targetCountGO.AddComponent<TextMeshProUGUI>();
        targetText.fontSize = 20;
        targetText.fontStyle = FontStyles.Bold;
        targetText.alignment = TextAlignmentOptions.Left;
        targetText.color = new Color(1f, 1f, 1f, 0.95f);
        targetText.text = "33";

        // Target label (small text under count)
        GameObject targetLabelGO = new GameObject("TargetLabel");
        targetLabelGO.transform.SetParent(panel.transform);
        RectTransform targetLabelRT = targetLabelGO.AddComponent<RectTransform>();
        targetLabelRT.anchorMin = new Vector2(0f, 0.5f);
        targetLabelRT.anchorMax = new Vector2(0f, 0.5f);
        targetLabelRT.pivot = new Vector2(0f, 0.5f);
        targetLabelRT.anchoredPosition = new Vector2(48f, -14f);
        targetLabelRT.sizeDelta = new Vector2(60f, 14f);
        TextMeshProUGUI targetLabel = targetLabelGO.AddComponent<TextMeshProUGUI>();
        targetLabel.fontSize = 9;
        targetLabel.fontStyle = FontStyles.Bold;
        targetLabel.alignment = TextAlignmentOptions.Left;
        targetLabel.color = new Color(1f, 1f, 1f, 0.5f);
        targetLabel.text = "GIFTS";

        // ── Steps side (right: label + count + icon) ──
        // Steps count
        GameObject stepsCountGO = new GameObject("StepsCount");
        stepsCountGO.transform.SetParent(panel.transform);
        RectTransform stepsCountRT = stepsCountGO.AddComponent<RectTransform>();
        stepsCountRT.anchorMin = new Vector2(1f, 0.5f);
        stepsCountRT.anchorMax = new Vector2(1f, 0.5f);
        stepsCountRT.pivot = new Vector2(1f, 0.5f);
        stepsCountRT.anchoredPosition = new Vector2(-48f, 2f);
        stepsCountRT.sizeDelta = new Vector2(60f, 28f);
        stepsText = stepsCountGO.AddComponent<TextMeshProUGUI>();
        stepsText.fontSize = 20;
        stepsText.fontStyle = FontStyles.Bold;
        stepsText.alignment = TextAlignmentOptions.Right;
        stepsText.color = new Color(1f, 1f, 1f, 0.95f);
        stepsText.text = "25";

        // Steps label
        GameObject stepsLabelGO = new GameObject("StepsLabel");
        stepsLabelGO.transform.SetParent(panel.transform);
        RectTransform stepsLabelRT = stepsLabelGO.AddComponent<RectTransform>();
        stepsLabelRT.anchorMin = new Vector2(1f, 0.5f);
        stepsLabelRT.anchorMax = new Vector2(1f, 0.5f);
        stepsLabelRT.pivot = new Vector2(1f, 0.5f);
        stepsLabelRT.anchoredPosition = new Vector2(-48f, -14f);
        stepsLabelRT.sizeDelta = new Vector2(60f, 14f);
        TextMeshProUGUI stepsLabel = stepsLabelGO.AddComponent<TextMeshProUGUI>();
        stepsLabel.fontSize = 9;
        stepsLabel.fontStyle = FontStyles.Bold;
        stepsLabel.alignment = TextAlignmentOptions.Right;
        stepsLabel.color = new Color(1f, 1f, 1f, 0.5f);
        stepsLabel.text = "STEPS";

        // Steps icon
        if (stepsIconSprite != null)
        {
            GameObject stepsIconGO = new GameObject("StepsIcon");
            stepsIconGO.transform.SetParent(panel.transform);
            RectTransform stepsIconRT = stepsIconGO.AddComponent<RectTransform>();
            stepsIconRT.anchorMin = new Vector2(1f, 0.5f);
            stepsIconRT.anchorMax = new Vector2(1f, 0.5f);
            stepsIconRT.pivot = new Vector2(0.5f, 0.5f);
            stepsIconRT.anchoredPosition = new Vector2(-28f, 0f);
            stepsIconRT.sizeDelta = new Vector2(24f, 24f);
            UnityEngine.UI.Image stepsImg = stepsIconGO.AddComponent<UnityEngine.UI.Image>();
            stepsImg.sprite = stepsIconSprite;
            stepsImg.preserveAspect = true;
        }

        // Store references for TargetPresentation flyers
        targetIcon = giftIconSprite != null ? panel.transform.Find("TargetIcon")?.gameObject : targetCountGO;
        stepsIcon = stepsCountGO;
    }

    public void UpdateTopBar(int suitcases, int steps)
    {
        if (targetText != null)
            targetText.text = $"{suitcases}";
        if (stepsText != null)
            stepsText.text = $"{steps}";
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
