using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayDesignManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject designUIPrefab;
    [SerializeField] private List<HomeDesignData> homeDesigns;
    [SerializeField] private DisplayMiniDesign displayMiniDesign;
    private List<Button> instantiatedButtons = new List<Button>();

    private void Start()
    {
        DisplayAllDesigns();
    }

    private void DisplayAllDesigns()
    {
        // Bersihkan contentParent dan list button
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        instantiatedButtons.Clear();

        foreach (HomeDesignData design in homeDesigns)
        {
            GameObject newUI = Instantiate(designUIPrefab, contentParent);

            // Dapatkan komponen tombol langsung dari prefab
            Button button = newUI.GetComponent<Button>();
            if (button != null)
            {
                instantiatedButtons.Add(button);

                // Capture data desain untuk digunakan dalam listener
                HomeDesignData capturedDesign = design;
                button.onClick.AddListener(() => OnDesignButtonClicked(capturedDesign));
            }

            // Update tampilan UI prefab menggunakan komponen lain
            HomeDesignUIOption uiOption = newUI.GetComponent<HomeDesignUIOption>();
            if (uiOption != null)
            {
                string roomCapacity = design.roomAmount.ToString();
                string bathroomCapacity = design.bathroomAmount.ToString();
                uiOption.UpdateProperty(null, design.homeDesignName, roomCapacity, bathroomCapacity);
            }
        }
    }

    private void OnDesignButtonClicked(HomeDesignData design)
    {
        if (design != null && displayMiniDesign != null)
        {
            displayMiniDesign.UpdateHomeDisplay(design.homePrefab);
        }
    }
}