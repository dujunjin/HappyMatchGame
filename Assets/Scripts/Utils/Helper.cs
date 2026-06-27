using UnityEngine;

public static class Helper
{
    public static bool IsInBounds(int row, int col, int rows, int cols)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }
}
