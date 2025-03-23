using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmbededCollection", menuName = "Embeded/EmbededCollectionSO")]
public class EmbededCollectionSO : ScriptableObject
{
    [Header("Floor Furniture")]
    public List<EmbededData> doorsCollection;
    [Header("Window Furniture")]
    public List<EmbededData> windowsCollection;
    [Header("Lamps Furniture")]
    public List<EmbededData> lampsCollection;
}

[System.Serializable]
public class EmbededData
{
    public string objectName;                    
    public GameObject objectPrefab;            
}

public enum EmbededObjectType
{
    Door,
    Window,
    Lamps
}