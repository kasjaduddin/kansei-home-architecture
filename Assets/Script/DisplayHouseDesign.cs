using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplayHouseDesign : MonoBehaviour
{
    [SerializeField] private Transform pivotDesign;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI houseNameText;
    [SerializeField] private TextMeshProUGUI roomAmountText;
    [SerializeField] private TextMeshProUGUI bathroomAmountText;
    [SerializeField] private Button continueButton;
    [SerializeField] private HomeDesignCollection homeDesignCollection;
    [SerializeField] private HomeDesignSelector homeDesignSelector;


    private void Start()
    {
        var selectedDesign = GetSelectedHomeDesign();
        if (selectedDesign != null)
        {
            UpdateHomeDisplay(selectedDesign.homePrefab);
            SaveDesignPrefab(selectedDesign);
            UpdateTextDisplay(selectedDesign);
        }
        continueButton.onClick.AddListener(() =>
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
    }
    private void SaveDesignPrefab(HomeDesignData design)
    {
        if (design != null)
        {
            DesignSelectionManager.Instance.SelectedDesignPrefab = design.homePrefab;
        }
    }
    private void UpdateTextDisplay(HomeDesignData designData)
    {
        typeText.text = homeDesignSelector.selectedHomeType.ToString();
        houseNameText.text = designData.homeDesignName;
        roomAmountText.text = $"Rooms: {designData.roomAmount}";
        bathroomAmountText.text = $"Bathrooms: {designData.bathroomAmount}";
    }
    public void UpdateHomeDisplay(GameObject prefabDesign)
    {
        foreach (Transform child in pivotDesign)
        {
            Destroy(child.gameObject);
        }
        GameObject instantiatedDesign = Instantiate(prefabDesign, pivotDesign);

        instantiatedDesign.transform.localPosition = Vector3.zero;
        instantiatedDesign.transform.localRotation = Quaternion.identity;

        instantiatedDesign.transform.localScale = Vector3.one * 0.1f;
        var structureManager = instantiatedDesign.GetComponent<HomeStructureManager>();
        if (structureManager != null)
        {
            structureManager.SetCeilingVisibility(false);
            structureManager.SetLampsVisibility(false);
        }
        else
        {
            Debug.LogWarning("HomeStructureManager not found on instantiated design.");
        }
    }

    private HomeDesignData GetSelectedHomeDesign()
    {
        foreach (var group in homeDesignCollection.homeDesignGroups)
        {
            if (group.homeType == homeDesignSelector.selectedHomeType &&
                group.homeStyle == homeDesignSelector.selectedHomeStyle)
            {
                var designs = group.homeDesigns;
                if (homeDesignSelector.selectedDesignIndex >= 0 &&
                    homeDesignSelector.selectedDesignIndex < designs.Count)
                {
                    return designs[homeDesignSelector.selectedDesignIndex];
                }
                else
                {
                    Debug.LogWarning("Selected design index is out of range.");
                }
            }
        }
        Debug.LogWarning("Matching HomeDesignGroup not found.");
        return null;
    }
}
