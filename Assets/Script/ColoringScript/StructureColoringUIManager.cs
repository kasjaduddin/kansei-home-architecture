using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StructureColoringUIManager : MonoBehaviour
{
    [SerializeField] private MaterialCollectionSO materialsDatabase; // Reference to the database
    [SerializeField] private GameObject buttonPrefab; // Prefab for each button
    [SerializeField] private Transform buttonContainer; // UI Panel to hold buttons
    [SerializeField] private ColoringManager coloringSystem; // Reference to the placement system
    [SerializeField] private Button floorButton;
    [SerializeField] private Button wallButton;
    [SerializeField] private Button ceilingButton;

    private ColoringManager.ObjectType currentObjectType = ColoringManager.ObjectType.Wall; // Default to Wall

    private void Start()
    {
        floorButton.onClick.AddListener(() => UpdateMaterialButtons(ColoringManager.ObjectType.Floor));
        wallButton.onClick.AddListener(() => UpdateMaterialButtons(ColoringManager.ObjectType.Wall));
        ceilingButton.onClick.AddListener(() => UpdateMaterialButtons(ColoringManager.ObjectType.Ceiling));

        // Display wall materials by default
        UpdateMaterialButtons(ColoringManager.ObjectType.Wall);
    }

    private void UpdateMaterialButtons(ColoringManager.ObjectType objectType)
    {
        // Set the current selected type
        currentObjectType = objectType;

        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Get the correct list of materials
        List<MaterialData> materialList = GetMaterialList(objectType);

        // Create a button for each material
        foreach (MaterialData materialData in materialList)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

            // Create a new material instance with the UI/Default shader
            Material uiMaterial = new Material(materialData.material)
            {
                shader = Shader.Find("UI/Default")
            };

            // Assign the material to the button's Image component
            RawImage buttonImage = newButton.GetComponentInChildren<RawImage>();
            if (buttonImage != null)
            {
                buttonImage.material = uiMaterial;
            }
            else
            {
                Debug.LogError("Image component not found in button prefab!");
            }

            // Assign click event
            Button buttonComponent = newButton.GetComponentInChildren<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() =>
                {
                    if (coloringSystem != null && materialData.material != null)
                    {
                        coloringSystem.ChangeRoomMaterial(materialData.material, currentObjectType);

                    }
                    else
                    {
                        Debug.LogError("coloringSystem or materialData is null!");
                    }
                });
            }
            else
            {
                Debug.LogError("Button component not found in button prefab!");
            }
        }
    }

    private List<MaterialData> GetMaterialList(ColoringManager.ObjectType objectType)
    {
        switch (objectType)
        {
            case ColoringManager.ObjectType.Floor:
                return materialsDatabase.floorMaterials;
            case ColoringManager.ObjectType.Ceiling:
                return materialsDatabase.ceilingMaterials;
            case ColoringManager.ObjectType.Wall:
            default:
                return materialsDatabase.wallMaterials;
        }
    }
}