using UnityEngine;

/// <summary>
/// Data for a single cell in the board grid.
/// </summary>
public struct CellData
{
    public int row;
    public int col;
    public ElementType elementType;
    public GameObject gameObject;
    public GameConfig.SpecialType specialType;
    public GameConfig.RocketDir rocketDir;

    public bool IsEmpty => elementType == ElementType.Empty;
    public bool HasSpecial => specialType != GameConfig.SpecialType.None;

    public Vector3 WorldPosition(float cellSize, float cellGap, Vector3 boardOrigin)
    {
        float x = boardOrigin.x + col * (cellSize + cellGap);
        float y = boardOrigin.y - row * (cellSize + cellGap);
        return new Vector3(x, y, 0);
    }

    public static (int row, int col) FromWorld(Vector3 worldPos, float cellSize, float cellGap, Vector3 boardOrigin)
    {
        int col = Mathf.RoundToInt((worldPos.x - boardOrigin.x) / (cellSize + cellGap));
        int row = Mathf.RoundToInt((boardOrigin.y - worldPos.y) / (cellSize + cellGap));
        return (row, col);
    }
}
