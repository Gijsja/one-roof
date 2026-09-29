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
    /// <summary>Authored, deterministic city-scale playground. City Status is still earned by simulation.</summary>
    public static class GoldStandardCityFixture
    {
        public const int FloorCount = 30;
        public const int ResidentCount = 300;
        public const ulong Seed = 301003;

        public static TowerSimulation Create()
        {
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
            for (var floor = 0; floor < FloorCount; floor++)
            {
                slabs.Add(new CellBounds(floor, -20, 17));
                Add(floor, 0, 1, "transit:elevator_shaft", 30, PortalType.ElevatorShaftDoor);
                Add(floor, -14, -13, "amenity:stairwell", 30, PortalType.StairwellDoor);
                Add(floor, -20, -20, "utility:electrical_riser", 0);
                Add(floor, -19, -19, "utility:water_riser", 0);
                Add(floor, -18, -18, "utility:waste_chute", 0);
                Add(floor, -17, -17, "utility:floor_transformer", 0);
                if (floor % 4 == 0 && floor > 0) Add(floor, -16, -15, "utility:water_booster", 0);
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
                    var slots = new[] { -12, -6, 2, 8 };
                    for (var slot = 0; slot < slots.Length; slot++)
                    {
                        var type = floor <= 25 ? "residential:studio" :
                            new[] { "commercial:office", "commercial:diner", "service:clinic", "service:maintenance_workshop", "service:security_station", "commercial:retail" }[(floor - 26 + slot) % 6];
                        var room = Add(floor, slots[slot], slots[slot] + 5, type, floor <= 25 ? 3 : 12);
                        if (floor <= 25) homes.Add(room); else jobs.Add(room.Id);
                    }
                }
            }
            var topology = new BuildingTopologyState(startingEntityId: 3000);
            topology.RestoreFromData(slabs, rooms, portals);
            var rng = new DeterministicRandomStream(Seed);
            var people = new List<PersonRecord>();
            var households = new List<HouseholdRecord>();
            var traits = (PersonTraitKind[])Enum.GetValues(typeof(PersonTraitKind));
            for (var h = 0; h < homes.Count; h++)
            {
                var householdId = new EntityId(1000 + h);
                var members = new[] { new EntityId(2000 + h * 3), new EntityId(2001 + h * 3), new EntityId(2002 + h * 3) };
                households.Add(new HouseholdRecord(householdId, members, homes[h].Id, .7f, .8f));
                for (var member = 0; member < 3; member++)
                {
                    var trait = new PersonTrait(traits[rng.NextInt(0, traits.Length)]);
                    var needs = new[] { new NeedState(NeedKind.Hunger, 1f), new NeedState(NeedKind.Energy, 1f),
                        new NeedState(NeedKind.Social, 1f), new NeedState(NeedKind.Hygiene, 1f), new NeedState(NeedKind.Purpose, 1f) };
                    // One internal worker per household, two autonomous Outside commuters.
                    people.Add(new PersonRecord(members[member], householdId, homes[h].Id,
                        member == 0 ? jobs[h % jobs.Count] : default,
                        DailySchedule.Standard(trait, rng), needs, new[] { trait }, worksOutside: member != 0));
                }
            }
            var simulation = new TowerSimulation(new SimulationClock(new Tick(450)), topology,
                new PopulationState(people, households),
                new ElevatorBank(0, 29, new[] { new ElevatorCar(new EntityId(901), 0, 20),
                    new ElevatorCar(new EntityId(902), 14, 20), new ElevatorCar(new EntityId(903), 29, 20) }),
                new TowerEconomyState(250000), rng);
            // Reserve the fixture's authored IDs through the existing save contract.
            var seed = simulation.ExportSaveData();
            seed.nextElevatorCarId = 904;
            seed.nextEntityId = 3000;
            return TowerSimulation.RestoreFromSaveData(seed, rng);
        }
    }
}
