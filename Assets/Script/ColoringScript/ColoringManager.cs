using System.Collections.Generic;
using UnityEngine;

public class ColoringManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private HomeStructureManager homeManager;

    [SerializeField] private Shader customShader;

    public enum ObjectType
    {
        Wall,
        Ceiling,
        Floor
    }


    public void ChangeRoomMaterial(Material newMaterial, ObjectType objectType)
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
    }
}
