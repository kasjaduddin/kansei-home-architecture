using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayMiniDesign : MonoBehaviour
{
    [SerializeField] private Transform pivotDesign;

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
        }
        else
        {
            Debug.LogWarning("HomeStructureManager not found on instantiated design.");
        }
    }
}
