using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HomeDesignCollection", menuName = "ScriptableObjects/HomeDesignCollection")]
public class HomeDesignCollectionSO : ScriptableObject
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