using UnityEngine;

// Attached to the root of each house layout prefab.
// Applies a MaterialCombination to interior floor, wall, and ceiling renderers.
// Bathroom surfaces and exterior walls are intentionally excluded - fixed per study protocol.
public class MaterialApplicator : MonoBehaviour
{
    [SerializeField] private Renderer[] _floorRenderers;
    [SerializeField] private Renderer[] _wallRenderers;
    [SerializeField] private Renderer[] _ceilingRenderers;

    public void ApplyCombination(VRHomeArch.DataCollection.MaterialCombination combination)
    {
        if (combination == null)
        {
            Debug.LogError($"[MaterialApplicator] Null combination on {gameObject.name}");
            return;
        }

        ApplyToRenderers(_floorRenderers, combination.FloorMaterial, "floor");
        ApplyToRenderers(_wallRenderers, combination.WallMaterial, "wall");
        // Ceiling material not varied in this study - renderers cached but not changed
    }

    private void ApplyToRenderers(Renderer[] renderers, Material material, string label)
    {
        if (material == null)
        {
            Debug.LogWarning($"[MaterialApplicator] {label} material is null - skipping");
            return;
        }
        foreach (Renderer r in renderers)
        {
            if (r != null) r.material = material;
        }
    }

    [ContextMenu("Auto-Populate Renderers")]
    private void AutoPopulateRenderers()
    {
        var floors = new System.Collections.Generic.List<Renderer>();
        var walls = new System.Collections.Generic.List<Renderer>();
        var ceilings = new System.Collections.Generic.List<Renderer>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(includeInactive: true))
        {
            string n = r.gameObject.name.ToLower();

            // Bathroom surfaces and exterior wall are fixed - excluded from combination apply
            // WallTile is a decorative tile surface (e.g. kitchen backsplash) - excluded separately
            if (n.EndsWith("_bathroom") || n == "wall_outside" || n.StartsWith("walltile"))
                continue;

            if (n.StartsWith("floor_"))
                floors.Add(r);
            else if (n.StartsWith("ceiling_"))
                ceilings.Add(r);
            else if (n.Contains("wall"))
                walls.Add(r);
        }

        _floorRenderers = floors.ToArray();
        _wallRenderers = walls.ToArray();
        _ceilingRenderers = ceilings.ToArray();

        Debug.Log($"[MaterialApplicator] Populated: {_floorRenderers.Length} floor, " +
                  $"{_wallRenderers.Length} wall, {_ceilingRenderers.Length} ceiling renderers");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}