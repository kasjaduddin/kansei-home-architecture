using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeDesignParentManager : MonoBehaviour
{
    [SerializeField] private Light lightPrefabs;
    [SerializeField] private GameObject XROrigin;
    [SerializeField] private Preview2DDesignManager preview2DDesign;
    [SerializeField] private PlacementSystem placementSystem;
    public static HomeDesignParentManager Instance { get; private set; }

    private GameObject currentHouseInstance;
    private Light currentDirectionalLight;
    private HomeStructureManager currentHomeStructureManager;
    public List<GameObject> wallReference;

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
            return;
        }

        //  Move the XROrigin to the initial position
        if (currentHomeStructureManager.initialPosition != null && XROrigin != null)
        {
            // Preserve current Y position of the XR Origin
            Vector3 targetPosition = currentHomeStructureManager.initialPosition.position;
            Vector3 newPosition = new Vector3(targetPosition.x, XROrigin.transform.position.y, targetPosition.z);
            XROrigin.transform.position = newPosition;

            // Apply only the Y rotation (yaw)
            Quaternion targetRotation = currentHomeStructureManager.initialPosition.rotation;
            Vector3 currentEuler = XROrigin.transform.rotation.eulerAngles;
            XROrigin.transform.rotation = Quaternion.Euler(currentEuler.x, targetRotation.eulerAngles.y, currentEuler.z);
        }
        else
        {
            Debug.LogWarning("InitialPosition or XROrigin is null.");
        }

        wallReference = currentHomeStructureManager.GetRoomObjectTransforms();
        InitializeWallsImmediately();

        RefreshLightingOnInstance(currentHouseInstance);
        preview2DDesign.Initialize();
    }

    private void InitializeWallsImmediately()
    {
        if (wallReference == null) return;

        foreach (GameObject wall in wallReference)
        {
            Transform wallTrans = wall.transform;
            placementSystem.PopulateWallObjectsStatic(wallTrans, placementSystem.GetWallData());
        }
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

    public List<GameObject> GetCurrentWallReference()
    {
        return wallReference;
    }

    public Light GetCurrentDirectionalLight()
    {
        return currentDirectionalLight;
    }
}
