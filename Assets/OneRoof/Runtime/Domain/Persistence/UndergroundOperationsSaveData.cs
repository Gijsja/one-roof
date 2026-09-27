using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class UndergroundOperationsSaveData
    {
        public int supplies;
        public int intel;
        public int researchPoints;
        public float exposure;
        public int coverPriority;
        public int staffingPriority;
        public int securityPosture;
        public int[] assignedResidentIds;
        public int[] assignedRoomIds;
        public int disruptedRoomId;
        public long disruptionEndsAtTick;
        public int investigatorPhase;
        public int investigatorTargetRoomId;
        public int investigatorX;
        public int investigatorDepth;
        public int investigatorPhaseTicks;
        public long lastVisitTick;
        public int securityStrength;
        public int vaultProtection;
        public int backupPowerCapacity;
        public float repairBoost;
        public float careBoost;
        public float commonsMoraleBoost;
        public float trainingBoost;
        public int shelterCapacity;
        public int lastContractIncome;
        public int lastDailyCost;
        public int[] investigatorRouteX;
        public int[] investigatorRouteDepth;
        public int investigatorRouteIndex;
    }
}
