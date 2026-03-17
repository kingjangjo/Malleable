using System.Collections.Generic;
using UnityEngine;

public class SpatialHash : MonoBehaviour
{
    private float cellSize;
    private Dictionary<long, List<int>> grid;

    private const long P1 = 73856093L;
    private const long P2 = 19349663L;
    private const long P3 = 83492791L;

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        grid = new Dictionary<long, List<int>>();
    }

    private void GetCell(Vector3 pos, out int cx, out int cy, out int cz)
    {
        cx = Mathf.FloorToInt(pos.x / cellSize);
        cy = Mathf.FloorToInt(pos.y / cellSize);
        cz = Mathf.FloorToInt(pos.z / cellSize);
    }
}
