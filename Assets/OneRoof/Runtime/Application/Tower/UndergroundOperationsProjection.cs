using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Underground;

namespace OneRoof.Application.Tower
{
    /// <summary>Explanation-ready immutable view of the underground operation.</summary>
    public sealed class UndergroundOperationsProjection
    {
        public UndergroundOperationsProjection(UndergroundOperationsState state, UndergroundDigState layout, long tick)
        {
            Supplies = state.Supplies;
            Intel = state.Intel;
            ResearchPoints = state.ResearchPoints;
            Exposure = state.Exposure;
            CoverPriority = state.CoverPriority;
            StaffingPriority = state.StaffingPriority;
            SecurityPosture = state.SecurityPosture;
            Phase = state.Phase;
            InvestigatorX = state.InvestigatorX;
            InvestigatorDepth = state.InvestigatorDepth;
            InvestigatorTargetRoomId = state.InvestigatorTargetRoomId;
            LastContractIncome = state.LastContractIncome;
            LastDailyCost = state.LastDailyCost;
            BackupPowerCapacity = state.BackupPowerCapacity;
            RepairBoost = state.RepairBoost;
            CareBoost = state.CareBoost;
            CommonsMoraleBoost = state.CommonsMoraleBoost;
            ShelterCapacity = state.ShelterCapacity;
            var assigned = new Dictionary<int, List<int>>();
            foreach (var assignment in state.RoomByResident)
            {
                if (!assigned.TryGetValue(assignment.Value, out var residents))
                {
                    residents = new List<int>();
                    assigned.Add(assignment.Value, residents);
                }
                residents.Add(assignment.Key);
            }
            var rooms = new List<UndergroundRoomOperationProjection>(layout.Rooms.Count);
            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var room = layout.Rooms[i];
                assigned.TryGetValue(room.Id, out var residents);
                if (residents != null) residents.Sort();
                var staff = residents?.Count ?? 0;
                var disrupted = state.IsDisrupted(room.Id, tick);
                var cause = !room.IsReachable ? "No connected corridor to the lobby access core."
                    : disrupted ? "Investigator disruption is temporarily stopping this room."
                    : staff < room.RequiredStaff ? $"Needs {room.RequiredStaff} staff; {staff} assigned."
                    : NeedsSupplies(room.Type) && state.Supplies == 0 ? "Waiting for delivered supplies."
                    : room.Type == UndergroundRoomType.OperationsCenter && state.Intel == 0 ? "Waiting for intel from Communications."
                    : room.Type == UndergroundRoomType.ResearchLab && state.Intel < 2 ? "Needs two intel for research."
                    : "Operating";
                rooms.Add(new UndergroundRoomOperationProjection(room.Id, room.Type, room.X,
                    room.Depth, room.Width, room.Height, room.Capacity, staff, room.RequiredStaff,
                    room.DailyUpkeep, room.IsReachable, disrupted, cause,
                    new ReadOnlyCollection<int>(residents ?? new List<int>())));
            }
            Rooms = new ReadOnlyCollection<UndergroundRoomOperationProjection>(rooms);
        }

        private static bool NeedsSupplies(UndergroundRoomType type) =>
            type == UndergroundRoomType.Generator || type == UndergroundRoomType.Workshop ||
            type == UndergroundRoomType.OperationsCenter || type == UndergroundRoomType.ResearchLab;

        public int Supplies { get; }
        public int Intel { get; }
        public int ResearchPoints { get; }
        public float Exposure { get; }
        public int CoverPriority { get; }
        public int StaffingPriority { get; }
        public int SecurityPosture { get; }
        public InvestigatorPhase Phase { get; }
        public int InvestigatorX { get; }
        public int InvestigatorDepth { get; }
        public int InvestigatorTargetRoomId { get; }
        public int LastContractIncome { get; }
        public int LastDailyCost { get; }
        public int BackupPowerCapacity { get; }
        public float RepairBoost { get; }
        public float CareBoost { get; }
        public float CommonsMoraleBoost { get; }
        public int ShelterCapacity { get; }
        public IReadOnlyList<UndergroundRoomOperationProjection> Rooms { get; }
    }

    public readonly struct UndergroundRoomOperationProjection
    {
        public UndergroundRoomOperationProjection(int id, UndergroundRoomType type, int x, int depth,
            int width, int height, int capacity, int staff, int requiredStaff, int upkeep,
            bool reachable, bool disrupted, string cause, IReadOnlyList<int> assignedResidentIds)
        {
            Id = id; Type = type; X = x; Depth = depth; Width = width; Height = height;
            Capacity = capacity; Staff = staff; RequiredStaff = requiredStaff; DailyUpkeep = upkeep;
            IsReachable = reachable; IsDisrupted = disrupted; Cause = cause;
            AssignedResidentIds = assignedResidentIds;
        }
        public int Id { get; }
        public UndergroundRoomType Type { get; }
        public int X { get; }
        public int Depth { get; }
        public int Width { get; }
        public int Height { get; }
        public int Capacity { get; }
        public int Staff { get; }
        public int RequiredStaff { get; }
        public int DailyUpkeep { get; }
        public bool IsReachable { get; }
        public bool IsDisrupted { get; }
        public string Cause { get; }
        public IReadOnlyList<int> AssignedResidentIds { get; }
    }
}
