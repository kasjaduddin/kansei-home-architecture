using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LightingManager : MonoBehaviour
{
    [SerializeField] private Slider lightSlider;
    [SerializeField] private Button backButton;
    [SerializeField] private MenuUIEditorManager menuUICanvas;

    private void Start()
    {
        backButton.onClick.AddListener(() => {
            menuUICanvas.ShowMainMenuCanvas();
        });
        lightSlider.onValueChanged.AddListener(OnIntensityChange);
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
}
