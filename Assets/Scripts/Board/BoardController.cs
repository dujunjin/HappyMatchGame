using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the board grid data and visual representation.
/// Owns the CellData[,] array and creates/positions element GameObjects.
/// </summary>
public class BoardController : MonoBehaviour
{
    [Header("Board Settings")]
    public float cellSize = 0.7f;
    public float cellGap = 0.1f;
    public Vector3 boardOrigin = new Vector3(0f, -0.5f, 0f);

    [Header("References")]
    public GameManager gameManager;

    private CellData[,] _cells;
    private int _rows;
    private int _cols;
    private GameObject _boardRoot;

    public CellData[,] Cells => _cells;
    public int Rows => _rows;
    public int Cols => _cols;

    public event System.Action<int, int> OnCellChanged;

    private void Awake()
    {
        _rows = GameConfig.BoardHeight;
        _cols = GameConfig.BoardWidth;
        _cells = new CellData[_rows, _cols];

        // Center the board in the camera view so all rows/cols are visible.
        float spacing = cellSize + cellGap;
        boardOrigin = new Vector3(
            -(_cols - 1) * spacing * 0.5f,
            (_rows - 1) * spacing * 0.5f,
            0f
        );
    }

    // NOTE: InitializeBoard() is called explicitly by GameManager.Start()
    // (not from Unity's Start()) so that suitcases are placed AFTER the board
    // is populated. BoardController is created at runtime via AddComponent,
    // so its Start() would otherwise run next frame — after PlaceInitialSuitcases.

