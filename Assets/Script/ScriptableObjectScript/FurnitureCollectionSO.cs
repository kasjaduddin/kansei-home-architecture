using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FurnitureCollection", menuName = "Furniture/FurnitureCollectionSO")]
public class FurnitureCollectionSO : ScriptableObject
{
    [Header("Floor Furniture")]
    public List<FurnitureData> floorFurniture;    // Furniture di atas lantai
    [Header("Wall Furniture")]
    public List<FurnitureData> wallFurniture;     // Furniture menempel di dinding
    [Header("Ceiling Furniture")]
    public List<FurnitureData> ceilingFurniture;  // Furniture menempel di langit-langit
    [Header("Embedded Furniture")]
    public List<FurnitureData> embeddedFurniture; // Furniture berada di dinding (contoh: pintu/jendela)
}

[System.Serializable]
public class FurnitureData
{
    public string furnitureName;                     // Nama furniture
    public FurnitureType furnitureType;              // Jenis furniture (kelompoknya)
    public FurniturePlacement furniturePlacement;    // Cara furniture diletakkan
    public GameObject furniturePrefab;               // Prefab Unity untuk furniture
    public Vector3 defaultSize;                      // Ukuran default furniture
    public Vector3 defaultPositionOffset;            // Posisi offset default (relatif terhadap titik peletakan)
    public bool isMovable;                           // Apakah furniture bisa dipindahkan
}

public enum FurniturePlacement
{
    OnFloor,        // Diletakkan di atas lantai
    OnWall,         // Menempel di dinding
    OnCeiling,      // Menempel di langit-langit
    EmbeddedInWall  // Berada di dinding (pintu/jendela)
}

public enum FurnitureType
{
    // Ruang Umum
    Seating,         // Kursi, sofa, bangku
    Tables,          // Meja tamu, meja makan
    Storage,         // Lemari, rak buku
    Decorative,      // Vas, lukisan, tanaman hias

    // Elektronik
    Electronics,     // TV, speaker, home theater
    Lighting,        // Lampu meja, lampu gantung, chandelier

    // Ruang Tidur
    Beds,            // Tempat tidur
    Wardrobes,       // Lemari pakaian
    BedsideTables,   // Meja samping tempat tidur

    // Dapur
    KitchenAppliances, // Kulkas, oven, microwave
    KitchenFixtures,   // Sink, rak dapur, kabinet dapur
    KitchenUtensils,   // Rak piring, tempat penyimpanan bumbu

    // Kamar Mandi
    BathroomFixtures,  // Toilet, shower, wastafel
    BathroomStorage,   // Lemari kamar mandi
    BathroomDecor,     // Karpet kamar mandi, tempat sabun

    // Pintu dan Jendela
    DoorsAndWindows, // Pintu, jendela

    // Luar Rumah
    OutdoorFurniture,  // Kursi taman, payung taman, meja luar ruangan
    PoolFurniture,     // Kursi kolam renang, rak handuk luar ruangan

    // Lainnya
    Miscellaneous      // Furniture atau objek lain yang tidak terkelompok
}