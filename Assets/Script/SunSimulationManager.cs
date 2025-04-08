using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static ColoringManager;

public class SunSimulationManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown sunDirectionDropdown;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private Slider lightSlider;
    [SerializeField] private Light sunLight;

    private void Start()
    {
        // Initialize dropdown
        sunDirectionDropdown.ClearOptions();
        sunDirectionDropdown.AddOptions(new List<string> { "Utara", "Timur", "Selatan", "Barat" });
        sunDirectionDropdown.onValueChanged.AddListener(OnDirectionChanged);

        // Initialize slider (6AM-6PM)
        timeSlider.minValue = 0;    // 6:00 (0°)
        timeSlider.maxValue = 12;   // 18:00 (180°)
        timeSlider.wholeNumbers = true;
        timeSlider.onValueChanged.AddListener(OnTimeChanged);
        lightSlider.onValueChanged.AddListener(OnIntensityChange);
        // Set initial state
        UpdateSunRotation(0, 6); // Start at 6 AM facing North
    }
    private void OnIntensityChange(float lightIntensity)
    {
        HomeDesignParentManager parentManager = FindObjectOfType<HomeDesignParentManager>();
        if (parentManager != null)
        {
            HomeStructureManager homeManager = parentManager.GetCurrentHomeStructureManager();
            if (homeManager != null)
            {
                var room = homeManager.GetNearestRoom(); // your own method

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
                            lightManager.ChangeLightIntensity(lightIntensity);
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("HomeDesignParentManager not found in the scene!");
        }
        
    }


    private void OnDirectionChanged(int directionIndex)
    {
        UpdateSunRotation(directionIndex, timeSlider.value);
    }

    private void OnTimeChanged(float hoursSince6AM)
    {
        UpdateSunRotation(sunDirectionDropdown.value, hoursSince6AM);

        // Update time display
        int hour = Mathf.FloorToInt(hoursSince6AM + 6);
        timeText.text = $"Jam {hour:00}:00";
    }

    private void UpdateSunRotation(int directionIndex, float hoursSince6AM)
    {
        float xRotation = hoursSince6AM * 15f;

        // Get Y rotation based on the dropdown selection (0° = North, 90° = East, etc.)
        float yRotation = directionIndex * 90f;

        // Use Quaternion.LookRotation to prevent the Z-axis from flipping
        Vector3 sunDirection = Quaternion.Euler(xRotation, yRotation, 0) * Vector3.forward;
        sunLight.transform.rotation = Quaternion.LookRotation(sunDirection, Vector3.up);
    }
}
