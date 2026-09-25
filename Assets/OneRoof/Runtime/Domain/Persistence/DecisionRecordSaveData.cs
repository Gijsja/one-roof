using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class DecisionRecordSaveData
    {
        public int version;
        public long nextId;
        public DecisionEntrySaveData[] entries;
    }

    [Serializable]
    public sealed class DecisionEntrySaveData
    {
        public long id;
        public long tick;
        public string kind;
        public string title;
        public string cause;
        public string immediateEffect;
        public string oldSetting;
        public string newSetting;
        public string factionId;
        public string phase;
        public bool active;
        public int[] residentIds;
        public int[] householdIds;
        public int[] businessIds;
        public int[] floorIds;
        public DecisionObservationSaveData[] observations;
    }

    [Serializable]
    public sealed class DecisionObservationSaveData
    {
        public long tick;
        public long treasury;
        public long householdCash;
        public long businessCash;
        public long rentReceipts;
        public long taxReceipts;
        public long subsidyExpense;
        public float satisfaction;
        public float strain;
        public float scrutiny;
        public float recentPolicyPressure;
        public float[] factionPressures;
        // First eight stable affected IDs, aligned with the owning entry's sorted ID arrays.
        public long[] householdBalances;
        public long[] businessBalances;
    }
}
