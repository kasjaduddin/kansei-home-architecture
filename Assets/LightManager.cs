using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    public void ChangeLightPrefab(GameObject newPrefab)
    {
        // Destroy existing children safely
        for (int i = this.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(this.transform.GetChild(i).gameObject);
        }

        // Instantiate new prefab as child
        GameObject newInstance = Instantiate(newPrefab, this.transform);
        newInstance.transform.localPosition = Vector3.zero; // Reset position
    }
    public void ChangeLightIntensity(float intensity)
    {
        // Get all Light components in the children
        Light[] lights = GetComponentsInChildren<Light>();

        foreach (Light light in lights)
        {
            light.intensity = intensity; // Change intensity
        }
    }
}
