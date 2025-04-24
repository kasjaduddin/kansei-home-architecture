using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WindowManager : MonoBehaviour
{
    private List<GameObject> cubeObjectsUp = new List<GameObject>();
    private List<GameObject> cubeObjectsDown = new List<GameObject>();
    [SerializeField] private Shader customShader;
    private Shader urpShader;

    public Material currentMaterialUp;
    public Material currentMaterialDown;

    private void Awake()
    {
        // Fallback to URP Lit shader if not assigned
        if (urpShader == null)
        {
            urpShader = Shader.Find("Universal Render Pipeline/Lit");
        }
    }
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

    public void ChangeWindowPrefab(GameObject newPrefab)
    {
        // Store current materials before destroying
        Material tempUp = currentMaterialUp;
        Material tempDown = currentMaterialDown;

        // Destroy existing children safely
        for (int i = this.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(this.transform.GetChild(i).gameObject);
        }

        // Instantiate new prefab as child
        GameObject newInstance = Instantiate(newPrefab, this.transform);

        // Wait for one frame to ensure the new prefab is fully initialized
        StartCoroutine(DelayedMaterialUpdate(tempUp, tempDown));

        UpdateCubeReferences();
    }

    private IEnumerator DelayedMaterialUpdate(Material upMat, Material downMat)
    {
        // Wait for end of frame to ensure all components are initialized
        yield return new WaitForEndOfFrame();
        
        // Reapply materials
        if (upMat != null) ChangeAllCubeColors(upMat, "up");
        if (downMat != null) ChangeAllCubeColors(downMat, "down");
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
        if (targetObject == null)
        {
            Debug.LogError("Target object is null!");
            return;
        }

        if (newMaterial == null)
        {
            Debug.LogError("New material is null!");
            return;
        }

        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("No Renderer found on " + targetObject.name);
            return;
        }

        // Case 1: For color-only materials (URP)
        if (IsColorOnlyMaterial(newMaterial))
        {
            ApplyURPMaterial(renderer, newMaterial);
            return;
        }

        // Case 2: For custom textured materials
        if (customShader == null)
        {
            Debug.LogError("Custom shader not assigned in ColoringManager!");
            return;
        }

        ApplyCustomShaderMaterial(renderer, newMaterial);
    }

    private bool IsColorOnlyMaterial(Material material)
    {
        // Check if this is a simple color material (no texture, standard or URP shader)
        return material.mainTexture == null &&
              (material.shader.name.Contains("Standard") ||
               material.shader.name.Contains("Universal Render Pipeline"));
    }

    private void ApplyURPMaterial(Renderer renderer, Material sourceMaterial)
    {
        Material urpMaterial = new Material(urpShader);

        // Copy color properties
        if (sourceMaterial.HasProperty("_Color"))
        {
            urpMaterial.color = sourceMaterial.color;
        }

        // Copy metallic/smoothness if available
        if (sourceMaterial.HasProperty("_Metallic"))
        {
            urpMaterial.SetFloat("_Metallic", sourceMaterial.GetFloat("_Metallic"));
        }

        if (sourceMaterial.HasProperty("_Smoothness"))
        {
            urpMaterial.SetFloat("_Smoothness", sourceMaterial.GetFloat("_Smoothness"));
        }

        renderer.material = urpMaterial;
    }

    private void ApplyCustomShaderMaterial(Renderer renderer, Material sourceMaterial)
    {
        Material appliedMaterial = new Material(customShader);

        // Set texture if available
        if (sourceMaterial.mainTexture != null)
        {
            appliedMaterial.SetTexture("_MainTexture", sourceMaterial.mainTexture);
        }

        // Set color
        if (sourceMaterial.HasProperty("_Color"))
        {
            appliedMaterial.color = sourceMaterial.color;
        }

        // Set other shader properties
        appliedMaterial.SetFloat("_Tiling", 1f);
        appliedMaterial.SetFloat("_Blend", 1f);

        renderer.material = appliedMaterial;
    }
}
