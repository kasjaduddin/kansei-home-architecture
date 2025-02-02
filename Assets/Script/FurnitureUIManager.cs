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

    private void Start()
    {
        GenerateFurnitureButtons();
    }

    private void GenerateFurnitureButtons()
    {
        foreach (var furniture in GetAllFurniture())
        {
            GameObject buttonObject = Instantiate(buttonPrefab, buttonContainer);
            Button button = buttonObject.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();

            buttonText.text = furniture.furnitureName; // Set button label
            int furnitureID = GetFurnitureID(furniture); // Get the unique ID

            button.onClick.AddListener(() => placementSystem.StartPlacement(furnitureID)); // Assign event
        }
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

    private int GetFurnitureID(FurnitureData furniture)
    {
        return GetAllFurniture().IndexOf(furniture); // Assign index as ID
    }
}
