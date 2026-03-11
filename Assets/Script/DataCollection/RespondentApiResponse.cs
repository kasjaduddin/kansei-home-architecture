using System;

namespace VRHomeArch.DataCollection
{
    // Matches the JSON shape returned by GET /active-respondent.
    // JsonUtility requires fields to be public — not using properties here intentionally.
    [Serializable]
    public class RespondentApiResponse
    {
        public string respondentId;
        public string layoutPrefabName;
        public int nextCombinationIndex;
        public string combinationId;
        public bool isComplete;
    }

    // Matches the JSON body sent to POST /combination-done.
    [Serializable]
    public class CombinationDoneRequest
    {
        public string respondentId;
        public int completedIndex;
    }
}