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
    public Vector3 boardOrigin = new Vector3(0f, -0.7f, 0f);

    [Header("References")]
    public GameManager gameManager;

    private CellData[,] _cells;
    private int _rows;
    private int _cols;
    private GameObject _boardRoot;
    private Sprite _cellBgSprite;

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
        // (Recomputed in InitializeBoard once the real grid dimensions land.)
        boardOrigin = ComputeBoardOrigin(_rows, _cols);
    }

    private Vector3 ComputeBoardOrigin(int rows, int cols)
    {
        float spacing = cellSize + cellGap;
        return new Vector3(
            -(cols - 1) * spacing * 0.5f,
            (rows - 1) * spacing * 0.5f - 0.9f, // shift down to clear TopBar
            0f
        );
    }

    // NOTE: InitializeBoard(grid) is called explicitly by GameManager.Start()
    // (not from Unity's Start()) so that suitcases are already in the grid
    // (placed by BoardGenerator per LevelConfig) before any GameObjects are
    // instantiated. BoardController is created at runtime via AddComponent,
    // so its Start() would otherwise run next frame.

    /// <summary>
    /// Initialize the board from an externally-generated layout (produced by
    /// BoardGenerator from a LevelConfig). Dimensions are taken from the grid;
    /// boardOrigin is recomputed so non-default sizes stay centered. Empty
    /// cells are skipped (no GameObject created).
    /// </summary>
    public void InitializeBoard(CellData[,] grid)
    {
        if (grid == null) return;

        _rows = grid.GetLength(0);
        _cols = grid.GetLength(1);
        _cells = new CellData[_rows, _cols];
        boardOrigin = ComputeBoardOrigin(_rows, _cols);

        // Create cell background sprite (shared, 9-slice for any cell size)
        // Matches HTML .cell: background rgba(255,255,255,0.08), border rgba(255,255,255,0.10)
        // border-radius 8px. Boosted alpha for visibility in Unity.
        _cellBgSprite = GlassPanelTexture.CreateGlassPanel(
            96, 18f,
            new Color(0.12f, 0.34f, 0.64f, 0.27f),
            0.34f,
            0.16f,
            0.014f
        );

        _boardRoot = new GameObject("BoardRoot");
        _boardRoot.transform.SetParent(transform);

        // Create cell backgrounds (fixed in position, don't move during swaps)
        CreateCellBackgrounds();

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                ElementType type = grid[row, col].elementType;
                if (type == ElementType.Empty) continue;
                CreateCell(row, col, type);
            }
        }
    }

    /// <summary>
    /// Creates subtle glass background tiles for each cell position.
    /// These stay fixed during swaps/matches — only the element sprites move.
    /// sortingOrder 0: above BoardBackdrop (-1), below elements (1).
    /// </summary>
    private void CreateCellBackgrounds()
    {
        if (_cellBgSprite == null) return;

        GameObject bgRoot = new GameObject("CellBackgrounds");
        bgRoot.transform.SetParent(_boardRoot.transform);

        float bgScale = cellSize * 0.88f; // slightly smaller than cell for gap

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                Vector3 pos = new CellData { row = row, col = col }
                    .WorldPosition(cellSize, cellGap, boardOrigin);

                GameObject bgGO = new GameObject($"CellBg_{row}_{col}");
                bgGO.transform.SetParent(bgRoot.transform);
                bgGO.transform.position = pos;
                bgGO.transform.localScale = Vector3.one;

                SpriteRenderer bgSr = bgGO.AddComponent<SpriteRenderer>();
                bgSr.sprite = _cellBgSprite;
                bgSr.sortingOrder = 0;
                bgSr.color = Color.white;
                // Use Sliced draw mode so 9-slice borders produce round corners
                bgSr.drawMode = SpriteDrawMode.Sliced;
                bgSr.size = new Vector2(bgScale, bgScale);
            }
        }
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
        PieceVisual visual = go.AddComponent<PieceVisual>();
        visual.Configure();
        visual.SetSprite(sprite);

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
            Sprite sprite = gameManager.GetSpriteForType(type, special, rocketDir);
            PieceVisual visual = _cells[row, col].gameObject.GetComponent<PieceVisual>();
            if (visual != null) visual.SetSprite(sprite);
            else
            {
                SpriteRenderer sr = _cells[row, col].gameObject.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = sprite;
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
        if (_boardRoot != null) go.transform.SetParent(_boardRoot.transform);
        go.transform.position = startPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = gameManager.GetSpriteForType(type, special, rocketDir);
        sr.sortingOrder = 1;
        PieceVisual visual = go.AddComponent<PieceVisual>();
        visual.Configure();
        visual.SetSprite(sr.sprite);

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

    /// <summary>
    /// All suitcase cells in row-major (top-to-bottom, left-to-right) order.
    /// The first element is the "highest priority" target (topmost, then
    /// leftmost) — used by the propeller and rocket×propeller combo.
    /// </summary>
    public System.Collections.Generic.List<(int row, int col)> GetSuitcaseCells()
    {
        var result = new System.Collections.Generic.List<(int, int)>();
        for (int row = 0; row < _rows; row++)
            for (int col = 0; col < _cols; col++)
                if (_cells[row, col].elementType == ElementType.Suitcase)
                    result.Add((row, col));
        return result;
    }
}
