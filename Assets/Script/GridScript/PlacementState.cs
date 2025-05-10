using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementState : IBuildingState
{
    private FurnitureData selectedFurniture;
    Grid grid;
    PreviewSystem previewSystem;
    GridData floorData;
    GridData wallData;
    GridData objectOnWallData;
    GridData ceilingData;
    GridData furnitureData;
    ObjectPlacer objectPlacer;
    List<Transform> wallReference;
    private Camera mainCamera;

    private int currentRotationAngle = 0;
    public PlacementState(FurnitureData selectedFurnitureData,
                      Grid grid,
                      PreviewSystem previewSystem,
                      GridData floorData,
                      GridData wallData,
                      GridData objectOnWallData,
                      GridData ceilingData,
                      GridData furnitureGridData,
                      ObjectPlacer objectPlacer,
                      List<Transform> wallReference)
    {
        this.selectedFurniture = selectedFurnitureData;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.floorData = floorData;
        this.wallData = wallData;
        this.objectOnWallData = objectOnWallData;
        this.ceilingData = ceilingData;
        this.furnitureData = furnitureGridData;
        this.objectPlacer = objectPlacer;
        this.wallReference = wallReference;
        this.mainCamera = Camera.main;

        previewSystem.StartShowingPlacementPreview(
            selectedFurniture.furniturePrefab,
            selectedFurniture.size
        );
    }

    private void PopulateWallObjects(Transform obj, GridData selectedData)
    {
        if (obj.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Vector3 worldPos = obj.position;
            Vector3Int gridPos = grid.WorldToCell(worldPos);
            gridPos.y = 0;

            Vector2Int wallSize = new Vector2Int(
                Mathf.RoundToInt(obj.localScale.x * 10),
                Mathf.RoundToInt(obj.localScale.z * 10)
            );

            Vector3Int bottomLeftOffset = new Vector3Int(
                Mathf.FloorToInt(wallSize.x / 2f),
                0,
                Mathf.FloorToInt(wallSize.y / 2f)
            );

            gridPos -= bottomLeftOffset;
            selectedData.AddInitialObjectAt(gridPos, wallSize);
        }

        foreach (Transform child in obj)
        {
            PopulateWallObjects(child, selectedData);
        }
    }

    private GridData GetSelectedGridData(FurniturePlacement placement)
    {
        switch (placement)
        {
            case FurniturePlacement.OnFloor:
                return floorData;
            case FurniturePlacement.OnWall:
                return objectOnWallData;
            default:
                Debug.LogError($"[PlacementState] Unknown FurniturePlacement: {placement}");
                return null;
        }
    }

    public void EndState()
    {
        previewSystem.StopShowingPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        Quaternion rotation = previewSystem.GetRotation();
        Vector2Int rotatedSize = previewSystem.GetAdjustedSize();
        int rotationAngle = (int)rotation.eulerAngles.y;

        bool placementValidity = CheckPlacementValidity(gridPosition);
        if (!placementValidity) return;

        if (selectedFurniture.furniturePlacement == FurniturePlacement.OnWall)
        {
            PlaceWallObject(gridPosition, rotation);
        }
        else
        {
            PlaceRegularObject(gridPosition, rotation);
        }
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition)
    {
        Vector2Int rotatedSize = previewSystem.GetAdjustedSize();
        int rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        GridData selectedData = GetSelectedGridData(selectedFurniture.furniturePlacement);

        // Special check for wall objects
        if (selectedFurniture.furniturePlacement == FurniturePlacement.OnWall)
        {
            // Check if at least one position overlaps with a wall
            if (!IsPartiallyOnWall(gridPosition, rotatedSize, rotationAngle))
            {
                Debug.Log("Placement failed: Object not on wall");
                return false;
            }
            if (!objectOnWallData.CanPlacedObjectAt(gridPosition, rotatedSize, rotationAngle))
            {
                return false;
            }
        }
        else // For non-wall objects
        {
            if (IsPartiallyOnWall(gridPosition, rotatedSize, rotationAngle))
            {
                Debug.Log("Placement failed: Object not on wall");
                return false;
            }
            if (!selectedData.CanPlacedObjectAt(gridPosition, rotatedSize, rotationAngle))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPartiallyOnWall(Vector3Int gridPosition, Vector2Int size, int rotationAngle)
    {
        List<Vector3Int> positionsToCheck = wallData.CalculatePosition(gridPosition, size, rotationAngle);

        foreach (Vector3Int pos in positionsToCheck)
        {
            // Check if this grid cell contains a wall (ID -1)
            if (wallData.IsPositionOccupied(pos) &&
                wallData.GetPlacementData(pos).ID == -1)
            {
                return true;
            }
        }
        return false;
    }
    private void PlaceWallObject(Vector3Int gridPosition, Quaternion rotation)
    {
        int index = objectPlacer.PlaceObject(
            selectedFurniture.furniturePrefab,
            grid.CellToWorld(gridPosition) + selectedFurniture.defaultPositionOffset,
            rotation
        );

        GridData selectedData = GetSelectedGridData(selectedFurniture.furniturePlacement);
        int rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        Vector2Int rotatedSize = previewSystem.GetAdjustedSize();

        selectedData.AddObjectAt(
            gridPosition,
            rotatedSize,
            (int)selectedFurniture.furnitureType,
            index,
            rotationAngle
        );

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
    }

    private void PlaceRegularObject(Vector3Int gridPosition, Quaternion rotation)
    {
        int index = objectPlacer.PlaceObject(
            selectedFurniture.furniturePrefab,
            grid.CellToWorld(gridPosition) + selectedFurniture.defaultPositionOffset,
            rotation
        );

        GridData selectedData = GetSelectedGridData(selectedFurniture.furniturePlacement);
        int rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        Vector2Int rotatedSize = previewSystem.GetAdjustedSize();

        selectedData.AddObjectAt(
            gridPosition,
            rotatedSize,
            (int)selectedFurniture.furnitureType,
            index,
            rotationAngle
        );

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool placementValidity = CheckPlacementValidity(gridPosition);

        // For wall objects, adjust the preview position slightly
        if (selectedFurniture.furniturePlacement == FurniturePlacement.OnWall && placementValidity)
        {
            Vector3 worldPos = grid.CellToWorld(gridPosition);
            worldPos += selectedFurniture.defaultPositionOffset;
            previewSystem.UpdatePosition(worldPos, placementValidity);
        }
        else
        {
            previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);
        }
    }
    
}