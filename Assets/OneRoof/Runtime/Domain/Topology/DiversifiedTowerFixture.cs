using System;
using System.Collections.Generic;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Randomness;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Topology
{
    /// <summary>
    /// Deterministic factory creating a tower with a diverse mix of 1x1 (Micro Pod),
    /// 2x2 (Studio Apartment), and 3x3 (Grand Suite) rooms, matching maximum inhabitant counts.
    /// </summary>
    public static class DiversifiedTowerFixture
    {
        public const int DefaultFloorCount = 6;
        public const ulong DefaultSeed = 421004;

        public const string MicroPodContentType = "residential:micro";
        public const string ApartmentContentType = "residential:apartment";
        public const string SuiteContentType = "residential:suite";

        public static TowerSimulation Create(int floorCount = DefaultFloorCount, ulong seed = DefaultSeed)
        {
            if (floorCount < 2) floorCount = 2;

            var slabs = new List<CellBounds>();
            var rooms = new List<Room>();
            var portals = new List<Portal>();
            var homes = new List<Room>();
            var jobs = new List<EntityId>();
            var nextId = 1;

            Room Add(int floor, int left, int right, string type, int capacity, PortalType portalType = PortalType.Door)
            {
                var roomId = new EntityId(nextId++);
                var portalId = new EntityId(nextId++);
                var room = new Room(roomId, new ContentId(type), new CellBounds(floor, left, right), new[] { portalId }, capacity);
                rooms.Add(room);
                portals.Add(new Portal(portalId, portalType, new CellCoordinate(left < 0 ? right : left, floor), roomId));
                return room;
            }

            var rng = new DeterministicRandomStream(seed);

            // Patterns of (widthInCells, contentType, capacity) totaling 12 cells for west and east bays
            var westPatterns = new[]
            {
                new[] { (9, SuiteContentType, 3), (3, MicroPodContentType, 1) },                     // 3x3 + 1x1
                new[] { (6, ApartmentContentType, 2), (6, ApartmentContentType, 2) },                 // 2x2 + 2x2
                new[] { (3, MicroPodContentType, 1), (9, SuiteContentType, 3) },                     // 1x1 + 3x3
                new[] { (3, MicroPodContentType, 1), (6, ApartmentContentType, 2), (3, MicroPodContentType, 1) }, // 1x1 + 2x2 + 1x1
                new[] { (6, ApartmentContentType, 2), (3, MicroPodContentType, 1), (3, MicroPodContentType, 1) }, // 2x2 + 1x1 + 1x1
            };

            var eastPatterns = new[]
            {
                new[] { (3, MicroPodContentType, 1), (9, SuiteContentType, 3) },                     // 1x1 + 3x3
                new[] { (6, ApartmentContentType, 2), (6, ApartmentContentType, 2) },                 // 2x2 + 2x2
                new[] { (9, SuiteContentType, 3), (3, MicroPodContentType, 1) },                     // 3x3 + 1x1
                new[] { (3, MicroPodContentType, 1), (3, MicroPodContentType, 1), (6, ApartmentContentType, 2) }, // 1x1 + 1x1 + 2x2
                new[] { (6, ApartmentContentType, 2), (3, MicroPodContentType, 1), (3, MicroPodContentType, 1) }, // 2x2 + 1x1 + 1x1
            };

            for (var floor = 0; floor < floorCount; floor++)
            {
                slabs.Add(new CellBounds(floor, -20, 17));
                Add(floor, 0, 1, "transit:elevator_shaft", 30, PortalType.ElevatorShaftDoor);
                Add(floor, -14, -13, "amenity:stairwell", 30, PortalType.StairwellDoor);
                Add(floor, -20, -20, "utility:electrical_riser", 0);
                Add(floor, -19, -19, "utility:water_riser", 0);
                Add(floor, -18, -18, "utility:waste_chute", 0);
                Add(floor, -17, -17, "utility:floor_transformer", 0);

                if (floor == 0)
                {
                    Add(0, -16, -15, "utility:electrical_substation", 4000);
                    Add(0, 14, 15, "utility:water_pump", 4000);
                    Add(0, 16, 17, "utility:waste_collection", 4000);
                    jobs.Add(Add(0, -12, -7, "commercial:retail", 12).Id);
                    jobs.Add(Add(0, -6, -1, "commercial:diner", 20).Id);
                    Add(0, 2, 13, "amenity:lobby", 50);
                }
                else
                {
                    // West residential bay: cells -12 to -1 (12 cells)
                    var westPattern = westPatterns[(floor - 1) % westPatterns.Length];
                    var curWestX = -12;
                    for (var i = 0; i < westPattern.Length; i++)
                    {
                        var (w, type, cap) = westPattern[i];
                        var room = Add(floor, curWestX, curWestX + w - 1, type, cap);
                        homes.Add(room);
                        curWestX += w;
                    }

                    // East residential bay: cells 2 to 13 (12 cells)
                    var eastPattern = eastPatterns[(floor - 1) % eastPatterns.Length];
                    var curEastX = 2;
                    for (var i = 0; i < eastPattern.Length; i++)
                    {
                        var (w, type, cap) = eastPattern[i];
                        var room = Add(floor, curEastX, curEastX + w - 1, type, cap);
                        homes.Add(room);
                        curEastX += w;
                    }
                }
            }

            // Keep authored entity ranges disjoint even at the largest supported Tower size.
            var firstElevatorId = Math.Max(901, nextId);
            var householdStartId = Math.Max(1000, firstElevatorId + ElevatorBank.MaxCarsPerBank);
            var topology = new BuildingTopologyState(startingEntityId: 3000);
            topology.RestoreFromData(slabs, rooms, portals);

            var people = new List<PersonRecord>();
            var households = new List<HouseholdRecord>();
            var traits = (PersonTraitKind[])Enum.GetValues(typeof(PersonTraitKind));
            var nextPersonId = Math.Max(2000, householdStartId + homes.Count);

            for (var h = 0; h < homes.Count; h++)
            {
                var home = homes[h];
                var householdId = new EntityId(householdStartId + h);
                var memberCount = home.Capacity;
                var members = new EntityId[memberCount];

                for (var m = 0; m < memberCount; m++)
                {
                    members[m] = new EntityId(nextPersonId++);
                }

                households.Add(new HouseholdRecord(householdId, members, home.Id, 0.75f, 1.0f));

                for (var m = 0; m < memberCount; m++)
                {
                    var trait = new PersonTrait(traits[rng.NextInt(0, traits.Length)]);
                    var workplace = m == 0 && jobs.Count > 0 ? jobs[h % jobs.Count] : default;
                    var content = workplace.IsValid ? topology.Rooms[workplace].ContentType.Value : null;
                    var schedule = EmploymentRoutineFactory.Create(members[m], trait, content, (uint)seed);
                    people.Add(new PersonRecord(members[m], householdId, home.Id,
                        workplace, schedule, EmploymentRoutineFactory.InitialNeeds(schedule, new Tick(450)),
                        new[] { trait }, worksOutside: !workplace.IsValid));
                }
            }

            var simulation = new TowerSimulation(
                new SimulationClock(new Tick(450)),
                topology,
                new PopulationState(people, households),
                new ElevatorBank(0, floorCount - 1, new[]
                {
                    new ElevatorCar(new EntityId(firstElevatorId), 0, 20),
                    new ElevatorCar(new EntityId(firstElevatorId + 1), Math.Min(floorCount - 1, 3), 20)
                }),
                new TowerEconomyState(100000),
                rng);

            // Restore through the save contract to reserve IDs for subsequent commands.
            var save = simulation.ExportSaveData();
            save.nextElevatorCarId = firstElevatorId + 2;
            save.nextEntityId = Math.Max(3000, nextPersonId);
            return TowerSimulation.RestoreFromSaveData(save, rng);
        }
    }
}
