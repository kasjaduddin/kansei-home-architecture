using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeDesignParentManager : MonoBehaviour
{
    [SerializeField] private Light lightPrefabs;
    public static HomeDesignParentManager Instance { get; private set; }

    private GameObject currentHouseInstance;
    private Light currentDirectionalLight;
    private HomeStructureManager currentHomeStructureManager;
    public List<Transform> wallReference;

    private void Start()
    {
        InstantiateHouse(DesignSelectionManager.Instance.SelectedDesignPrefab);
        currentDirectionalLight = lightPrefabs;
        ChangeLightIntensity(2);
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

        RefreshLightingOnInstance(currentHouseInstance);
    }

    private void RefreshLightingOnInstance(GameObject house)
    {
        Renderer[] renderers = house.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
        }
    }
    private void ChangeLightIntensity(float lightIntensity)
    {
        HomeStructureManager homeManager = GetCurrentHomeStructureManager();
        if (homeManager != null)
        {
            foreach (var room in homeManager.roomStructure)
            {
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
            Debug.LogWarning("Current HomeStructureManager is null!");
        }

    }
    public HomeStructureManager GetCurrentHomeStructureManager()
    {
        return currentHomeStructureManager;
    }

    public List<Transform> GetCurrentWallReference()
    {
        return wallReference;
    }

    public Light GetCurrentDirectionalLight()
    {
        return currentDirectionalLight;
    }
}
