using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManagerMouse inputManager;

    [SerializeField] private Grid grid;

    [SerializeField] private FurnitureCollectionSO furnitureDatabase;


    [SerializeField] private GameObject gridVisualization;

    private GridData floorData, furnitureData;

    [SerializeField] private PreviewSystem preview;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;

    [SerializeField] private ObjectPlacer objectPlacer;

    IBuildingState buildingState;

    private void Start()
    {
        StopPlacement();
        floorData = new();
        furnitureData = new();
    }

    public void StartPlacement(int furnitureID)
    {
        StopPlacement();
        gridVisualization.SetActive(true);

        FurnitureData selectedFurniture = FindFurnitureByID(furnitureID);
        if (selectedFurniture == null)
        {
            Debug.LogError($"Furniture with ID {furnitureID} not found.");
            return;
        }

        buildingState = new PlacementState(
            selectedFurniture,
            grid,
            preview,
            floorData,
            furnitureData,
            objectPlacer);

        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }
    private FurnitureData FindFurnitureByID(int id)
    {
        List<FurnitureData> allFurniture = GetAllFurniture();
        if (id >= 0 && id < allFurniture.Count)
            return allFurniture[id];

        return null;
    }
    private List<FurnitureData> GetAllFurniture()
    {
        List<FurnitureData> allFurniture = new List<FurnitureData>();
        allFurniture.AddRange(furnitureDatabase.floorFurniture);
        allFurniture.AddRange(furnitureDatabase.wallFurniture);
        allFurniture.AddRange(furnitureDatabase.ceilingFurniture);
        allFurniture.AddRange(furnitureDatabase.embeddedFurniture);
        return allFurniture;
    }
    public void StartRemoving()
    {
        StopPlacement();
        gridVisualization.SetActive(true);
        buildingState = new RemovingState(grid, preview, floorData, furnitureData, objectPlacer);
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }
    private void PlaceStructure()
    {
        if (inputManager.IsPointerOverUI()) { return; }
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        buildingState.OnAction(gridPosition);
    }


    private void StopPlacement()
    {
        if (buildingState == null) { return; }
        gridVisualization.SetActive(false);
        buildingState.EndState();
        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;
        lastDetectedPosition = Vector3Int.zero;
        buildingState= null;
    }

    private void Update()
    {
        if (buildingState == null )
            return;
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        if (lastDetectedPosition != gridPosition)
        {
            buildingState.UpdateState(gridPosition);
            lastDetectedPosition= gridPosition;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            preview.RotateLeft();
            buildingState.UpdateState(gridPosition);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            preview.RotateRight();
            buildingState.UpdateState(gridPosition);
        }
    }
}
