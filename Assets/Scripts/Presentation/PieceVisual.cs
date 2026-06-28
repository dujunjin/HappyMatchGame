using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight visual dressing attached to every board piece. The logical
/// cell remains on the root; shadow and halo follow every swap/fall for free.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class PieceVisual : MonoBehaviour
{
    private static Sprite _haloSprite;

    private SpriteRenderer _main;
    private SpriteRenderer _shadow;
    private GameObject _haloObject;
    private SpriteRenderer _halo;
    private Coroutine _selectionRoutine;

    public void Configure()
    {
        _main = GetComponent<SpriteRenderer>();

        Transform shadowTransform = transform.Find("SoftShadow");
        if (shadowTransform == null)
        {
            var shadowObject = new GameObject("SoftShadow");
            shadowObject.transform.SetParent(transform, false);
            shadowTransform = shadowObject.transform;
        }
        shadowTransform.localPosition = new Vector3(0.035f, -0.055f, 0f);
        shadowTransform.localScale = Vector3.one * 1.03f;
        _shadow = shadowTransform.GetComponent<SpriteRenderer>();
        if (_shadow == null) _shadow = shadowTransform.gameObject.AddComponent<SpriteRenderer>();
        _shadow.color = new Color(0.015f, 0.025f, 0.08f, 0.42f);

        Transform haloTransform = transform.Find("SelectionHalo");
        if (haloTransform == null)
        {
            _haloObject = new GameObject("SelectionHalo");
            _haloObject.transform.SetParent(transform, false);
            haloTransform = _haloObject.transform;
        }
        else
        {
            _haloObject = haloTransform.gameObject;
        }

        haloTransform.localPosition = Vector3.zero;
        haloTransform.localScale = Vector3.one * 1.14f;
        _halo = haloTransform.GetComponent<SpriteRenderer>();
        if (_halo == null) _halo = haloTransform.gameObject.AddComponent<SpriteRenderer>();
        if (_haloSprite == null)
            _haloSprite = SpriteGenerator.CreateCircleSprite(Color.white, 96);
        _halo.sprite = _haloSprite;
        _halo.color = new Color(0.40f, 0.90f, 1f, 0.34f);

        SetSprite(_main.sprite);
        SetSortingOrder(_main.sortingOrder);
        _haloObject.SetActive(false);
    }

    public void SetSprite(Sprite sprite)
    {
        if (_main == null) _main = GetComponent<SpriteRenderer>();
        _main.sprite = sprite;
        if (_shadow != null) _shadow.sprite = sprite;
    }

    public void SetSortingOrder(int order)
    {
        if (_main == null) _main = GetComponent<SpriteRenderer>();
        _main.sortingOrder = order;
        if (_shadow != null) _shadow.sortingOrder = order - 1;
        if (_halo != null) _halo.sortingOrder = order - 2;
    }

    public void SetSelected(bool selected)
    {
        if (_haloObject == null) Configure();
        if (_selectionRoutine != null)
        {
            StopCoroutine(_selectionRoutine);
            _selectionRoutine = null;
        }

        _haloObject.SetActive(selected);
        if (!Application.isPlaying)
        {
            transform.localScale = selected ? Vector3.one * 1.07f : Vector3.one;
            return;
        }

        if (selected)
            _selectionRoutine = StartCoroutine(SelectIn());
        else
            transform.localScale = Vector3.one;
    }

    private IEnumerator SelectIn()
    {
        Vector3 from = transform.localScale;
        const float duration = 0.10f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = PolishMotion.EaseOutBack(elapsed / duration);
            transform.localScale = Vector3.LerpUnclamped(from, Vector3.one * 1.07f, t);
            yield return null;
        }
        transform.localScale = Vector3.one * 1.07f;
        _selectionRoutine = null;
    }
}
