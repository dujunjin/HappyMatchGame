using UnityEngine;
using TMPro;

/// <summary>
/// Win/Lose result overlay dialog.
/// </summary>
public class ResultDialog : MonoBehaviour
{
    private GameManager _gameManager;
    private GameObject _dialogRoot;

    public void Init(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void Show(bool won)
    {
        if (_dialogRoot != null)
        {
            Object.Destroy(_dialogRoot);
        }

        // Dedicated overlay canvas so the dialog is always centered over the
        // board and rendered on top, independent of any other canvas's scaler
        // settings (the shared TopBarCanvas uses a portrait reference, which
        // shifts content off-center on a landscape screen).
        _dialogRoot = new GameObject("ResultDialogCanvas");
        Canvas canvas = _dialogRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        UnityEngine.UI.CanvasScaler scaler = _dialogRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _dialogRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        RectTransform rootRT = _dialogRoot.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;

        // Full-screen dim panel. Use SetParent(parent, false) so the
        // RectTransform anchors (not world position) control layout — using
        // the default (true) is what left the dialog shifted to the lower-left.
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(_dialogRoot.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        UnityEngine.UI.Image img = panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);

        // Center text
        GameObject textGO = new GameObject("ResultText");
        textGO.transform.SetParent(panel.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(600f, 200f);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 64;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = won ? Color.green : Color.red;
        tmp.text = won ? "You Win!" : "Game Over";

        // Sub text
        GameObject subGO = new GameObject("SubText");
        subGO.transform.SetParent(panel.transform, false);
        RectTransform subRT = subGO.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.5f);
        subRT.anchorMax = new Vector2(0.5f, 0.5f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.anchoredPosition = new Vector2(0f, -80f);
        subRT.sizeDelta = new Vector2(600f, 100f);
        TextMeshProUGUI subTmp = subGO.AddComponent<TextMeshProUGUI>();
        subTmp.fontSize = 28;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = Color.white;
        subTmp.text = won
            ? $"All suitcases cleared!"
            : $"Out of steps!\nSuitcases left: {_gameManager.RemainingSuitcases}";

        // Restart button
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(panel.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0f, -170f);
        btnRT.sizeDelta = new Vector2(300f, 70f);
        UnityEngine.UI.Image btnImg = btnGO.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f);
        UnityEngine.UI.Button btn = btnGO.AddComponent<UnityEngine.UI.Button>();

        GameObject btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnTmp = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnTmp.fontSize = 32;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
        btnTmp.text = "Restart";

        btn.onClick.AddListener(() =>
        {
            Object.Destroy(_dialogRoot);
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });
    }

    public void Hide()
    {
        if (_dialogRoot != null)
        {
            Object.Destroy(_dialogRoot);
            _dialogRoot = null;
        }
    }
}
