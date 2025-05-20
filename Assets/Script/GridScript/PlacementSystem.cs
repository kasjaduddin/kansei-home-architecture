using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.XR;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManagerVR inputManager;

    [SerializeField] private Grid grid;

    [SerializeField] private FurnitureCollectionSO furnitureDatabase;


    [SerializeField] private GameObject gridVisualization;

    private GridData floorData, furnitureData, wallData, objectOnWallData, ceilingData;

    [SerializeField] private PreviewSystem preview;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;

    [SerializeField] private ObjectPlacer objectPlacer;

    [SerializeField] private HomeDesignParentManager ParentHomeDesign;

    public List<GameObject> wallReference;

    //[SerializeField] private HomeStructureManager homeManager;

    private Vector2 lastThumbstickValue;
    private bool wallsInitialized = false;

    IBuildingState buildingState;

    private void Start()
    {
        InitializeGridData();
        StopPlacement();
    }

    private void InitializeGridData()
    {
        floorData = new GridData();
        wallData = new GridData();
        objectOnWallData = new GridData();
        ceilingData = new GridData();
        furnitureData = new GridData();
    }
    private IEnumerator InitializeWallsCoroutine()
    {
        if (wallsInitialized || wallReference == null) yield break;

        Debug.Log("Initializing wall data...");
        foreach (GameObject wall in wallReference)
        {
            Transform wallTrans = wall.transform;
            PopulateWallObjects(wallTrans, wallData);
            yield return null; // Wait one frame between walls
        }
        wallsInitialized = true;
    }

    /*private void InitializeWalls()
    {
        if (wallsInitialized || wallReference == null) return;

        Debug.Log("Initializing wall data...");
        foreach (GameObject wall in wallReference)
        {
            Transform wallTrans = wall.transform;
            PopulateWallObjects(wallTrans, wallData);
        }
        wallsInitialized = true;

    }*/

    public GridData GetWallData()
    {
        return wallData;
    }
    public void PopulateWallObjectsStatic(Transform obj, GridData targetGrid)
    {
        if (obj.gameObject.layer != LayerMask.NameToLayer("Wall"))
        {
            return;
        }

        Vector3 worldPos = obj.parent != null ? obj.parent.position : obj.position;
        Vector3Int gridPos = grid.WorldToCell(worldPos); // Use instance grid now
        gridPos.y = 0;

        Vector2Int wallSize = new Vector2Int(
            Mathf.RoundToInt(obj.localScale.x * 20),
            Mathf.RoundToInt(obj.localScale.z * 20)
        );

        Vector3Int bottomLeftOffset = new Vector3Int(
            Mathf.FloorToInt(wallSize.x / 2f),
            0,
            Mathf.FloorToInt(wallSize.y / 2f)
        );

        gridPos -= bottomLeftOffset;
        targetGrid.AddInitialObjectAt(gridPos, wallSize);
    }

    private void PopulateWallObjects(Transform obj, GridData targetGrid)
    {
        if (obj.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            PopulateWallObjectsStatic(obj, targetGrid);
        }

        foreach (Transform child in obj)
        {
            PopulateWallObjects(child, targetGrid);
        }
    }
    /*private void PopulateWallObjects(Transform obj, GridData targetGrid)
    {
        if (obj.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Vector3 worldPos = obj.parent != null ? obj.parent.position : obj.position;
            Vector3Int gridPos = grid.WorldToCell(worldPos);
            gridPos.y = 0;

            Vector2Int wallSize = new Vector2Int(   
                Mathf.RoundToInt(obj.localScale.x * 20),
                Mathf.RoundToInt(obj.localScale.z * 20)
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
    }*/

    public void StartPlacement(int furnitureID)
    {
        wallReference = ParentHomeDesign.wallReference;

        StartCoroutine(InitializeWallsCoroutine()); // Ensure walls are initialized
        //StopPlacement();
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
        inputManager.OnRotateLeft += RotateLeft;
        inputManager.OnRotateRight += RotateRight;
    }
    private void RotateLeft()
    {
        Vector3Int gridPosition = grid.WorldToCell(inputManager.GetSelectedMapPosition());
        preview.RotateLeft();
        buildingState.UpdateState(gridPosition);
    }

    private void RotateRight()
    {
        Vector3Int gridPosition = grid.WorldToCell(inputManager.GetSelectedMapPosition());
        preview.RotateRight();
        buildingState.UpdateState(gridPosition);
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
        if (buildingState == null) return;

        Vector3 position = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(position);

        if (lastDetectedPosition != gridPosition)
        {
            buildingState.UpdateState(gridPosition);
            lastDetectedPosition = gridPosition;
        }

        // Optional fallback for keyboard rotation
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RotateLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            RotateRight();
        }
    }
}
