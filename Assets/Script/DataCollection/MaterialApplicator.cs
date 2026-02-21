using UnityEngine;
using VRHomeArch.DataCollection;

// Attached to the root of each house layout prefab.
// Applies a MaterialCombination to all floor and wall renderers in the layout.
// Renderer arrays are populated once (via ContextMenu or on Awake) rather than
// searched at apply-time, to avoid per-frame or per-apply FindObjectsOfType calls.
public class MaterialApplicator : MonoBehaviour
{
    [SerializeField] private Renderer[] _floorRenderers;
    [SerializeField] private Renderer[] _wallRenderers;

    public void ApplyCombination(MaterialCombination combination)
    {
        if (combination == null)
        {
            Debug.LogError($"[MaterialApplicator] ApplyCombination called with null combination on {gameObject.name}");
            return;
        }

        ApplyMaterialToRenderers(_floorRenderers, combination.FloorMaterial, "floor");
        ApplyMaterialToRenderers(_wallRenderers, combination.WallMaterial, "wall");
    }

    private void ApplyMaterialToRenderers(Renderer[] renderers, Material material, string label)
    {
        if (material == null)
        {
            Debug.LogWarning($"[MaterialApplicator] {label} material is null - skipping");
            return;
        }

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.material = material;
        }
    }

    // Scans child GameObjects by name to populate renderer arrays.
    // Call this from the Inspector context menu after prefab setup, not at runtime.
    [ContextMenu("Auto-Populate Renderers")]
    private void AutoPopulateRenderers()
    {
        var floors = new System.Collections.Generic.List<Renderer>();
        var walls = new System.Collections.Generic.List<Renderer>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(includeInactive: true))
        {
            string n = r.gameObject.name.ToLower();

            // Name-based heuristic matching existing prefab naming convention
            if (n.StartsWith("floor"))
                floors.Add(r);
            else if (n.Contains("wall") || n.StartsWith("horizontal") || n.StartsWith("vertical"))
                walls.Add(r);
        }

        _floorRenderers = floors.ToArray();
        _wallRenderers = walls.ToArray();

        Debug.Log($"[MaterialApplicator] Auto-populated: {_floorRenderers.Length} floor renderers, {_wallRenderers.Length} wall renderers");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}