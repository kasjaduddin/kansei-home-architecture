using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayDesignManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject designUIPrefab;
    [SerializeField] private HomeDesignCollectionSO homeDesignCollection;
    [SerializeField] private DisplayMiniDesign displayMiniDesign;
    private List<Button> instantiatedButtons = new List<Button>();

    private void Start()
    {
        DisplayAllDesigns();
    }

    public void DisplayAllDesigns()
    {
        // Bersihkan contentParent dan list button
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        instantiatedButtons.Clear();

        foreach (HomeDesignGroup group in homeDesignCollection.homeDesignGroups)
        {
            foreach (HomeDesignData design in group.homeDesigns)
            {
                GameObject newUI = Instantiate(designUIPrefab, contentParent);

                // Get the Button component from the prefab
                Button button = newUI.GetComponent<Button>();
                if (button != null)
                {
                    instantiatedButtons.Add(button);

                    // Capture data for the button click listener
                    HomeDesignData capturedDesign = design;
                    button.onClick.AddListener(() => OnDesignButtonClicked(capturedDesign));
                }

                // Update the prefab UI with design details
                HomeDesignUIOption uiOption = newUI.GetComponent<HomeDesignUIOption>();
                if (uiOption != null)
                {
                    // UpdateProperty now requires a Sprite. Replace "null" with the actual sprite if available.
                    Sprite placeholderSprite = null; // Replace this with your logic to assign a proper sprite
                    string roomCapacity = design.roomAmount.ToString();
                    string bathroomCapacity = design.bathroomAmount.ToString();
                    uiOption.UpdateProperty(placeholderSprite, design.homeDesignName, roomCapacity, bathroomCapacity);
                }
            }
        }
    }
    public void DisplayDesignsByType(homeType selectedType)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        instantiatedButtons.Clear();

        foreach (HomeDesignGroup group in homeDesignCollection.homeDesignGroups)
        {
            if (group.homeType == selectedType)
            {
                foreach (HomeDesignData design in group.homeDesigns)
                {
                    GameObject newUI = Instantiate(designUIPrefab, contentParent);

                    Button button = newUI.GetComponent<Button>();
                    if (button != null)
                    {
                        instantiatedButtons.Add(button);
                        HomeDesignData capturedDesign = design;
                        button.onClick.AddListener(() => OnDesignButtonClicked(capturedDesign));
                    }

                    HomeDesignUIOption uiOption = newUI.GetComponent<HomeDesignUIOption>();
                    if (uiOption != null)
                    {
                        Sprite placeholderSprite = null;
                        string roomCapacity = design.roomAmount.ToString();
                        string bathroomCapacity = design.bathroomAmount.ToString();
                        uiOption.UpdateProperty(placeholderSprite, design.homeDesignName, roomCapacity, bathroomCapacity);
                    }
                }
            }
        }
    }


    private void OnDesignButtonClicked(HomeDesignData design)
    {
        if (design != null && displayMiniDesign != null)
        {
            displayMiniDesign.UpdateHomeDisplay(design.homePrefab);
            DesignSelectionManager.Instance.SelectedDesignPrefab = design.homePrefab;
        }
    }
}