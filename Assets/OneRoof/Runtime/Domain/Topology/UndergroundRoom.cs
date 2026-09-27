namespace OneRoof.Domain.Topology
{
    public enum UndergroundRoomType
    {
        AccessHub, SupplyDepot, Generator, Workshop, OperationsCenter, ResearchLab,
        CoverOffice, SecurityPost, StaffCommons, Infirmary, Vault, TrainingRoom,
        Communications, EmergencyShelter
    }

    public readonly struct UndergroundRoomDefinition
    {
        public UndergroundRoomDefinition(int capacity, int requiredStaff, int dailyUpkeep, int baseCost)
        {
            Capacity = capacity;
            RequiredStaff = requiredStaff;
            DailyUpkeep = dailyUpkeep;
            BaseCost = baseCost;
        }

        public int Capacity { get; }
        public int RequiredStaff { get; }
        public int DailyUpkeep { get; }
        public int BaseCost { get; }
    }

    public static class UndergroundRoomCatalog
    {
        private static readonly UndergroundRoomDefinition[] Definitions =
        {
            new UndergroundRoomDefinition(10, 1, 8, 240),
            new UndergroundRoomDefinition(40, 1, 12, 300),
            new UndergroundRoomDefinition(80, 2, 28, 520),
            new UndergroundRoomDefinition(12, 2, 16, 360),
            new UndergroundRoomDefinition(16, 3, 30, 600),
            new UndergroundRoomDefinition(12, 3, 28, 580),
            new UndergroundRoomDefinition(20, 2, 18, 400),
            new UndergroundRoomDefinition(12, 2, 22, 440),
            new UndergroundRoomDefinition(24, 1, 10, 280),
            new UndergroundRoomDefinition(16, 2, 24, 480),
            new UndergroundRoomDefinition(100, 2, 20, 500),
            new UndergroundRoomDefinition(20, 2, 18, 380),
            new UndergroundRoomDefinition(14, 2, 22, 460),
            new UndergroundRoomDefinition(60, 1, 14, 340)
        };

        public static bool IsDefined(UndergroundRoomType type) => (int)type >= 0 && (int)type < Definitions.Length;
        public static UndergroundRoomDefinition Get(UndergroundRoomType type) => Definitions[(int)type];
    }

    public sealed class UndergroundRoom
    {
        public UndergroundRoom(int id, UndergroundRoomType type, int x, int depth, int width, int height, UndergroundRoomDefinition definition)
        {
            Id = id; Type = type; X = x; Depth = depth; Width = width; Height = height;
            Capacity = definition.Capacity; RequiredStaff = definition.RequiredStaff;
            DailyUpkeep = definition.DailyUpkeep;
            IsReachable = true;
        }

        public int Id { get; }
        public UndergroundRoomType Type { get; }
        public int X { get; }
        public int Depth { get; }
        public int Width { get; }
        public int Height { get; }
        public int Capacity { get; }
        public int RequiredStaff { get; }
        public int DailyUpkeep { get; }
        public bool IsReachable { get; internal set; }

        public UndergroundRoom Snapshot()
        {
            var copy = new UndergroundRoom(Id, Type, X, Depth, Width, Height, UndergroundRoomCatalog.Get(Type));
            copy.IsReachable = IsReachable;
            return copy;
        }
    }
}
