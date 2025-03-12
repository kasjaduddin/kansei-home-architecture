using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlacementState : IBuildingState
{
    private FurnitureData selectedFurniture;
    Grid grid;
    PreviewSystem previewSystem;
    GridData floorData;
    GridData wallData;
    GridData ceilingData;
    GridData furnitureData; // Class field
    ObjectPlacer objectPlacer;
    Transform wallReference;

    private int currentRotationAngle = 0;
    public PlacementState(FurnitureData selectedFurnitureData,
                      Grid grid,
                      PreviewSystem previewSystem,
                      GridData floorData,
                      GridData wallData,
                      GridData ceilingData,
                      GridData furnitureGridData,
                      ObjectPlacer objectPlacer,
                      Transform wallReference)
    {
        this.selectedFurniture = selectedFurnitureData;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.floorData = floorData;
        this.wallData = wallData;
        this.ceilingData = ceilingData;
        this.furnitureData = furnitureGridData;
        this.objectPlacer = objectPlacer;
        this.wallReference = wallReference;

        if (selectedFurniture != null)
        {
            Debug.Log($"[PlacementState] Showing preview for {selectedFurniture.furnitureName}");

            previewSystem.StartShowingPlacementPreview(
                selectedFurniture.furniturePrefab,
                selectedFurniture.size
            );
        }
        else
        {
            Debug.LogError("[PlacementState] Selected furniture is NULL!");
        }
        GridData selectedData = GetSelectedGridData(selectedFurniture.furniturePlacement);
        foreach (Transform wall in wallReference)
        {
            Debug.Log("Ukuran wallnya  " + wall.localScale.x * 10 + " , " + wall.localScale.z * 10);

            Vector3 worldPos = wall.position;
            Vector3Int gridPos = grid.WorldToCell(worldPos);
            gridPos.y = 0;
            Vector2Int wallSize = new Vector2Int(
                Mathf.RoundToInt(wall.localScale.x * 10),
                Mathf.RoundToInt(wall.localScale.z * 10)
            );
            // Calculate the bottom-left offset for the pivot in grid units
            Vector3Int bottomLeftOffset = new Vector3Int(
                Mathf.FloorToInt(wallSize.x / 2f), // Half the width (X-axis)
                0,
                Mathf.FloorToInt(wallSize.y / 2f)  // Half the height (Z-axis)
            );

            // Shift the gridPos to align the pivot to the bottom-left corner
            gridPos -= bottomLeftOffset;
            // Determine the size of the wall in grid units

            // Mark the grid as occupied
            selectedData.AddInitialObjectAt(gridPos, wallSize);
            //buildingState.SaveInitialObject(gridPos, wallSize);
        }
    }
    private GridData GetSelectedGridData(FurniturePlacement placement)
    {
        switch (placement)
        {
            case FurniturePlacement.OnFloor:
                return floorData;
            case FurniturePlacement.OnWall:
                return wallData;
            case FurniturePlacement.OnCeiling:
                return ceilingData;
            case FurniturePlacement.EmbeddedInWall:
                return furnitureData; // Adjust if a separate grid is needed
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

        int index = objectPlacer.PlaceObject(
            selectedFurniture.furniturePrefab,
            grid.CellToWorld(gridPosition) + selectedFurniture.defaultPositionOffset,
            rotation
        );

        GridData selectedData = GetSelectedGridData(selectedFurniture.furniturePlacement);
        rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        Debug.Log("ini nih rotation angle " + rotationAngle);
        selectedData.AddObjectAt(gridPosition, rotatedSize, (int)selectedFurniture.furnitureType, index, rotationAngle);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition)
    {
        Vector2Int rotatedSize = previewSystem.GetAdjustedSize();
        int rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        GridData selectedData = GetSelectedGridData(selectedFurniture.furniturePlacement);
        return selectedData.CanPlacedObjectAt(gridPosition, rotatedSize, rotationAngle);
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool placementValidity = CheckPlacementValidity(gridPosition);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);
    }
}