using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable] public sealed class FactionSaveData
    {
        public int version;
        public long lastEvaluationTick;
        public RelationshipEdgeSaveData[] edges;
        public FactionSupportSaveData[] supports;
        public FactionRecordSaveData[] factions;
    }
    [Serializable] public sealed class RelationshipEdgeSaveData
    { public int first; public int second; public float affinity; public long lastContactTick; public string cause; }
    [Serializable] public sealed class FactionSupportSaveData
    { public int residentId; public string factionId; public float support; public int sustainedDays; public string driver; public int homeFloor; }
    [Serializable] public sealed class FactionRecordSaveData
    { public string id; public float pressure; public float previousPressure; public string topGrievance; }
}
