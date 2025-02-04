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
    GridData furnitureData; // Class field
    ObjectPlacer objectPlacer;

    private int currentRotationAngle = 0;
    public PlacementState(FurnitureData selectedFurnitureData,
                      Grid grid,
                      PreviewSystem previewSystem,
                      GridData floorData,
                      GridData furnitureGridData,
                      ObjectPlacer objectPlacer)
    {
        this.selectedFurniture = selectedFurnitureData;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.floorData = floorData;
        this.furnitureData = furnitureGridData;
        this.objectPlacer = objectPlacer;

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

        GridData selectedData = (selectedFurniture.furniturePlacement == FurniturePlacement.OnFloor) ? floorData : furnitureData;
        rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        Debug.Log("ini nih rotation angle " + rotationAngle);
        selectedData.AddObjectAt(gridPosition, rotatedSize, (int)selectedFurniture.furnitureType, index, rotationAngle);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition)
    {
        Vector2Int rotatedSize = previewSystem.GetAdjustedSize();
        int rotationAngle = (int)previewSystem.GetRotation().eulerAngles.y;
        GridData selectedData = (selectedFurniture.furniturePlacement == FurniturePlacement.OnFloor) ? floorData : furnitureData;
        return selectedData.CanPlacedObjectAt(gridPosition, rotatedSize, rotationAngle);
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool placementValidity = CheckPlacementValidity(gridPosition);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);
    }
}