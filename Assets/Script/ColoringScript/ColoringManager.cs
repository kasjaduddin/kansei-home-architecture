using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class ColoringManager : MonoBehaviour
{
    [Header("Room")]
    public List<RoomReference> roomStructure;    // Furniture di atas lantai

    [SerializeField] private Shader customShader;
    public enum ObjectType
    {
        Wall,
        Ceiling,
        Floor
    }

    [System.Serializable]
    public class RoomReference
    {
        public string roomName;                     
        public List<GameObject> wallObject;    
        public List<GameObject> ceilingObject;    
        public List<GameObject> floorObject;
        public List<doorData> doorObject;
        public List<windowData> windowObject;
        public List<GameObject> lampsObjects;
        public Transform roomPosition;
    }
    [System.Serializable]
    public class doorData
    {
        public string doorSide;
        public GameObject objectPrefab;
    }
    [System.Serializable]
    public class windowData
    {
        public string windowSide;
        public GameObject objectPrefab;
    }
    public void ChangeRoomMaterial(Material newMaterial, ObjectType objectType)
    {
        RoomReference nearestRoom = GetNearestRoom();

        if (nearestRoom == null)
        {
            Debug.LogWarning("No rooms found!");
            return;
        }

        List<GameObject> targetObjects = null;

        // Determine which objects to change
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
        if (targetObjects == nearestRoom.wallObject)
        {
            ChangeWindowAndDoorWallMaterial(nearestRoom, newMaterial);
        }
        // Apply the material change
        if (targetObjects != null)
        {
            foreach (GameObject obj in targetObjects)
            {
                ApplyCustomMaterial(obj, newMaterial);
            }
        }
    }
    private void ChangeWindowAndDoorWallMaterial(RoomReference room, Material newMaterial)
    {
        // Change material of walls inside window objects
        foreach (windowData window in room.windowObject)
        {
            if (window != null && window.objectPrefab != null)
            {
                WindowManager windowManager = window.objectPrefab.GetComponent<WindowManager>();
                if (windowManager != null)
                {
                    string windowSide = window.windowSide; // Ensure case insensitivity
                    if (windowSide == "up" || windowSide == "down")
                    {
                        windowManager.ChangeAllCubeColors(newMaterial, windowSide);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid doorSide '{window.windowSide}' for door: {window.objectPrefab.name}");
                    }
                }
            }
        }

        // Change material of walls inside door objects (via DoorManager)
        foreach (doorData door in room.doorObject)
        {
            if (door != null && door.objectPrefab != null)
            {
                DoorManager doorManager = door.objectPrefab.GetComponent<DoorManager>();
                if (doorManager != null)
                {
                    string doorSide = door.doorSide; // Ensure case insensitivity
                    if (doorSide == "up" || doorSide == "down")
                    {
                        doorManager.ChangeAllCubeColors(newMaterial, doorSide);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid doorSide '{door.doorSide}' for door: {door.objectPrefab.name}");
                    }
                }
            }
        }
    }
    public RoomReference GetNearestRoom()
    {
        // Get all cameras in the scene
        Camera[] allCameras = Camera.allCameras;
        Camera activeCamera = null;

        // Find the first enabled camera
        foreach (Camera cam in allCameras)
        {
            if (cam.isActiveAndEnabled)
            {
                activeCamera = cam;
                break;
            }
        }

        if (activeCamera == null)
        {
            Debug.LogError("No active camera found!");
            return null;
        }

        // Rest of the code remains the same
        RoomReference nearestRoom = null;
        float minDistance = Mathf.Infinity;

        foreach (RoomReference room in roomStructure)
        {
            if (room.roomPosition == null)
            {
                Debug.LogError("roomPosition is null for room: " + room.roomName);
                continue;
            }

            float distance = Vector3.Distance(activeCamera.transform.position, room.roomPosition.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestRoom = room;
            }
        }

        return nearestRoom;
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
