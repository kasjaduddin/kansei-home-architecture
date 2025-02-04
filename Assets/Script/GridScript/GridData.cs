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
        foreach (var pos in positionToOccupy)
        {
            if (placedObjects.ContainsKey(pos))
            {
                throw new Exception($"Dictionary already contains this cell position {pos}");
            }
            placedObjects[pos] = data;
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

    /*private Vector2Int GetRotatedSize(Vector2Int objectSize, int rotationAngle)
    {
        Debug.Log("di get rotated " + rotationAngle + " Rotation sizenya " + objectSize);
        return (rotationAngle % 180 == 0) ? objectSize : new Vector2Int(objectSize.y, objectSize.x);
    }*/

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
