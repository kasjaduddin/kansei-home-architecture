using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeStructureManager : MonoBehaviour
{
    [Header("Room")]
    public List<RoomReference> roomStructure;

    private List<Transform> cachedTransforms = new List<Transform>();


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

    public RoomReference GetNearestRoom()
    {
        Camera[] allCameras = Camera.allCameras;
        Camera activeCamera = null;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            OpenClosestDoor();
        }
    }

    private Camera GetActiveCamera()
    {
        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.isActiveAndEnabled)
                return cam;
        }
        Debug.LogError("No active camera found!");
        return null;
    }

    private void OpenClosestDoor()
    {
        var nearestRoom = GetNearestRoom();
        if (nearestRoom == null || nearestRoom.doorObject.Count == 0)
        {
            Debug.LogWarning("No doors found in the nearest room!");
            return;
        }

        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null) return;

        Vector3 playerPosition = activeCamera.transform.position;

        HomeStructureManager.doorData closestDoor = null;
        float minDistance = Mathf.Infinity;

        foreach (var door in nearestRoom.doorObject)
        {
            if (door == null || door.objectPrefab == null) continue;

            float distance = Vector3.Distance(playerPosition, door.objectPrefab.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestDoor = door;
            }
        }

        if (closestDoor?.objectPrefab == null) return;

        DoorManager doorManager = closestDoor.objectPrefab.GetComponent<DoorManager>();
        if (doorManager != null)
        {
            doorManager.OpenDoor();
            Debug.Log("Opened door: " + closestDoor.objectPrefab.name);
        }
    }

    public List<Transform> GetRoomObjectTransforms()
    {
        foreach (var room in roomStructure)
        {
            // Wall Objects
            foreach (var wall in room.wallObject)
            {
                if (wall != null && !cachedTransforms.Contains(wall.transform))
                {
                    cachedTransforms.Add(wall.transform);
                }
            }

            // Door Objects
            foreach (var door in room.doorObject)
            {
                if (door?.objectPrefab != null && !cachedTransforms.Contains(door.objectPrefab.transform))
                {
                    cachedTransforms.Add(door.objectPrefab.transform);
                }
            }

            // Window Objects
            foreach (var window in room.windowObject)
            {
                if (window?.objectPrefab != null && !cachedTransforms.Contains(window.objectPrefab.transform))
                {
                    cachedTransforms.Add(window.objectPrefab.transform);
                }
            }
        }

        return cachedTransforms;
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

    public void SetCeilingVisibility(bool isActive)
    {
        foreach (var room in roomStructure)
        {
            foreach (var ceiling in room.ceilingObject)
            {
                if (ceiling != null)
                {
                    ceiling.SetActive(isActive);
                }
            }
        }
    }
}