    /// <summary>
    /// Initialize the board with random elements, avoiding initial matches.
    /// </summary>
    public void InitializeBoard()
    {
        _boardRoot = new GameObject("BoardRoot");
        _boardRoot.transform.SetParent(transform);

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                ElementType type;
                do
                {
                    type = GameConfig.NormalElements[Random.Range(0, GameConfig.NormalElements.Length)];
                } while (WouldMatch(row, col, type));

                CreateCell(row, col, type);
            }
        }
    }

    private bool WouldMatch(int row, int col, ElementType type)
    {
        // Check horizontal (left 2)
        if (col >= 2 &&
            _cells[row, col - 1].elementType == type &&
            _cells[row, col - 2].elementType == type)
            return true;

        // Check vertical (up 2)
        if (row >= 2 &&
            _cells[row - 1, col].elementType == type &&
            _cells[row - 2, col].elementType == type)
            return true;

        return false;
    }

    private void CreateCell(int row, int col, ElementType type)
    {
        // Clear old cell object if exists
        if (_cells[row, col].gameObject != null)
        {
            Object.Destroy(_cells[row, col].gameObject);
        }

        CellData cell = new CellData
        {
            row = row,
            col = col,
            elementType = type,
            gameObject = null,
            specialType = GameConfig.SpecialType.None
        };

        Vector3 worldPos = cell.WorldPosition(cellSize, cellGap, boardOrigin);

        GameObject go = new GameObject($"Cell_{row}_{col}");
        go.transform.SetParent(_boardRoot.transform);
        go.transform.position = worldPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite sprite = gameManager.GetSpriteForType(type);
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        // Add collider for input
        CircleCollider2D collider2D = go.AddComponent<CircleCollider2D>();
        collider2D.radius = cellSize * 0.45f;

        cell.gameObject = go;
        _cells[row, col] = cell;

        OnCellChanged?.Invoke(row, col);
    }

    /// <summary>
    /// Update a cell's type and visual.
    /// </summary>
    public void SetCellType(int row, int col, ElementType type, GameConfig.SpecialType special = GameConfig.SpecialType.None, GameConfig.RocketDir rocketDir = GameConfig.RocketDir.Horizontal)
    {
        if (!Helper.IsInBounds(row, col, _rows, _cols)) return;

        _cells[row, col].elementType = type;
        _cells[row, col].specialType = special;
        _cells[row, col].rocketDir = rocketDir;

        if (_cells[row, col].gameObject != null)
        {
            SpriteRenderer sr = _cells[row, col].gameObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = gameManager.GetSpriteForType(type, special, rocketDir);
            }
        }

        OnCellChanged?.Invoke(row, col);
    }

    /// <summary>
    /// Set the world position of a cell's GameObject (for animation).
    /// </summary>
    public void SetCellPosition(int row, int col, Vector3 worldPos)
    {
        if (!Helper.IsInBounds(row, col, _rows, _cols)) return;
        if (_cells[row, col].gameObject != null)
        {
            _cells[row, col].gameObject.transform.position = worldPos;
        }
    }

    /// <summary>
    /// Get world position for a grid cell.
    /// </summary>
    public Vector3 GetWorldPosition(int row, int col)
    {
        CellData cell = new CellData { row = row, col = col };
        return cell.WorldPosition(cellSize, cellGap, boardOrigin);
    }

    /// <summary>
    /// Convert world position to grid coordinates.
    /// </summary>
    public (int row, int col) WorldToGrid(Vector3 worldPos)
    {
        return CellData.FromWorld(worldPos, cellSize, cellGap, boardOrigin);
    }

    /// <summary>
    /// Destroy a cell's GameObject and mark it empty.
    /// </summary>
    public void DestroyCell(int row, int col)
    {
        if (!Helper.IsInBounds(row, col, _rows, _cols)) return;

        if (_cells[row, col].gameObject != null)
        {
            Object.Destroy(_cells[row, col].gameObject);
            _cells[row, col].gameObject = null;
        }
        _cells[row, col].elementType = ElementType.Empty;
        _cells[row, col].specialType = GameConfig.SpecialType.None;
        OnCellChanged?.Invoke(row, col);
    }

    /// <summary>
    /// Create a falling element at a specific world position (for refill).
    /// </summary>
    public GameObject CreateFallingElement(ElementType type, Vector3 startPos, GameConfig.SpecialType special = GameConfig.SpecialType.None, GameConfig.RocketDir rocketDir = GameConfig.RocketDir.Horizontal)
    {
        GameObject go = new GameObject("FallingElement");
        go.transform.position = startPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = gameManager.GetSpriteForType(type, special, rocketDir);
        sr.sortingOrder = 1;

        CircleCollider2D collider2D = go.AddComponent<CircleCollider2D>();
        collider2D.radius = cellSize * 0.45f;

        return go;
    }

    /// <summary>
    /// Place a falling element into a cell.
    /// </summary>
    public void PlaceElementInCell(int row, int col, GameObject go, ElementType type, GameConfig.SpecialType special = GameConfig.SpecialType.None, GameConfig.RocketDir rocketDir = GameConfig.RocketDir.Horizontal)
    {
        if (!Helper.IsInBounds(row, col, _rows, _cols)) return;

        if (_cells[row, col].gameObject != null)
        {
            Object.Destroy(_cells[row, col].gameObject);
        }

        go.transform.position = GetWorldPosition(row, col);
        go.name = $"Cell_{row}_{col}";

        _cells[row, col] = new CellData
        {
            row = row,
            col = col,
            elementType = type,
            gameObject = go,
            specialType = special,
            rocketDir = rocketDir
        };

        OnCellChanged?.Invoke(row, col);
    }

    /// <summary>
    /// Get all empty cells sorted top-to-bottom, left-to-right within each row.
    /// </summary>
    public System.Collections.Generic.List<(int row, int col)> GetEmptyCells()
    {
        var empty = new System.Collections.Generic.List<(int, int)>();
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                if (_cells[row, col].IsEmpty)
                {
                    empty.Add((row, col));
                }
            }
        }
        return empty;
    }

    /// <summary>
    /// Count elements in each column above a given row (for gravity).
    /// </summary>
    public int CountElementsAbove(int row, int col)
    {
        int count = 0;
        for (int r = 0; r < row; r++)
        {
            if (!_cells[r, col].IsEmpty)
                count++;
        }
        return count;
    }
}
