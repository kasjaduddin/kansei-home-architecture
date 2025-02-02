using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialCollection", menuName = "Materials/MaterialCollectionSO")]
public class MaterialCollectionSO : ScriptableObject
{
    [Header("Floor Materials")]
    public List<MaterialData> floorMaterials; // Material untuk lantai

    [Header("Wall Materials")]
    public List<MaterialData> wallMaterials; // Material untuk dinding

    [Header("Ceiling Materials")]
    public List<MaterialData> ceilingMaterials; // Material untuk langit-langit

    [Header("Glass Materials")]
    public List<MaterialData> glassMaterials; // Material untuk kaca
}

[System.Serializable]
public class MaterialData
{
    public string materialName;                // Nama material
    public Material material;                  // Material Unity
    public MaterialCategory materialCategory;  // Kategori material
    public float reflectivity;                 // Reflektivitas material
    public bool isTransparent;                 // Apakah material transparan
}

public enum MaterialCategory
{
    Wood,         // Kayu
    Metal,        // Logam
    Glass,        // Kaca
    Plastic,      // Plastik
    Stone,        // Batu
    Concrete,     // Beton
    Ceramic,      // Keramik
    Fabric,       // Kain/Tekstil
    Leather,      // Kulit
    Water,        // Air
    Soil,         // Tanah
    Grass,        // Rumput
    Paper,        // Kertas
    Rubber,        // Karet
    Marmer
}
