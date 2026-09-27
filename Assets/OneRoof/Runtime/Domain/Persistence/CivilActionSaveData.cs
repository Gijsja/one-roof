using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class CivilActionSaveData
    {
        public int version;
        public CivilActionRecordSaveData[] actions;
    }

    [Serializable]
    public sealed class CivilActionRecordSaveData
    {
        public int kind;
        public int phase;
        public string factionId;
        public string onsetCause;
        public int[] affectedResidentIds;
        public int[] affectedFloors;
        public long phaseStartedTick;
        public int warningDays;
        public int activeDays;
        public int cooldownDays;
        public long lastEvaluationTick;
    }
}
