using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StructureColoringUIManager : MonoBehaviour
{
    [SerializeField] private MaterialCollectionSO materialsDatabase;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private ColoringManager coloringSystem;
    [SerializeField] private Button floorButton;
    [SerializeField] private Button wallButton;
    [SerializeField] private Button ceilingButton;

    private ColoringManager.ObjectType currentObjectType = ColoringManager.ObjectType.Wall;
    private Material customColorMaterial; // Store the custom color material

    private void Start()
    {
        floorButton.onClick.AddListener(() => UpdateMaterialButtons(ColoringManager.ObjectType.Floor));
        wallButton.onClick.AddListener(() => UpdateMaterialButtons(ColoringManager.ObjectType.Wall));
        ceilingButton.onClick.AddListener(() => UpdateMaterialButtons(ColoringManager.ObjectType.Ceiling));

        UpdateMaterialButtons(ColoringManager.ObjectType.Wall);

        // Initialize the custom color material once
        customColorMaterial = new Material(Shader.Find("Standard"));
    }

    private void UpdateMaterialButtons(ColoringManager.ObjectType objectType)
    {
        currentObjectType = objectType;

        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        List<MaterialData> materialList = GetFilteredMaterialsByUsage(objectType);

        foreach (MaterialData materialData in materialList)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

            Material uiMaterial = new Material(materialData.material)
            {
                shader = Shader.Find("UI/Default")
            };

            RawImage buttonImage = newButton.GetComponentInChildren<RawImage>();
            if (buttonImage != null)
            {
                buttonImage.material = uiMaterial;
            }
            else
            {
                Debug.LogError("Image component not found in button prefab!");
            }

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

    private List<MaterialData> GetFilteredMaterialsByUsage(ColoringManager.ObjectType objectType)
    {
        MaterialUsage targetUsage = objectType switch
        {
            ColoringManager.ObjectType.Floor => MaterialUsage.Floor,
            ColoringManager.ObjectType.Ceiling => MaterialUsage.Ceiling,
            _ => MaterialUsage.Wall,
        };

        return materialsDatabase.materials
            .Where(mat => (mat.usage & targetUsage) != 0) // bitwise match
            .ToList();
    }
}