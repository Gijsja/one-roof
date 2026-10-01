using System;

namespace OneRoof.Domain.Persistence
{
    [Serializable]
    public sealed class CaretakerCrisisSaveData
    {
        public bool practice, attackDucking;
        public int attackSerial;
        public int nextParticipant;
        public int[] floorParticipantIds;
        public int[] encounterResidentIds;
        public CaretakerCrisisSaveData beforePractice;
        public CaretakerResidentFloorSaveData[] residentFloors;
        public int version = 1, phase, warningDays, reliefDays, routeIndex, x, facing = 1, health = 100, attack, attackFrame, jumpFrames, clock, score;
        public long lastEvaluationTick = -1;
        public string factionId, cause;
        public int[] affectedResidentIds, affectedFloors;
        public bool paused, consequencesApplied;
        public CaretakerRouteSaveData[] route;
        public CaretakerOpponentSaveData[] opponents;
        public CaretakerEvictionSaveData[] evictions;
    }

    [Serializable]
    public sealed class CaretakerResidentFloorSaveData
    {
        public int residentId, floor;
    }

    [Serializable]
    public sealed class CaretakerRouteSaveData
    {
        public int floor, minX, maxX, entryX, exitX;
        public bool encounter;
    }

    [Serializable]
    public sealed class CaretakerOpponentSaveData
    {
        public int id, residentId, role, x, health, facing, timer, attackFrame, hitAttack;
    }

    [Serializable]
    public sealed class CaretakerEvictionSaveData
    {
        public int residentId, floor;
        public string factionId, cause;
    }
}
