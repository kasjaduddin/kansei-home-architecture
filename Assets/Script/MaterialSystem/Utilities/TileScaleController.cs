using UnityEngine;

/// <summary>
/// Utility class for tile scaling calculations.
/// Provides static helper methods for calculating and applying UV tiling
/// based on surface dimensions and desired tile size.
/// 
/// Used by ColoringManager to implement tile size customization feature.
/// </summary>
public static class TileScaleController
{
    /// <summary>
    /// Default tile size in meters (60cm x 60cm).
    /// </summary>
    public const float DEFAULT_TILE_SIZE = 0.6f;

    /// <summary>
    /// Calculates UV tiling for a surface based on tile size.
    /// </summary>
    /// <param name="surfaceSize">Physical size of the surface in meters</param>
    /// <param name="tileSizeInMeters">Desired tile size in meters (e.g., 0.6 for 60cm)</param>
    /// <param name="useXZ">True for floor/ceiling (uses X and Z), false for wall (uses X and Y)</param>
    /// <returns>UV tiling vector (X and Y scale)</returns>
    public static Vector2 CalculateTiling(
        Vector3 surfaceSize,
        float tileSizeInMeters,
        bool useXZ = true
    )
    {
        float tilingX, tilingY;

        if (useXZ)
        {
            // Floor and ceiling use X (width) and Z (depth)
            tilingX = surfaceSize.x / tileSizeInMeters;
            tilingY = surfaceSize.z / tileSizeInMeters;
        }
        else
        {
            // Walls use X (width) and Y (height)
            tilingX = surfaceSize.x / tileSizeInMeters;
            tilingY = surfaceSize.y / tileSizeInMeters;
        }

        return new Vector2(tilingX, tilingY);
    }

    /// <summary>
    /// Applies tile scaling to a renderer's material.
    /// Handles different texture property names (URP, Built-in, Custom shaders).
    /// </summary>
    /// <param name="renderer">Renderer component to apply tiling to</param>
    /// <param name="tileSizeInMeters">Tile size in meters</param>
    /// <param name="useXZ">True for floor/ceiling (XZ), false for wall (XY)</param>
    public static void ApplyTileScaling(
        Renderer renderer,
        float tileSizeInMeters,
        bool useXZ = true
    )
    {
        if (renderer == null || renderer.material == null)
        {
            Debug.LogWarning("TileScaleController: Renderer or material is null");
            return;
        }

        // Calculate tiling based on surface bounds
        Vector3 surfaceSize = renderer.bounds.size;
        Vector2 tiling = CalculateTiling(surfaceSize, tileSizeInMeters, useXZ);

        // Try to apply to material using various property names
        Material mat = renderer.material;

        // Try common texture property names
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTextureScale("_MainTex", tiling);
        }
        else if (mat.HasProperty("_BaseMap"))
        {
            // URP Lit shader
            mat.SetTextureScale("_BaseMap", tiling);
        }
        else if (mat.HasProperty("_MainTexture"))
        {
            // Custom shader (from existing ColoringManager)
            mat.SetTextureScale("_MainTexture", tiling);
        }
        else
        {
            Debug.LogWarning($"TileScaleController: No supported texture property found on material {mat.name}");
        }
    }

    /// <summary>
    /// Checks if a material should have tile scaling applied.
    /// Based on material name patterns.
    /// </summary>
    /// <param name="material">Material to check</param>
    /// <returns>True if material should be tileable</returns>
    public static bool IsTileableMaterial(Material material)
    {
        if (material == null) return false;

        string name = material.name.ToLower();

        // Check for common tile/ceramic/marble keywords in name
        return name.Contains("tile") ||
               name.Contains("ceramic") ||
               name.Contains("marble") ||
               name.Contains("brick");
    }

    /// <summary>
    /// Converts TileSize enum to meters.
    /// </summary>
    /// <param name="tileSize">TileSize enum value</param>
    /// <returns>Size in meters</returns>
    public static float TileSizeToMeters(TileSize tileSize)
    {
        return (int)tileSize / 100f;
    }

    /// <summary>
    /// Gets all available tile sizes.
    /// </summary>
    /// <returns>Array of TileSize enum values</returns>
    public static TileSize[] GetAvailableTileSizes()
    {
        return new TileSize[]
        {
            TileSize.Small_30cm,
            TileSize.Medium_60cm,
            TileSize.Large_90cm
        };
    }
}