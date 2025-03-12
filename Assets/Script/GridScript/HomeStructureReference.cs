using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HomeStructureReference : MonoBehaviour
{

    public Grid grid;
    public Transform wallParent;
    public GridData gridData;

    void Start()
    {
        RegisterWallsAsOccupied(wallParent);
    }

    public void RegisterWallsAsOccupied(Transform wallParent)
    {
        foreach (Transform wall in wallParent)
        {
            Vector3 worldPos = wall.position;
            Vector3Int gridPos = grid.WorldToCell(worldPos);

            // Determine the size of the wall in grid units
            Vector2Int wallSize = new Vector2Int(
                Mathf.RoundToInt(wall.localScale.x / 0.1f),
                Mathf.RoundToInt(wall.localScale.z / 0.1f)
            );

            // Mark the grid as occupied
            gridData.AddObjectAt(gridPos, wallSize, -1, -1, 0);
        }
    }
}


