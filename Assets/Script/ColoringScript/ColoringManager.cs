using System.Collections.Generic;
using UnityEngine;
using static HomeStructureManager;

public class ColoringManager : MonoBehaviour
{
    [Header("Dependencies")]
    //[SerializeField] private HomeStructureManager homeManager;

    [SerializeField] private Shader customShader;
    private Shader urpShader;
    public enum ObjectType
    {
        Wall,
        Ceiling,
        Floor
    }

    private void Awake()
    {
        // Fallback to URP Lit shader if not assigned
        if (urpShader == null)
        {
            urpShader = Shader.Find("Universal Render Pipeline/Lit");
        }
    }
    public void ChangeRoomMaterial(Material newMaterial, ObjectType objectType)
    {
        HomeDesignParentManager parentManager = FindObjectOfType<HomeDesignParentManager>();
        if (parentManager != null)
        {
            HomeStructureManager homeManager = parentManager.GetCurrentHomeStructureManager();
            if (homeManager != null)
            {
                var nearestRoom = homeManager.GetNearestRoom();
                if (nearestRoom == null)
                {
                    Debug.LogWarning("No rooms found!");
                    return;
                }

                List<GameObject> targetObjects = null;
                switch (objectType)
                {
                    case ObjectType.Wall:
                        targetObjects = nearestRoom.wallObject;
                        break;
                    case ObjectType.Ceiling:
                        targetObjects = nearestRoom.ceilingObject;
                        break;
                    case ObjectType.Floor:
                        targetObjects = nearestRoom.floorObject;
                        break;
                }

                if (objectType == ObjectType.Wall)
                {
                    homeManager.ChangeWindowAndDoorWallMaterial(nearestRoom, newMaterial);
                }

                if (targetObjects != null)
                {
                    foreach (GameObject obj in targetObjects)
                    {
                        ApplyCustomMaterial(obj, newMaterial);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("HomeDesignParentManager not found in the scene!");
        }
        
    }


    public void ChangeWindowAndDoorWallMaterial(HomeStructureManager.RoomReference room, Material newMaterial)
    {
        foreach (var window in room.windowObject)
        {
            if (window?.objectPrefab == null) continue;

            var windowManager = window.objectPrefab.GetComponent<WindowManager>();
            if (windowManager != null && (window.windowSide == "up" || window.windowSide == "down"))
            {
                windowManager.ChangeAllCubeColors(newMaterial, window.windowSide);
            }
        }

        foreach (var door in room.doorObject)
        {
            if (door?.objectPrefab == null) continue;

            var doorManager = door.objectPrefab.GetComponent<DoorManager>();
            if (doorManager != null && (door.doorSide == "up" || door.doorSide == "down"))
            {
                doorManager.ChangeAllCubeColors(newMaterial, door.doorSide);
            }
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
        if (sourceMaterial.shader == customShader)
        {
            // Already using the desired shader; apply directly
            renderer.material = sourceMaterial;
            return;
        }

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

    /* private void ApplyCustomMaterial(GameObject targetObject, Material newMaterial)
     {
         if (customShader == null || newMaterial == null || targetObject == null)
         {
             Debug.LogError("Missing references when applying material.");
             return;
         }

         Renderer renderer = targetObject.GetComponent<Renderer>();
         if (renderer == null) return;

         Material appliedMaterial = new Material(customShader);
         appliedMaterial.SetFloat("_Tiling", 1f);
         appliedMaterial.SetFloat("_Blend", 1f);
         if (newMaterial.mainTexture != null)
             appliedMaterial.SetTexture("_MainTexture", newMaterial.mainTexture);

         renderer.material = appliedMaterial;
     }*/
}
