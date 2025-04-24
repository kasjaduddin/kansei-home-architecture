using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialCollection", menuName = "Materials/MaterialCollectionSO")]
public class MaterialCollectionSO : ScriptableObject
{
    public List<MaterialData> materials; // Unified list, usage is defined per entry
}

[System.Serializable]
public class MaterialData
{
    public string materialName;                     // Display name
    public Material material;                       // Unity Material asset
    public MaterialUsage usage;                     // Where it's used (Wall/Floor/Ceiling/etc)
    public MaterialCategory category;               // Material type (Wood/Tile/Paint/etc)
}

[System.Flags]
public enum MaterialUsage
{
    None = 0,
    Wall = 1 << 0,  // 1
    Floor = 1 << 1,  // 2
    Ceiling = 1 << 2,  // 4
}

public enum MaterialCategory
{
    Paint,
    Wood,
    Tile,
    Marble,
    Stone,
    Concrete,
    Carpet,
    Wallpaper,
    Brick,
    Texture
}