using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeDesignUIOption : MonoBehaviour
{
    [SerializeField] private Image HomeDesignImage;
    [SerializeField] private TextMeshProUGUI HomeDesignNameText;
    [SerializeField] private TextMeshProUGUI RoomCapacityText;
    [SerializeField] private TextMeshProUGUI BathroomCapacityText;
    [SerializeField] private TextMeshProUGUI HomeTypeText;

    private HomeDesignData homeDesignData;
    private DisplayMiniDesign displayMiniDesign;

    public void Initialize(HomeDesignData data, DisplayMiniDesign displayManager)
    {
        homeDesignData = data;
        displayMiniDesign = displayManager;

        // Update UI elements
        UpdateProperty(null, data.homeDesignName, data.roomAmount.ToString(), data.bathroomAmount.ToString(), data.homeType);
        
    }

    public void OnButtonClick()
    {
        if (homeDesignData != null && displayMiniDesign != null)
        {
            // Call the function to display the design in DisplayMiniDesign
            displayMiniDesign.UpdateHomeDisplay(homeDesignData.homePrefab);
        }
    }

    public void UpdateProperty(Sprite image, string designName, string roomCapacity, string bathroomCapacity, homeType type)
    {
        if (image != null)
        {
            HomeDesignImage.sprite = image;
        }
        if (!string.IsNullOrEmpty(designName))
        {
            HomeDesignNameText.text = designName.ToUpper();
        }
        if (!string.IsNullOrEmpty(roomCapacity))
        {
            RoomCapacityText.text = "ROOM: " + roomCapacity;
        }
        if (!string.IsNullOrEmpty(bathroomCapacity))
        {
            BathroomCapacityText.text = "BATHROOM: " + bathroomCapacity;
        }
        HomeTypeText.text = FormatHomeTypeText(type);
    }
    private string FormatHomeTypeText(homeType type)
    {
        // Convert enum to string and format it as uppercase with space
        string typeStr = type.ToString().Replace("Type_", "TYPE ");
        return typeStr.ToUpper(); // Ensure it's all caps
    }

}
