using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureUIManager : MonoBehaviour
{
    [SerializeField] private FurnitureCollectionSO furnitureDatabase; // Reference to the database
    [SerializeField] private GameObject buttonPrefab; // Prefab for each button
    [SerializeField] private Transform buttonContainer; // UI Panel to hold buttons
    [SerializeField] private PlacementSystem placementSystem; // Reference to the placement system
    [SerializeField] private Button floorButton;
    [SerializeField] private Button wallButton;
    [SerializeField] private MenuUIEditorManager menuUIManager;
    [SerializeField] private Button backButton;
    [SerializeField] private MenuUIEditorManager menuUICanvas;
    [SerializeField] private ScrollRect scrollMenu;

    private bool isCanvasOpen = false;
    
    private void Start()
    {
        InputManagerVR inputManager = FindObjectOfType<InputManagerVR>();
        if (inputManager != null)
        {
            inputManager.OnThumbstickScroll += HandleThumbstickScroll;
        }
        backButton.onClick.AddListener(() => {
            menuUICanvas.ShowMainMenuCanvas();
        });
        // Initially clear any existing buttons
        ClearFurnitureButtons();
        isCanvasOpen = true;
        // Assign category button click listeners
        floorButton.onClick.AddListener(() => DisplayFurnitureByPlacement(FurniturePlacement.OnFloor));
        wallButton.onClick.AddListener(() => DisplayFurnitureByPlacement(FurniturePlacement.OnWall));
    }
    private void HandleThumbstickScroll(float scrollDelta)
    {
        if (!isCanvasOpen || scrollMenu == null) return;

        float scrollSpeed = 0.5f;
        float newPos = scrollMenu.verticalNormalizedPosition + scrollDelta * scrollSpeed * Time.deltaTime;
        scrollMenu.verticalNormalizedPosition = Mathf.Clamp01(newPos);
    }
    private void OnDestroy()
    {
        InputManagerVR inputManager = FindObjectOfType<InputManagerVR>();
        if (inputManager != null)
        {
            inputManager.OnThumbstickScroll -= HandleThumbstickScroll;
        }
    }
    private void DisplayFurnitureByPlacement(FurniturePlacement placement)
    {
        // Clear existing buttons
        ClearFurnitureButtons();

        // Get the relevant furniture based on placement
        List<FurnitureData> filteredFurniture = GetFurnitureByPlacement(placement);

        // Generate new buttons only for this category
        foreach (var furniture in filteredFurniture)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

            RawImage rawImage = newButton.GetComponentInChildren<RawImage>();
            if (rawImage != null && furniture.objectTexture != null)
            {
                rawImage.texture = furniture.objectTexture;
            }
            else if (rawImage == null)
            {
                Debug.LogWarning("RawImage component not found in button prefab!");
            }
            Button buttonComponent = newButton.GetComponentInChildren<Button>();
            int furnitureID = GetFurnitureID(furniture);

            // Assign click behavior
            buttonComponent.onClick.AddListener(() => {
                placementSystem.StartPlacement(furnitureID);
                VisibilityCanvasFurniture();
            });
        }
    }

    public void VisibilityCanvasFurniture()
    {
        if (isCanvasOpen)
        {
            menuUIManager.HideAllCanvases();
            isCanvasOpen = false;
        }
        else
        {
            menuUIManager.ShowFurnitureCanvas();
            isCanvasOpen = true;
        }
        
    }
    private void ClearFurnitureButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private List<FurnitureData> GetFurnitureByPlacement(FurniturePlacement placement)
    {
        List<FurnitureData> filteredFurniture = new List<FurnitureData>();

        switch (placement)
        {
            case FurniturePlacement.OnFloor:
                filteredFurniture = furnitureDatabase.floorFurniture;
                break;
            case FurniturePlacement.OnWall:
                filteredFurniture = furnitureDatabase.wallFurniture;
                break;
        }

        return filteredFurniture;
    }

    private int GetFurnitureID(FurnitureData furniture)
    {
        return GetAllFurniture().IndexOf(furniture);
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
}