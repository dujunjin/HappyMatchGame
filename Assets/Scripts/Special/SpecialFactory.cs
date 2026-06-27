using UnityEngine;
using System.Collections;

/// <summary>
/// Factory for creating special items (rockets and bombs) on the board.
/// </summary>
public class SpecialFactory
{
    private BoardController _board;
    private GameManager _gameManager;

    public void Init(BoardController board, GameManager gameManager)
    {
        _board = board;
        _gameManager = gameManager;
    }

    /// <summary>
    /// Create a rocket at the specified position.
    /// </summary>
    public IEnumerator CreateRocketAt(int row, int col, GameConfig.RocketDir dir)
    {
        if (!Helper.IsInBounds(row, col, _board.Rows, _board.Cols)) yield break;

        // Get the element color at this position
        ElementType elementType = _board.Cells[row, col].elementType;
        if (elementType == ElementType.Empty)
            elementType = GameConfig.NormalElements[Random.Range(0, GameConfig.NormalElements.Length)];

        // Remove existing element
        if (_board.Cells[row, col].gameObject != null)
        {
            Object.Destroy(_board.Cells[row, col].gameObject);
            _board.Cells[row, col].gameObject = null;
        }

        // Create rocket game object
        GameObject go = new GameObject("Rocket");
        go.transform.SetParent(_board.transform);
        go.transform.position = _board.GetWorldPosition(row, col);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _gameManager.GetSpriteForType(elementType, GameConfig.SpecialType.Rocket, dir);
        sr.sortingOrder = 2;

        CircleCollider2D collider2D = go.AddComponent<CircleCollider2D>();
        collider2D.radius = _board.cellSize * 0.45f;

        // Add RocketBehavior
        RocketBehavior rb = go.AddComponent<RocketBehavior>();
        rb.Init(_board, _gameManager, row, col, elementType, dir);

        _board.Cells[row, col].elementType = elementType;
        _board.Cells[row, col].specialType = GameConfig.SpecialType.Rocket;
        _board.Cells[row, col].rocketDir = dir;
        _board.Cells[row, col].gameObject = go;

        // Pop-in animation
        yield return AnimationHelper.PopIn(go, 0.2f);
    }

    /// <summary>
    /// Create a bomb at the specified position.
    /// </summary>
    public IEnumerator CreateBombAt(int row, int col)
    {
        if (!Helper.IsInBounds(row, col, _board.Rows, _board.Cols)) yield break;

        // Get the element color at this position
        ElementType elementType = _board.Cells[row, col].elementType;
        if (elementType == ElementType.Empty)
            elementType = GameConfig.NormalElements[Random.Range(0, GameConfig.NormalElements.Length)];

        // Remove existing element
        if (_board.Cells[row, col].gameObject != null)
        {
            Object.Destroy(_board.Cells[row, col].gameObject);
            _board.Cells[row, col].gameObject = null;
        }

        // Create bomb game object
        GameObject go = new GameObject("Bomb");
        go.transform.SetParent(_board.transform);
        go.transform.position = _board.GetWorldPosition(row, col);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _gameManager.GetSpriteForType(elementType, GameConfig.SpecialType.Bomb);
        sr.sortingOrder = 2;

        CircleCollider2D c = go.AddComponent<CircleCollider2D>();
        c.radius = _board.cellSize * 0.45f;

        // Add BombBehavior
        BombBehavior bb = go.AddComponent<BombBehavior>();
        bb.Init(_board, _gameManager, row, col, elementType);

        _board.Cells[row, col].elementType = elementType;
        _board.Cells[row, col].specialType = GameConfig.SpecialType.Bomb;
        _board.Cells[row, col].gameObject = go;

        // Pop-in animation
        yield return AnimationHelper.PopIn(go, 0.2f);
    }
}
