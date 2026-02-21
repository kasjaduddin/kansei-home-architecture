using UnityEngine;

// One asset per floor+wall pairing. Ceiling is not varied in this study.
// 12 assets cover all combinations (3 floor x 4 wall) across both layouts.
namespace VRHomeArch.DataCollection
{
    [CreateAssetMenu(fileName = "Combination_XX", menuName = "VRHomeArch/MaterialCombination")]
    public class MaterialCombination : ScriptableObject
    {
        [SerializeField] private string _combinationId;
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _wallMaterial;

        // Ceiling material intentionally excluded - fixed per study protocol
        public string CombinationId => _combinationId;
        public Material FloorMaterial => _floorMaterial;
        public Material WallMaterial => _wallMaterial;
    }
}