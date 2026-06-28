using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates an iPhone 15 Pro-style frame overlay on a ScreenSpaceOverlay canvas.
/// Includes Dynamic Island, status bar (time + signal + battery), and home indicator.
/// Sorting order 90 — above TopBar (10) and VFX (18-21), below result dialog (100)
/// and win sequence (200).
/// </summary>
public class PhoneFrame : MonoBehaviour
{
    private Canvas _canvas;
    private Sprite _roundedRectSprite;
    private TextMeshProUGUI _timeText;

    private void Start()
    {
        CreateFrame();
    }

    private void CreateFrame()
    {
        // Shared rounded-rect sprite (9-slice, can stretch to any size)
        _roundedRectSprite = GlassPanelTexture.CreateRoundedRect(64, 64, 16, Color.white);

        // Canvas
        GameObject canvasGO = new GameObject("PhoneFrameCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight = 0f; // scale by width (portrait)
        canvasGO.AddComponent<GraphicRaycaster>();
        _canvas = canvas;

        CreateDynamicIsland();
        CreateStatusBar();
        CreateHomeIndicator();
    }

    /// <summary>
    /// Dynamic Island: black pill at top center (126×37, 11px from top).
    /// </summary>
    private void CreateDynamicIsland()
    {
        GameObject go = new GameObject("DynamicIsland");
        go.transform.SetParent(_canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -11f);
        rt.sizeDelta = new Vector2(126f, 37f);

        Image img = go.AddComponent<Image>();
        img.sprite = _roundedRectSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// Status bar: time text on left, signal+wifi+battery icons on right.
    /// Height ~54px, padding 18px from sides.
    /// </summary>
    private void CreateStatusBar()
    {
        // Time text (left side)
        GameObject timeGO = new GameObject("StatusTime");
        timeGO.transform.SetParent(_canvas.transform, false);

        RectTransform timeRT = timeGO.AddComponent<RectTransform>();
        timeRT.anchorMin = new Vector2(0f, 1f);
        timeRT.anchorMax = new Vector2(0f, 1f);
        timeRT.pivot = new Vector2(0f, 0.5f);
        timeRT.anchoredPosition = new Vector2(32f, -27f);
        timeRT.sizeDelta = new Vector2(60f, 20f);

        _timeText = timeGO.AddComponent<TextMeshProUGUI>();
        _timeText.text = "9:41";
        _timeText.fontSize = 15;
        _timeText.fontStyle = FontStyles.Bold;
        _timeText.alignment = TextAlignmentOptions.Left;
        _timeText.color = new Color(1f, 1f, 1f, 0.9f);

        // Signal bars (right side, leftmost of the three icons)
        CreateSignalBars(new Vector2(-78f, -22f));

        // WiFi icon (right side, middle)
        CreateWiFiIcon(new Vector2(-50f, -22f));

        // Battery (right side, rightmost)
        CreateBatteryIcon(new Vector2(-22f, -22f));
    }

    /// <summary>
    /// Signal: 4 bars of increasing height.
    /// </summary>
    private void CreateSignalBars(Vector2 offset)
    {
        GameObject container = new GameObject("Signal");
        container.transform.SetParent(_canvas.transform, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(24f, 12f);

        float[] heights = { 4f, 6f, 8f, 11f };
        float barWidth = 3f;
        float gap = 1f;

        for (int i = 0; i < 4; i++)
        {
            GameObject bar = new GameObject($"Bar{i}");
            bar.transform.SetParent(container.transform, false);

            RectTransform barRT = bar.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0f, 0f);
            barRT.anchorMax = new Vector2(0f, 0f);
            barRT.pivot = new Vector2(0f, 0f);
            barRT.anchoredPosition = new Vector2(i * (barWidth + gap), 0f);
            barRT.sizeDelta = new Vector2(barWidth, heights[i]);

            Image barImg = bar.AddComponent<Image>();
            barImg.color = new Color(1f, 1f, 1f, 0.9f);
        }
    }

    /// <summary>
    /// WiFi: 3 concentric arcs (simplified as 3 dots of decreasing size going up).
    /// </summary>
    private void CreateWiFiIcon(Vector2 offset)
    {
        GameObject container = new GameObject("WiFi");
        container.transform.SetParent(_canvas.transform, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(16f, 12f);

        // Draw a simple wifi shape using 3 small circles
        float[] sizes = { 3f, 2.2f, 1.5f };
        float[] yOffsets = { 0f, 4f, 8f };

        for (int i = 0; i < 3; i++)
        {
            GameObject dot = new GameObject($"Dot{i}");
            dot.transform.SetParent(container.transform, false);

            RectTransform dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0.5f, 0f);
            dotRT.anchorMax = new Vector2(0.5f, 0f);
            dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(0f, yOffsets[i]);
            dotRT.sizeDelta = Vector2.one * sizes[i];

            Image dotImg = dot.AddComponent<Image>();
            dotImg.sprite = _roundedRectSprite;
            dotImg.type = Image.Type.Sliced;
            dotImg.color = new Color(1f, 1f, 1f, 0.9f);
        }
    }

    /// <summary>
    /// Battery: rounded rect outline + fill bar.
    /// </summary>
    private void CreateBatteryIcon(Vector2 offset)
    {
        GameObject container = new GameObject("Battery");
        container.transform.SetParent(_canvas.transform, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(27f, 12f);

        // Battery outline
        GameObject outline = new GameObject("BatteryOutline");
        outline.transform.SetParent(container.transform, false);

        RectTransform outlineRT = outline.AddComponent<RectTransform>();
        outlineRT.anchorMin = Vector2.zero;
        outlineRT.anchorMax = Vector2.one;
        outlineRT.offsetMin = new Vector2(1f, 1f);
        outlineRT.offsetMax = new Vector2(-3f, -1f);

        Image outlineImg = outline.AddComponent<Image>();
        outlineImg.sprite = _roundedRectSprite;
        outlineImg.type = Image.Type.Sliced;
        outlineImg.color = new Color(1f, 1f, 1f, 0.35f);

        // Battery fill (green-ish, 80% full)
        GameObject fill = new GameObject("BatteryFill");
        fill.transform.SetParent(outline.transform, false);

        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.zero;
        fillRT.pivot = new Vector2(0f, 0.5f);
        fillRT.anchoredPosition = new Vector2(1.5f, 0f);
        fillRT.sizeDelta = new Vector2(19f, 8f);

        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = _roundedRectSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(1f, 1f, 1f, 0.9f);

        // Battery nub (small bump on right side)
        GameObject nub = new GameObject("BatteryNub");
        nub.transform.SetParent(container.transform, false);

        RectTransform nubRT = nub.AddComponent<RectTransform>();
        nubRT.anchorMin = new Vector2(1f, 0.5f);
        nubRT.anchorMax = new Vector2(1f, 0.5f);
        nubRT.pivot = new Vector2(1f, 0.5f);
        nubRT.anchoredPosition = new Vector2(0f, 0f);
        nubRT.sizeDelta = new Vector2(2f, 4f);

        Image nubImg = nub.AddComponent<Image>();
        nubImg.color = new Color(1f, 1f, 1f, 0.35f);
    }

    /// <summary>
    /// Home indicator: white semi-transparent pill at bottom center (134×5, 8px from bottom).
    /// </summary>
    private void CreateHomeIndicator()
    {
        GameObject go = new GameObject("HomeIndicator");
        go.transform.SetParent(_canvas.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 8f);
        rt.sizeDelta = new Vector2(134f, 5f);

        Image img = go.AddComponent<Image>();
        img.sprite = _roundedRectSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.4f);
    }

    private void Update()
    {
        // Update status bar time to real device time
        if (_timeText != null)
        {
            _timeText.text = System.DateTime.Now.ToString("H:mm");
        }
    }
}
