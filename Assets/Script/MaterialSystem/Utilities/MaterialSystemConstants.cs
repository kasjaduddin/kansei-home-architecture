/// <summary>
/// Constants used throughout the Material System.
/// Centralized location for default values, limits, and configuration.
/// </summary>
public static class MaterialSystemConstants
{
    #region Tile Size Constants

    /// <summary>
    /// Default tile size in meters (60cm x 60cm).
    /// </summary>
    public const float DEFAULT_TILE_SIZE = 0.6f;

    /// <summary>
    /// Minimum allowed tile size in meters (10cm).
    /// </summary>
    public const float MIN_TILE_SIZE = 0.1f;

    /// <summary>
    /// Maximum allowed tile size in meters (150cm).
    /// </summary>
    public const float MAX_TILE_SIZE = 1.5f;

    #endregion

    #region Material Counts

    /// <summary>
    /// Expected total number of floor materials.
    /// </summary>
    public const int EXPECTED_FLOOR_MATERIALS = 68;

    /// <summary>
    /// Expected total number of wall materials.
    /// </summary>
    public const int EXPECTED_WALL_MATERIALS = 58;

    /// <summary>
    /// Expected total number of ceiling materials.
    /// </summary>
    public const int EXPECTED_CEILING_MATERIALS = 45;

    /// <summary>
    /// Total expected materials across all surfaces.
    /// </summary>
    public const int TOTAL_MATERIALS = EXPECTED_FLOOR_MATERIALS + EXPECTED_WALL_MATERIALS + EXPECTED_CEILING_MATERIALS;

    #endregion

    #region PlayerPrefs Keys

    /// <summary>
    /// Prefix for all material system PlayerPrefs keys.
    /// </summary>
    public const string PREFS_PREFIX = "material_";

    /// <summary>
    /// Suffix for material ID keys.
    /// </summary>
    public const string PREFS_ID_SUFFIX = "_id";

    /// <summary>
    /// Suffix for tile scale keys.
    /// </summary>
    public const string PREFS_SCALE_SUFFIX = "_tileScale";

    /// <summary>
    /// Gets the PlayerPrefs key for a material ID.
    /// </summary>
    /// <param name="surfaceID">Surface identifier</param>
    /// <returns>PlayerPrefs key string</returns>
    public static string GetMaterialIDKey(string surfaceID)
    {
        return PREFS_PREFIX + surfaceID + PREFS_ID_SUFFIX;
    }

    /// <summary>
    /// Gets the PlayerPrefs key for a tile scale.
    /// </summary>
    /// <param name="surfaceID">Surface identifier</param>
    /// <returns>PlayerPrefs key string</returns>
    public static string GetTileScaleKey(string surfaceID)
    {
        return PREFS_PREFIX + surfaceID + PREFS_SCALE_SUFFIX;
    }

    #endregion

    #region UI Constants

    /// <summary>
    /// Maximum number of materials to display in grid at once.
    /// </summary>
    public const int MAX_VISIBLE_MATERIALS = 30;

    /// <summary>
    /// Preferred width for material preview sprites in pixels.
    /// </summary>
    public const int PREVIEW_SPRITE_SIZE = 256;

    #endregion

    #region Performance Constants

    /// <summary>
    /// Target frame rate for VR (Meta Quest 2).
    /// </summary>
    public const int TARGET_FRAME_RATE = 72;

    /// <summary>
    /// Maximum time in milliseconds for material operations.
    /// </summary>
    public const float MAX_MATERIAL_OP_TIME_MS = 100f;

    #endregion
}