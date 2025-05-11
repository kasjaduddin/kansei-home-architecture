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
    [SerializeField] private Button basicColorButton;
    [SerializeField] private Button materialButton;

    private ColoringManager.ObjectType currentObjectType = ColoringManager.ObjectType.Wall;
    private Material customColorMaterial; // Store the custom color material

    private bool isBasicColor = true;
    private ColoringManager.ObjectType currentType;
    private void Start()
    {
        floorButton.onClick.AddListener(() => {
            currentType = ColoringManager.ObjectType.Floor;
            UpdateMaterialButtons(currentType);
            });
        wallButton.onClick.AddListener(() =>
        {
            currentType = ColoringManager.ObjectType.Wall;
            UpdateMaterialButtons(currentType);
        });
        ceilingButton.onClick.AddListener(() =>
        {
            currentType = ColoringManager.ObjectType.Ceiling;
            UpdateMaterialButtons(currentType);
        });
        basicColorButton.onClick.AddListener(() => {
            isBasicColor = true;
            UpdateMaterialButtons(currentType);
        });
        materialButton.onClick.AddListener(() => {
            isBasicColor = false;
            UpdateMaterialButtons(currentType);
        });

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

            RawImage buttonImage = newButton.GetComponentInChildren<RawImage>();
            if (buttonImage != null)
            {
                Material mat = materialData.material;

                if (mat.mainTexture != null)
                {
                    buttonImage.texture = mat.mainTexture;
                    buttonImage.color = Color.white; // Reset color to ensure texture shows
                }
                else if (mat.HasProperty("_Color"))
                {
                    // Generate solid color preview if no texture
                    Texture2D colorTexture = new Texture2D(1, 1);
                    colorTexture.SetPixel(0, 0, mat.color);
                    colorTexture.Apply();
                    buttonImage.texture = colorTexture;
                    buttonImage.color = Color.white;
                }
                else
                {
                    Debug.LogWarning("Material has no texture or color property!");
                }
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

    /*private void UpdateMaterialButtons(ColoringManager.ObjectType objectType)
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
    }*/

    private List<MaterialData> GetFilteredMaterialsByUsage(ColoringManager.ObjectType objectType)
    {
        MaterialUsage targetUsage = objectType switch
        {
            ColoringManager.ObjectType.Floor => MaterialUsage.Floor,
            ColoringManager.ObjectType.Ceiling => MaterialUsage.Ceiling,
            _ => MaterialUsage.Wall,
        };

        return materialsDatabase.materials
            .Where(mat => (mat.usage & targetUsage) != 0)
            .Where(mat =>
                isBasicColor ?
                mat.category == MaterialCategory.Paint :
                mat.category != MaterialCategory.Paint)
            .ToList();
    }
}