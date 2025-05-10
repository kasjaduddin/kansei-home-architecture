using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "HomeDesignData", menuName = "ScriptableObjects/HomeDesignData")]
public class HomeDesignData : ScriptableObject
{
    public string homeDesignName;    
    public int roomAmount;           
    public int bathroomAmount;       
    public GameObject homePrefab;    
}

public enum homeType
{
    Type_36,
    Type_45,
    Type_54
}
public enum HomeStyle
{
    Modern,        
    Minimalist,     
    Traditional,  
    Other           
}

[System.Serializable]
public class HomeDesignSelector
{
    public homeType selectedHomeType;
    public HomeStyle selectedHomeStyle;
    public int selectedDesignIndex;
}