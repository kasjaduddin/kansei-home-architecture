using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    [SerializeField] private GameObject handleDoorPrefab;
    private bool isOpen = false;
    private float targetYRotation;
    private float rotationSpeed = 85f;

    // Add these to track the door's open/close states
    private float closedRotationY;
    private float openRotationY;

    private List<GameObject> cubeObjectsUp = new List<GameObject>();
    private List<GameObject> cubeObjectsDown = new List<GameObject>();


    [SerializeField] private Shader customShader;
    private void UpdateCubeReferences()
    {
        // Clear previous references
        cubeObjectsUp.Clear();
        cubeObjectsDown.Clear();

        Transform up = transform.Find("up");
        Transform down = transform.Find("down");

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


    private void Start()
    {
        // Store the initial rotation as closed state
        closedRotationY = handleDoorPrefab.transform.localEulerAngles.y;

        // Determine open rotation based on door's initial rotation
        if (Mathf.Round(transform.localEulerAngles.y) == 270) // -90 equivalent
        {
            openRotationY = closedRotationY + 85f;
        }
        else
        {
            openRotationY = closedRotationY - 85f;
        }
    }

    public void OpenDoor()
    {
        if (handleDoorPrefab == null)
        {
            Debug.LogError("handleDoorPrefab is null!");
            return;
        }

        isOpen = !isOpen; // Toggle state

        // Determine target rotation
        targetYRotation = isOpen ? openRotationY : closedRotationY;

        // Set the rotation immediately (or you could lerp it over time)
        Vector3 currentRotation = handleDoorPrefab.transform.localEulerAngles;
        handleDoorPrefab.transform.localEulerAngles = new Vector3(
            currentRotation.x,
            targetYRotation,
            currentRotation.z
        );
    }

    public void ChangeDoorPrefab(GameObject newPrefab)
    {
        if (handleDoorPrefab.transform.childCount > 0)
        {
            foreach (Transform child in handleDoorPrefab.transform)
            {
                Destroy(child.gameObject);
            }
        }

        GameObject newDoor = Instantiate(newPrefab, handleDoorPrefab.transform);
        newDoor.transform.localPosition = new Vector3(-0.45f, 0, 0);

        // Update references after changing prefab
        UpdateCubeReferences();
    }

    public void ChangeAllCubeColors(Material newMaterial, string direction)
    {
        // Ensure we have updated references
        UpdateCubeReferences();

        List<GameObject> targetCubes = direction.ToLower() == "up" ? cubeObjectsUp : cubeObjectsDown;

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