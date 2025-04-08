using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HomeDesignCollection", menuName = "ScriptableObjects/HomeDesignCollection", order = 1)]
public class HomeDesignCollection : ScriptableObject
{
    public List<HomeDesignGroup> homeDesignGroups; 
}

[System.Serializable]
public class HomeDesignGroup
{
    public homeType homeType; 
    public HomeStyle homeStyle; 
    public List<HomeDesignData> homeDesigns; 
}

[System.Serializable]
public class HomeDesignData
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
    Type_54,
    Type_70
}
public enum HomeStyle
{
    Modern,        
    Minimalist,     
    Traditional,  
    Other           
}
