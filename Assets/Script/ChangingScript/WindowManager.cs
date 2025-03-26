using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    private List<GameObject> cubeObjectsUp = new List<GameObject>();
    private List<GameObject> cubeObjectsDown = new List<GameObject>();
    [SerializeField] private Shader customShader;


    public Material currentMaterialUp;
    public Material currentMaterialDown;
    private void UpdateCubeReferences()
    {
        // Clear previous references
        cubeObjectsUp.Clear();
        cubeObjectsDown.Clear();

        // Get the first child (parent of prefabs)
        if (transform.childCount == 0) return;
        Transform prefabParent = transform.GetChild(0);

        // Find "up" and "down" inside the prefab parent
        Transform up = prefabParent.Find("up");
        Transform down = prefabParent.Find("down");

        // Add all children from "up" and "down" to their respective lists
        if (up != null)
        {
            foreach (Transform child in up)
            {
                cubeObjectsUp.Add(child.gameObject);
            }
        }

        if (down != null)
        {
            foreach (Transform child in down)
            {
                cubeObjectsDown.Add(child.gameObject);
            }
        }
    }
    public void UpdatePrefabWallColor()
    {
        if (currentMaterialUp != null)
        {
            UpdateCubeReferences();
            ChangeAllCubeColors(currentMaterialUp, "up");
        }

        // Check and apply down material if it exists
        if (currentMaterialDown != null)
        {
            UpdateCubeReferences();
            ChangeAllCubeColors(currentMaterialDown, "down");
        }
    }

    public void ChangeWindowPrefab(GameObject newPrefab)
    {
        // Destroy existing children safely
        for (int i = this.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(this.transform.GetChild(i).gameObject);
        }

        // Instantiate new prefab as child
        GameObject newInstance = Instantiate(newPrefab, this.transform);
    }

    public void ChangeAllCubeColors(Material newMaterial, string direction)
    {
        // Ensure we have updated references
        UpdateCubeReferences();

        List<GameObject> targetCubes = direction.ToLower() == "up" ? cubeObjectsUp : cubeObjectsDown;

        if (direction.ToLower() == "up")
        {
            currentMaterialUp = newMaterial;
        }
        else
        {
            currentMaterialDown = newMaterial;
        }

        foreach (GameObject cube in targetCubes)
        {
            ApplyCustomMaterial(cube, newMaterial);
        }
    }

    private void ApplyCustomMaterial(GameObject targetObject, Material newMaterial)
    {
        if (customShader == null)
        {
            Debug.LogError("Shader not assigned in ColoringManager!");
            return;
        }

        if (newMaterial == null)
        {
            Debug.LogError("Material is null!");
            return;
        }

        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("No Renderer found on " + targetObject.name);
            return;
        }

        // Create a new material using the custom shader
        Material appliedMaterial = new Material(customShader);

        // Set Tiling and Blend values
        appliedMaterial.SetFloat("_Tiling", 1f);
        appliedMaterial.SetFloat("_Blend", 1f);

        // Assign the texture from the new material
        if (newMaterial.mainTexture != null)
        {
            appliedMaterial.SetTexture("_MainTexture", newMaterial.mainTexture);
        }
        else
        {
            Debug.LogWarning("New material has no texture assigned.");
        }

        // Apply the modified material to the object
        renderer.material = appliedMaterial;
    }
}
