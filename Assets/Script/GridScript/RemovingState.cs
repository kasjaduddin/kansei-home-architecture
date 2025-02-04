using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.OpenXR.Features.Interactions;

public class RemovingState : IBuildingState
{
    private int gameObjectIndex = -1;
    Grid grid;
    PreviewSystem previewSystem;
    GridData floorData;
    GridData furnitureData;
    ObjectPlacer objectPlacer;

    public RemovingState(Grid grid,
                         PreviewSystem previewSystem,
                         GridData floorData,
                         GridData furnitureData,
                         ObjectPlacer objectPlacer)
    {
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.floorData = floorData;
        this.furnitureData = furnitureData;
        this.objectPlacer = objectPlacer;

        previewSystem.StartShowingRemovePreview();
    }

    public void EndState()
    {
        previewSystem.StopShowingPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        GridData selectedData = GetOccupiedGridData(gridPosition);

        if (selectedData == null)
        {
            // Play error sound if needed (no object to remove)
            return;
        }

        gameObjectIndex = selectedData.GetRepresentationIndex(gridPosition);
        if (gameObjectIndex == -1)
            return;

        selectedData.RemoveObjectAt(gridPosition); // Remove from GridData
        objectPlacer.RemoveObjectAt(gameObjectIndex); // Remove GameObject from scene

        Vector3 cellPosition = grid.CellToWorld(gridPosition);
        previewSystem.UpdatePosition(cellPosition, CheckIfSelectionIsValid(gridPosition));
    }

    private GridData GetOccupiedGridData(Vector3Int gridPosition)
    {
        if (furnitureData.GetRepresentationIndex(gridPosition) != -1)
            return furnitureData;
        if (floorData.GetRepresentationIndex(gridPosition) != -1)
            return floorData;

        return null; // No object is placed here
    }

    private bool CheckIfSelectionIsValid(Vector3Int gridPosition)
    {
        return GetOccupiedGridData(gridPosition) != null;
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool validity = CheckIfSelectionIsValid(gridPosition);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), validity);
    }
}