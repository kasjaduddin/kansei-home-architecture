using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColoringManager : MonoBehaviour
{
    [Header("Room")]
    public List<RoomReference> roomStructure;    // Furniture di atas lantai

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
        public Transform roomPosition;
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

        // Apply the material change
        if (targetObjects != null)
        {
            foreach (GameObject obj in targetObjects)
            {
                if (obj != null)
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = newMaterial;
                    }
                }
            }
        }
    }

    private RoomReference GetNearestRoom()
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
}
