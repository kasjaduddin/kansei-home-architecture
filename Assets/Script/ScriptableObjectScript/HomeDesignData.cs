using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewHomeDesign", menuName = "ScriptableObjects/HomeDesignData", order = 1)]
public class HomeDesignData : ScriptableObject
{
    public string homeDesignName;
    public string homeType;
    public int roomAmount;
    public int bathroomAmount;

    public GameObject homePrefab;
}