using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HomeStructureManager : MonoBehaviour
{
    [Header("Room")]
    public List<RoomReference> roomStructure;
    public Transform initialPosition;
    public Sprite previewSprite;

    private List<GameObject> cachedTransforms = new List<GameObject>();
    private InputDevice rightController;
    private bool previousButtonState = false;

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
    private void Start()
    {
        TryInitializeController();
    }
    private void TryInitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightController = devices[0];
        }
    }   
    private void Update()
    {
        if (!rightController.isValid)
        {
            TryInitializeController();
            return;
        }

        // Detect A button (primaryButton)
        if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool buttonPressed))
        {
            if (buttonPressed && !previousButtonState)
            {
                OpenClosestDoor();
            }

            previousButtonState = buttonPressed;
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
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null) return;

        Vector3 playerPosition = activeCamera.transform.position;

        doorData closestDoor = null;
        float minDistance = Mathf.Infinity;

        foreach (var room in roomStructure)
        {
            foreach (var door in room.doorObject)
            {
                if (door == null || door.objectPrefab == null) continue;

                float distance = Vector3.Distance(playerPosition, door.objectPrefab.transform.position);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestDoor = door;
                }
            }
        }

        if (closestDoor?.objectPrefab == null) return;

        DoorManager doorManager = closestDoor.objectPrefab.GetComponent<DoorManager>();
        if (doorManager != null)
        {
            doorManager.OpenDoor();
            Debug.Log("Opened closest door: " + closestDoor.objectPrefab.name);
        }
        else
        {
            Debug.LogWarning("No DoorManager found on the closest door.");
        }
    }
    /*private void OpenClosestDoor()
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
    }*/

    public List<GameObject> GetRoomObjectTransforms()
    {
        foreach (var room in roomStructure)
        {
            // Wall Objects
            foreach (var wall in room.wallObject)
            {
                if (wall != null && !cachedTransforms.Contains(wall.gameObject))
                {
                    cachedTransforms.Add(wall.gameObject);
                }
            }

            // Door Objects
            foreach (var door in room.doorObject)
            {
                if (door?.objectPrefab != null && !cachedTransforms.Contains(door.objectPrefab.gameObject))
                {
                    cachedTransforms.Add(door.objectPrefab.gameObject);
                }
            }

            // Window Objects
            foreach (var window in room.windowObject)
            {
                if (window?.objectPrefab != null && !cachedTransforms.Contains(window.objectPrefab.gameObject))
                {
                    cachedTransforms.Add(window.objectPrefab.gameObject);
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
    public void SetLampsVisibility(bool isActive)
    {
        foreach (var room in roomStructure)
        {
            foreach (var lamp in room.lampsObjects)
            {
                if (lamp != null)
                {
                    lamp.SetActive(isActive);
                }
            }
        }
    }
}
