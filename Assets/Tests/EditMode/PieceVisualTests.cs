using NUnit.Framework;
using UnityEngine;

public class PieceVisualTests
{
    [Test]
    public void Configure_CreatesShadowAndSelectionHalo()
    {
        var go = new GameObject("Piece");
        go.AddComponent<SpriteRenderer>();
        var visual = go.AddComponent<PieceVisual>();

        visual.Configure();

        Assert.IsNotNull(go.transform.Find("SoftShadow"));
        Assert.IsNotNull(go.transform.Find("SelectionHalo"));
        Assert.IsFalse(go.transform.Find("SelectionHalo").gameObject.activeSelf);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SetSelected_TogglesHaloAndScale()
    {
        var go = new GameObject("Piece");
        go.AddComponent<SpriteRenderer>();
        var visual = go.AddComponent<PieceVisual>();
        visual.Configure();

        visual.SetSelected(true);
        Assert.IsTrue(go.transform.Find("SelectionHalo").gameObject.activeSelf);
        Assert.Greater(go.transform.localScale.x, 1f);

        visual.SetSelected(false);
        Assert.IsFalse(go.transform.Find("SelectionHalo").gameObject.activeSelf);
        Assert.AreEqual(Vector3.one, go.transform.localScale);
        Object.DestroyImmediate(go);
    }
}
