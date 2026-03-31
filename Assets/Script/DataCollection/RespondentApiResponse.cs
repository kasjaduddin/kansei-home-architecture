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

    // Matches GET /session-signal response shape.
    // signal is null when no researcher action is pending.
    // Valid non-null values: "break" | "start_baseline" | "start_neutral"
    [System.Serializable]
    public class SessionSignalResponse
    {
        public string signal;
    }

    // Matches the JSON body sent to POST /combination-done.
    [Serializable]
    public class CombinationDoneRequest
    {
        public string respondentId;
        public int completedIndex;
    }
}