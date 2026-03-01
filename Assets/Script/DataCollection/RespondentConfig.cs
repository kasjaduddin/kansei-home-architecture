using System;
using System.Collections.Generic;

// Data-only classes for JSON deserialization via JsonUtility.
// Kept in a dedicated file to isolate serialization concerns from session logic.
namespace VRHomeArch.DataCollection
{
    [Serializable]
    public class SessionEntry
    {
        public int sessionIndex;
        public string layoutPrefabName;  // Must match prefab name inside Resources/HouseLayouts/Type36/
        public string combinationId;     // e.g. "C01" - matches MaterialCombination.CombinationId
    }

    [Serializable]
    public class RespondentConfig
    {
        public string respondentId;
        public List<SessionEntry> sessions;
    }
}