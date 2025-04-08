using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeDesignParentManager : MonoBehaviour
{
    public static HomeDesignParentManager Instance { get; private set; }

    private GameObject currentHouseInstance;
    private HomeStructureManager currentHomeStructureManager;
    public List<Transform> wallReference;

    private void Start()
    {
        InstantiateHouse(DesignSelectionManager.Instance.SelectedDesignPrefab);
    }
    public void InstantiateHouse(GameObject housePrefab)
    {
        if (currentHouseInstance != null)
        {
            Destroy(currentHouseInstance);
        }

        var foundHouse = housePrefab.GetComponent<HomeStructureManager>();
        if (foundHouse == null)
        {
            Debug.LogError("House not found: ");
            return;
        }

        currentHouseInstance = Instantiate(housePrefab, transform);
        currentHomeStructureManager = currentHouseInstance.GetComponent<HomeStructureManager>();

        if (currentHomeStructureManager == null)
        {
            Debug.LogError("Prefab does not contain HomeStructureManager!");
        }

        wallReference = currentHomeStructureManager.GetRoomObjectTransforms();

    }


    public HomeStructureManager GetCurrentHomeStructureManager()
    {
        return currentHomeStructureManager;
    }

    public List<Transform> GetCurrentWallReference()
    {
        return wallReference;
    }
}
