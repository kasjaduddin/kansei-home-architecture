using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ColoringManager;

public class ChangeObjectUIManager : MonoBehaviour
{
    [SerializeField] private EmbededCollectionSO embededDatabase; // Reference to the database
    [SerializeField] private GameObject buttonPrefab; // Prefab for each button
    [SerializeField] private Transform buttonContainer; // UI Panel to hold buttons
    [SerializeField] private ColoringManager roomReference;  //to get the RoomReference 
    [SerializeField] private Button doorsButton;
    [SerializeField] private Button windowsButton;
    [SerializeField] private Button lampsButton;

    private string activeType;

    private void Start()
    {
        doorsButton.onClick.AddListener(() => { UpdatePreviewButtons(EmbededObjectType.Door); });
        windowsButton.onClick.AddListener(() => { UpdatePreviewButtons(EmbededObjectType.Window); });
        lampsButton.onClick.AddListener(() => { UpdatePreviewButtons(EmbededObjectType.Lamps); });

        // Display door materials by default
        UpdatePreviewButtons(EmbededObjectType.Door);
    }

    private void UpdatePreviewButtons(EmbededObjectType objectType)
    {
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Get the appropriate prefab list
        List<EmbededData> prefabList = GetPrefabList(objectType);

        foreach (EmbededData prefabData in prefabList)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

            Button buttonComponent = newButton.GetComponentInChildren<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() =>
                {
                    if (objectType == EmbededObjectType.Door)
                    {
                        ChangeRoomDoors(prefabData.objectPrefab);
                    }
                    else if (objectType == EmbededObjectType.Window)
                    {
                        ChangeRoomWindows(prefabData.objectPrefab);
                    }
                    else
                    {
                        ChangeRoomLight(prefabData.objectPrefab);
                    }
                });
            }
            else
            {
                Debug.LogError("Button component not found in button prefab!");
            }
        }
    }

    private void ChangeRoomDoors( GameObject newDoorPrefab)
    {
        ColoringManager.RoomReference room = roomReference.GetNearestRoom();
        if (room == null)
        {
            Debug.LogWarning("No nearest room found!");
            return;
        }
        foreach (doorData door in room.doorObject)
        {
            if (door != null && door.objectPrefab != null)
            {
                DoorManager doorManager = door.objectPrefab.GetComponent<DoorManager>();
                if (doorManager != null)
                {
                    doorManager.ChangeDoorPrefab(newDoorPrefab);
                }
            }
        }
    }

    private void ChangeRoomLight(GameObject newLightPrefab)
    {
        ColoringManager.RoomReference room = roomReference.GetNearestRoom();
        if (room == null)
        {
            Debug.LogWarning("No nearest room found!");
            return;
        }
        foreach (GameObject light in room.lampsObjects)
        {
            if (light != null)
            {
                LightManager lightManager = light.GetComponent<LightManager>();
                if (lightManager != null)
                {
                    lightManager.ChangeLightPrefab(newLightPrefab);
                }
            }
        }
    }

    private void ChangeRoomWindows(GameObject newWindowsPrefab)
    {
        ColoringManager.RoomReference room = roomReference.GetNearestRoom();
        if (room == null)
        {
            Debug.LogWarning("No nearest room found!");
            return;
        }
        foreach (windowData window in room.windowObject)
        {
            if (window != null && window.objectPrefab != null)
            {
                WindowManager windowManager = window.objectPrefab.GetComponent<WindowManager>();
                if (windowManager != null)
                {
                    windowManager.ChangeWindowPrefab(newWindowsPrefab);
                }
            }
        }
    }

    private List<EmbededData> GetPrefabList(EmbededObjectType objectType)
    {
        switch (objectType)
        {
            case EmbededObjectType.Door:
                return embededDatabase.doorsCollection;
            case EmbededObjectType.Window:
                return embededDatabase.windowsCollection;
            case EmbededObjectType.Lamps:
            default:
                return embededDatabase.lampsCollection;
        }
    }
}

