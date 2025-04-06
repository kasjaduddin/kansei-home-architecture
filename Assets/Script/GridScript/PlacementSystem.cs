using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManagerMouse inputManager;

    [SerializeField] private Grid grid;

    [SerializeField] private FurnitureCollectionSO furnitureDatabase;


    [SerializeField] private GameObject gridVisualization;

    private GridData floorData, furnitureData, wallData, objectOnWallData, ceilingData;

    [SerializeField] private PreviewSystem preview;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;

    [SerializeField] private ObjectPlacer objectPlacer;

    private List<Transform> wallReference;

    [SerializeField] private HomeStructureManager homeManager;


    private bool wallsInitialized = false;

    IBuildingState buildingState;

    private void Start()
    {
        InitializeGridData();
        InitializeWalls();
        StopPlacement();
        wallReference = homeManager.GetRoomObjectTransforms();
    }

    private void InitializeGridData()
    {
        floorData = new GridData();
        wallData = new GridData();
        objectOnWallData = new GridData();
        ceilingData = new GridData();
        furnitureData = new GridData();
    }

    private void InitializeWalls()
    {
        if (wallsInitialized || wallReference == null) return;

        Debug.Log("Initializing wall data...");
        foreach (Transform wall in wallReference)
        {
            PopulateWallObjects(wall, wallData);
        }
        wallsInitialized = true;

    }

    private void PopulateWallObjects(Transform obj, GridData targetGrid)
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
            targetGrid.AddInitialObjectAt(gridPos, wallSize);
        }

        foreach (Transform child in obj)
        {
            PopulateWallObjects(child, targetGrid);
        }
    }

    public void StartPlacement(int furnitureID)
    {
        InitializeWalls(); // Ensure walls are initialized
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
            wallData,
            objectOnWallData,
            ceilingData,
            furnitureData,
            objectPlacer,
            wallReference
        );

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
        return allFurniture;
    }
    public void StartRemoving()
    {
        StopPlacement();
        gridVisualization.SetActive(true);
        buildingState = new RemovingState(grid,
                                          preview,
                                          floorData,
                                          wallData,
                                          objectOnWallData,
                                          ceilingData,
                                          furnitureData,
                                          objectPlacer);
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
