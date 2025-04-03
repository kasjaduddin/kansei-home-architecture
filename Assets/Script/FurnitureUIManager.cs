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
    [SerializeField] private Button ceilingButton;

    private void Start()
    {
        // Initially clear any existing buttons
        ClearFurnitureButtons();

        // Assign category button click listeners
        floorButton.onClick.AddListener(() => DisplayFurnitureByPlacement(FurniturePlacement.OnFloor));
        wallButton.onClick.AddListener(() => DisplayFurnitureByPlacement(FurniturePlacement.OnWall));
        ceilingButton.onClick.AddListener(() => DisplayFurnitureByPlacement(FurniturePlacement.OnCeiling));
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
            GameObject buttonObject = Instantiate(buttonPrefab, buttonContainer);
            Button button = buttonObject.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = furniture.furnitureName; // Set button label
            int furnitureID = GetFurnitureID(furniture); // Get the unique ID

            button.onClick.AddListener(() => placementSystem.StartPlacement(furnitureID));
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
            case FurniturePlacement.OnCeiling:
                filteredFurniture = furnitureDatabase.ceilingFurniture;
                break;
            case FurniturePlacement.EmbeddedInWall:
                filteredFurniture = furnitureDatabase.embeddedFurniture;
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