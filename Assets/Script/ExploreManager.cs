using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class ExploreManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown HomeTypeDropdown;
    [SerializeField] private Button BackButton;
    [SerializeField] private Button ContinueButton;

    private UICanvasManager canvasManager;
    private DisplayDesignManager displayManager;
    private void Start()
    {
        canvasManager = FindObjectOfType<UICanvasManager>();
        displayManager = FindObjectOfType<DisplayDesignManager>();

        BackButton.onClick.AddListener(() =>
        {
            canvasManager.ActivateCanvas(UICanvasManager.CanvasType.MainCanvas);
        });

        ContinueButton.onClick.AddListener(() =>
        {
            if (DesignSelectionManager.Instance.SelectedDesignPrefab != null)
            {
                Loader.Load(Loader.Scene.EditDesign);
            }
            else
            {
                Debug.LogWarning("No design selected to proceed!");
            }
        });

        PopulateDropdownWithEnumValues();
        HomeTypeDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void PopulateDropdownWithEnumValues()
    {
        HomeTypeDropdown.ClearOptions();

        List<string> options = new List<string> { "All Designs" }; // Add this manually

        // Add enum names
        options.AddRange(Enum.GetNames(typeof(homeType)));

        HomeTypeDropdown.AddOptions(options);
    }

    private void OnDropdownValueChanged(int index)
    {
        if (displayManager == null) return;

        if (index == 0)
        {
            // Show all designs
            Debug.Log("Showing all designs");
            displayManager.DisplayAllDesigns();
        }
        else
        {
            homeType selectedType = (homeType)(index - 1); // Adjust for "All Designs" offset
            Debug.Log($"Selected Home Type: {selectedType}");
            displayManager.DisplayDesignsByType(selectedType);
        }
    }
}