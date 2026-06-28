using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Portrait-safe target header. A warm target pill carries the visual goal;
/// the steps badge stays deliberately quieter so the collection objective
/// remains the first read, matching the reference video's hierarchy.
/// </summary>
public class TopBarView : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI stepsText;
    public GameObject targetIcon;
    public GameObject stepsIcon;

    [Header("Optional Icon Sprites")]
    public Sprite giftIconSprite;
    public Sprite stepsIconSprite;

    private GameManager _gameManager;
    private RectTransform _targetPill;
    private Image _targetPillImage;
    private readonly Color _targetPillColor = new Color(1f, 0.86f, 0.63f, 0.96f);

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
        if (GetComponentInParent<Canvas>() == null)
            CreateUI();
    }

    private void CreateUI()
    {
        GameObject canvasGO = new GameObject("TopBarCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390f, 844f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        Sprite headerSprite = GlassPanelTexture.CreateRectGlassPanel(
            716, 132, 54f,
            new Color(0.035f, 0.10f, 0.24f, 0.74f),
            0.40f, 0.18f, 0.012f);

        GameObject panel = new GameObject("TopBarPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -12f);
        panelRT.sizeDelta = new Vector2(358f, 66f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = headerSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;

        CreateTargetPill(panel.transform);
        CreateStepsBadge(panel.transform);

        GameObject shimmer = new GameObject("HeaderShimmer");
        shimmer.transform.SetParent(panel.transform, false);
        RectTransform shimmerRT = shimmer.AddComponent<RectTransform>();
        shimmerRT.anchorMin = new Vector2(0.08f, 1f);
        shimmerRT.anchorMax = new Vector2(0.92f, 1f);
        shimmerRT.pivot = new Vector2(0.5f, 1f);
        shimmerRT.anchoredPosition = new Vector2(0f, -3f);
        shimmerRT.sizeDelta = new Vector2(0f, 1f);
        Image shimmerImage = shimmer.AddComponent<Image>();
        shimmerImage.color = new Color(0.65f, 0.90f, 1f, 0.34f);
        shimmerImage.raycastTarget = false;
    }

    private void CreateTargetPill(Transform parent)
    {
        Sprite pillSprite = GlassPanelTexture.CreateRoundedRect(256, 96, 42f, Color.white);
        GameObject pill = new GameObject("TargetPill");
        pill.transform.SetParent(parent, false);
        _targetPill = pill.AddComponent<RectTransform>();
        _targetPill.anchorMin = _targetPill.anchorMax = new Vector2(0f, 0.5f);
        _targetPill.pivot = new Vector2(0f, 0.5f);
        _targetPill.anchoredPosition = new Vector2(10f, 0f);
        _targetPill.sizeDelta = new Vector2(154f, 48f);
        _targetPillImage = pill.AddComponent<Image>();
        _targetPillImage.sprite = pillSprite;
        _targetPillImage.type = Image.Type.Sliced;
        _targetPillImage.color = _targetPillColor;
        _targetPillImage.raycastTarget = false;

        GameObject glow = new GameObject("InnerGlow");
        glow.transform.SetParent(pill.transform, false);
        RectTransform glowRT = glow.AddComponent<RectTransform>();
        glowRT.anchorMin = new Vector2(0.08f, 0.70f);
        glowRT.anchorMax = new Vector2(0.92f, 0.70f);
        glowRT.sizeDelta = new Vector2(0f, 1.5f);
        Image glowImage = glow.AddComponent<Image>();
        glowImage.color = new Color(1f, 1f, 1f, 0.62f);
        glowImage.raycastTarget = false;

        if (giftIconSprite != null)
        {
            GameObject iconGO = new GameObject("TargetIcon");
            iconGO.transform.SetParent(pill.transform, false);
            RectTransform iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(28f, 0f);
            iconRT.sizeDelta = new Vector2(31f, 31f);
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = giftIconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            targetIcon = iconGO;
        }
        else
        {
            targetIcon = pill;
        }

        targetText = CreateText("TargetCount", pill.transform, new Vector2(49f, 3f), new Vector2(64f, 27f),
            "33", 23f, TextAlignmentOptions.Left, new Color(0.22f, 0.10f, 0.07f, 1f), FontStyles.Bold);
        CreateText("TargetLabel", pill.transform, new Vector2(50f, -14f), new Vector2(78f, 14f),
            "TARGET", 8.5f, TextAlignmentOptions.Left, new Color(0.36f, 0.20f, 0.14f, 0.65f), FontStyles.Bold);
    }

    private void CreateStepsBadge(Transform parent)
    {
        Sprite badgeSprite = GlassPanelTexture.CreateRoundedRect(220, 92, 40f, Color.white);
        GameObject badge = new GameObject("StepsBadge");
        badge.transform.SetParent(parent, false);
        RectTransform badgeRT = badge.AddComponent<RectTransform>();
        badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(1f, 0.5f);
        badgeRT.pivot = new Vector2(1f, 0.5f);
        badgeRT.anchoredPosition = new Vector2(-10f, 0f);
        badgeRT.sizeDelta = new Vector2(112f, 44f);
        Image badgeImage = badge.AddComponent<Image>();
        badgeImage.sprite = badgeSprite;
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = new Color(0.15f, 0.34f, 0.62f, 0.68f);
        badgeImage.raycastTarget = false;

        stepsText = CreateText("StepsCount", badge.transform, new Vector2(14f, 3f), new Vector2(46f, 25f),
            "25", 21f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);
        CreateText("StepsLabel", badge.transform, new Vector2(14f, -13f), new Vector2(58f, 13f),
            "STEPS", 8f, TextAlignmentOptions.Left, new Color(0.82f, 0.92f, 1f, 0.62f), FontStyles.Bold);

        if (stepsIconSprite != null)
        {
            GameObject iconGO = new GameObject("StepsIcon");
            iconGO.transform.SetParent(badge.transform, false);
            RectTransform iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(1f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(-22f, 0f);
            iconRT.sizeDelta = new Vector2(25f, 25f);
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = stepsIconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            stepsIcon = iconGO;
        }
        else
        {
            stepsIcon = badge;
        }
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Vector2 size,
        string value, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    public void UpdateTopBar(int suitcases, int steps)
    {
        if (targetText != null) targetText.text = suitcases.ToString();
        if (stepsText != null) stepsText.text = steps.ToString();
    }

    public Vector3 GetTargetWorldPosition()
    {
        if (targetIcon == null || Camera.main == null) return Vector3.zero;
        Vector3 screenPos = targetIcon.transform.position;
        screenPos.z = -Camera.main.transform.position.z;
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        return world;
    }

    public void Bounce()
    {
        if (_targetPill == null) return;
        StopAllCoroutines();
        StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        const float duration = 0.22f;
        float elapsed = 0f;
        Color baseTextColor = targetText != null ? targetText.color : Color.white;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);
            float scale = 1f + wave * 0.16f;
            _targetPill.localScale = new Vector3(scale, 1f + wave * 0.10f, 1f);
            if (_targetPillImage != null)
                _targetPillImage.color = Color.Lerp(_targetPillColor, new Color(1f, 0.96f, 0.72f, 1f), wave);
            if (targetText != null)
                targetText.color = Color.Lerp(baseTextColor, new Color(0.60f, 0.20f, 0.04f, 1f), wave);
            yield return null;
        }
        _targetPill.localScale = Vector3.one;
        if (_targetPillImage != null) _targetPillImage.color = _targetPillColor;
        if (targetText != null) targetText.color = baseTextColor;
    }
}
