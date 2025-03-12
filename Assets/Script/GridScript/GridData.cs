using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class GridData
{
    Dictionary<Vector3Int, PlacementData> placedObjects = new();
 
    public void AddObjectAt(Vector3Int gridPosition, Vector2Int objectSize, int ID, int placedObjectIndex, int rotationAngle)
    {
        rotationAngle =  Mathf.RoundToInt(rotationAngle / 90f) * 90;
        List<Vector3Int> positionToOccupy = CalculatePosition(gridPosition, objectSize, rotationAngle);
        PlacementData data = new PlacementData(positionToOccupy, ID, placedObjectIndex);
        Debug.Log($"Taruh object di  {gridPosition} dengan ukuran {objectSize}");

        foreach (var pos in positionToOccupy)
        {
            if (placedObjects.ContainsKey(pos))
            {
                Debug.LogWarning($"Position {pos} is already occupied, skipping.");
                continue;
            }
            Debug.Log($"Placing object furniture at {pos}");
            placedObjects[pos] = data;
        }
        foreach (var obj in placedObjects)
        {
            Debug.Log($"data placed object furniture {obj}");
        }
    }
    public void AddInitialObjectAt(Vector3Int gridPosition, Vector2Int objectSize)
    {
        List <Vector3Int> positionToOccupy = CalculatePosition(gridPosition, objectSize, 0);

        // Store in dictionary only if the position is NOT already occupied
        PlacementData data = new PlacementData(positionToOccupy, -1, -1);
        Debug.Log($"Taruh wall di  {gridPosition} dengan ukuran {objectSize}");

        foreach (var pos in positionToOccupy)
        {
            if (placedObjects.ContainsKey(pos))
            {
                Debug.Log($"Position {pos} is already occupied, skipping this position but continuing.");
                continue; // Skip this position but continue with the rest
            }
            Debug.Log($"Placing object at {pos}");

            placedObjects[pos] = data;
            //data.occupiedPositions.Add(pos); // Track only positions that were actually stored
        }
        foreach (var obj in placedObjects)
        {
            Debug.Log($"data placed object {obj}");
        }
    }


    private List<Vector3Int> CalculatePosition(Vector3Int gridPosition, Vector2Int objectSize, int rotationAngle)
    {
        List<Vector3Int> returnVal = new();
        for (int x = 0; x < objectSize.x; x++)
        {
            for (int y = 0; y < objectSize.y; y++)
            {
                returnVal.Add(gridPosition + new Vector3Int(x, 0, y));
            }
        }
        return returnVal;
    }
    public bool CanPlacedObjectAt(Vector3Int gridPosition, Vector2Int objectSize, int rotationAngle)
    {
        List<Vector3Int> positionToOccupy = CalculatePosition(gridPosition, objectSize, rotationAngle);
        foreach (var pos in positionToOccupy)
        {
            if (placedObjects.ContainsKey(pos))
                return false;
        }
        return true;
    }

    internal int GetRepresentationIndex(Vector3Int gridPosition)
    {
        if (placedObjects.ContainsKey(gridPosition) == false)
            return -1;
        return placedObjects[gridPosition].PlacedObjectIndex;
    }
    internal PlacementData GetPlacementData(Vector3Int gridPosition)
    {
        if (placedObjects.ContainsKey(gridPosition))
            return placedObjects[gridPosition];
        return null;
    }
    internal void RemoveObjectAt(Vector3Int gridPosition)
    {
        if (!placedObjects.ContainsKey(gridPosition))
            return;

        PlacementData data = placedObjects[gridPosition]; // Get stored data
        foreach (var pos in data.occupiedPositions) // Remove all occupied grid cells
        {
            placedObjects.Remove(pos);
        }
    }
}

    public class PlacementData
{
    public List<Vector3Int> occupiedPositions;
    public int ID {get; private set;}
    public int PlacedObjectIndex {get; private set;}

    public PlacementData(List<Vector3Int> occupiedPositions, int iD, int placedObjectIndex)
    {
        this.occupiedPositions = occupiedPositions;
        ID = iD;
        PlacedObjectIndex = placedObjectIndex;
    }
}
