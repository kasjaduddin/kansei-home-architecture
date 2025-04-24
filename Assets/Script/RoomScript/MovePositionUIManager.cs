using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovePositionUIManager : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab; // Prefab for each room button
    [SerializeField] private Transform buttonContainer; // UI Panel to hold buttons
    [SerializeField] private Transform playerTransform; // Reference to the player/camera transform that will be moved

    private void Start()
    {
        // Initially clear any existing buttons
        ClearRoomButtons();

        // Generate buttons for all rooms
        DisplayRoomButtons();
    }

    private void DisplayRoomButtons()
    {
        HomeDesignParentManager parentManager = FindObjectOfType<HomeDesignParentManager>();
        if (parentManager != null)
        {
            HomeStructureManager homeManager = parentManager.GetCurrentHomeStructureManager();
            if (homeManager != null)
            {
                // Clear existing buttons
                ClearRoomButtons();

                // Generate buttons for each room
                foreach (var room in homeManager.roomStructure)
                {
                    GameObject buttonObject = Instantiate(buttonPrefab, buttonContainer);
                    Button button = buttonObject.GetComponent<Button>();
                    TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();

                    buttonText.text = room.roomName; // Set button label to room name

                    // When button is clicked, move player to room position
                    button.onClick.AddListener(() => MoveToRoomPosition(room.roomPosition));
                }
            }
            
        }
    }

    private void ClearRoomButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void MoveToRoomPosition(Transform roomPosition)
    {
        if (playerTransform != null && roomPosition != null)
        {
            // Get current player position
            Vector3 currentPosition = playerTransform.position;

            // Get target room position
            Vector3 targetPosition = roomPosition.position;

            // Create new position with:
            // - X from room position
            // - Y from current player position (unchanged)
            // - Z from room position
            Vector3 newPosition = new Vector3(
                targetPosition.x,
                currentPosition.y,
                targetPosition.z
            );

            // Move the player to the new position
            playerTransform.position = newPosition;

            Debug.Log($"Moved to {roomPosition.name} (XZ only)");
        }
        else
        {
            Debug.LogError("Player transform or room position is null!");
        }
    }

    // Optional: If you want to refresh the buttons at runtime (e.g., if rooms are added dynamically)
    public void RefreshRoomButtons()
    {
        DisplayRoomButtons();
    }
}