using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    /// <summary>
    /// From-scratch acceptance coverage for the ground-floor start scene:
    /// a bare slab plus lobby shell must support the full loop of economy,
    /// utilities, commute, routines, and vertical expansion without any fixture population.
    /// </summary>
    [TestFixture]
    public sealed class GroundFloorStartTests
    {
        [Test]
        public void CreateGroundFloorStart_BeginsWithOneSlabLobbyShaftAndNoResidents()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();

            Assert.That(sim.Topology.FloorCount, Is.EqualTo(1));
            Assert.That(sim.Topology.HasFloor(0), Is.True);
            Assert.That(sim.ResidentCount, Is.EqualTo(0));
            Assert.That(sim.Economy.CashBalance, Is.EqualTo(TowerEconomyState.DefaultStartingTreasury));
            Assert.That(sim.ElevatorBank.Cars.Count, Is.EqualTo(1));
            Assert.That(sim.CurrentTick, Is.EqualTo(0));

            var groundRooms = sim.Topology.GetRoomsOnFloor(0);
            Assert.That(ContainsContent(groundRooms, "amenity:lobby"), Is.True, "Ground start must include a lobby shell.");
            Assert.That(ContainsContent(groundRooms, "transit:elevator_shaft"), Is.True, "Ground start must include an elevator shaft.");
        }

        [Test]
        public void CreateGroundFloorStart_TicksSafelyWithZeroResidents()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();

            for (var i = 0; i < 60; i++) sim.AdvanceOneTick();

            Assert.That(sim.CurrentTick, Is.EqualTo(60));
            Assert.That(sim.ResidentCount, Is.EqualTo(0));
            Assert.That(sim.Economy.TotalRevenue, Is.EqualTo(0));
        }

        [Test]
        public void FromScratchLoop_EconomyUtilitiesCommuteRoutineAndExpansion()
        {
            var sim = TowerSimulation.CreateGroundFloorStart(settlementPeriod: 50);
            var startingCash = sim.Economy.CashBalance;

            // ── 1. Widen the ground slab, then zone a diner workplace plus a home ──
            AssertAccepted(sim.ExpandGroundSlab(new ExpandGroundSlabCommand(-20, 23)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, -14, -5, new ContentId("commercial:diner"), 5)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, -20, -15, new ContentId("residential:apartment"), 5)));
            Assert.That(sim.Economy.CashBalance, Is.LessThan(startingCash), "Construction must deduct treasury cash.");

            // ── 2. Power + water backbone on the free ground cells ──
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, -4, -1, new ContentId("utility:electrical_substation"), 120)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, 15, 16, new ContentId("utility:electrical_riser"), 0)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, 18, 21, new ContentId("utility:water_pump"), 120)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, 22, 23, new ContentId("utility:water_riser"), 0)));

            // ── 3. Expand upward: second slab, apartment, risers, transformer, shaft ──
            AssertAccepted(sim.BuildFloorSlab(new BuildFloorSlabCommand(1, -20, 23)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, -20, -15, new ContentId("residential:apartment"), 5)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, 15, 16, new ContentId("utility:electrical_riser"), 0)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, 18, 19, new ContentId("utility:floor_transformer"), 0)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, 22, 23, new ContentId("utility:water_riser"), 0)));
            AssertAccepted(sim.AddElevatorShaft(new AddElevatorShaftCommand(0, 1, 0, 1)));

            Assert.That(sim.Topology.FloorCount, Is.EqualTo(2));
            Assert.That(sim.ElevatorBank.MaxFloor, Is.EqualTo(1));

            // ── 4. Utilities: upper floor reads powered and pressurised ──
            var power = sim.ElectricalGridSnapshot();
            var poweredFloor = FindFloor(power.Floors, 1);
            Assert.That(poweredFloor.IsConnected, Is.True, "Floor 1 must connect via substation + continuous riser + transformer.");
            Assert.That(poweredFloor.Voltage, Is.GreaterThanOrEqualTo(0.8f));

            var water = sim.WaterWasteNetworkSnapshot();
            var wateredFloor = FindWaterFloor(water.Floors, 1);
            Assert.That(wateredFloor.WaterPressure, Is.GreaterThanOrEqualTo(0.5f), "Floor 1 must hold service pressure via pump + continuous riser.");

            // ── 5. Routines + economy: demand leases homes, rent flows, commutes run ──
            // Leased residents use default schedules whose first Sleep→Work transition
            // lands near tick 480, so run past it to observe real commute trips.
            for (var i = 0; i < 700; i++) sim.AdvanceOneTick();

            Assert.That(sim.ResidentCount, Is.GreaterThanOrEqualTo(2), "Both apartments must lease through demand-driven move-ins.");
            Assert.That(sim.Economy.TotalRevenue, Is.GreaterThan(0), "Rent cycles must collect from residents and commercial rooms.");

            var transitTraffic = sim.TotalQueuedElevatorPassengers
                + sim.ElevatorBank.Cars[0].Passengers.Count
                + sim.ElevatorBank.DeliveredCount;
            Assert.That(transitTraffic + sim.ActiveTripCount, Is.GreaterThan(0), "Resident routines must produce trips and elevator traffic.");
        }

        private static void AssertAccepted(CommandResult result)
        {
            Assert.That(result.Accepted, Is.True,
                result.Rejections.Count > 0 ? result.Rejections[0].Message : "Command rejected without reason.");
        }

        [Test]
        public void ReservedShaftColumn_BlocksRoomsAndStairsButAdmitsShaftExtension()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();
            AssertAccepted(sim.ExpandGroundSlab(new ExpandGroundSlabCommand(-20, 23)));

            // Direct overlap with the built shaft room stays rejected.
            var directOverlap = sim.BuildRoom(new BuildRoomCommand(0, 0, 5, new ContentId("residential:apartment"), 5));
            Assert.That(directOverlap.Accepted, Is.False);
            Assert.That(directOverlap.Rejections[0].Code, Is.EqualTo(new ContentId("transit:shaft_overlap")));

            // Control: rooms clear of the column build normally.
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, -14, -5, new ContentId("commercial:diner"), 5)));

            // Expansion floor has no shaft room yet, but the established column
            // is still reserved: full and partial overlaps are rejected by name.
            AssertAccepted(sim.BuildFloorSlab(new BuildFloorSlabCommand(1, -20, 23)));
            var reservedFull = sim.BuildRoom(new BuildRoomCommand(1, 0, 5, new ContentId("residential:apartment"), 5));
            Assert.That(reservedFull.Accepted, Is.False);
            Assert.That(reservedFull.Rejections[0].Code, Is.EqualTo(new ContentId("transit:shaft_overlap")));
            Assert.That(reservedFull.Rejections[0].Message, Does.Contain("elevator shaft column"));

            var reservedPartial = sim.BuildRoom(new BuildRoomCommand(1, -2, 1, new ContentId("residential:apartment"), 5));
            Assert.That(reservedPartial.Accepted, Is.False);
            Assert.That(reservedPartial.Rejections[0].Code, Is.EqualTo(new ContentId("transit:shaft_overlap")));

            // Stairs cannot squat the column above the current shaft top either.
            AssertAccepted(sim.BuildFloorSlab(new BuildFloorSlabCommand(2, -20, 23)));
            var stairAboveShaft = sim.BuildStairwell(new BuildStairwellCommand(0, 1, 1, 2));
            Assert.That(stairAboveShaft.Accepted, Is.False);
            Assert.That(stairAboveShaft.Rejections[0].Code, Is.EqualTo(new ContentId("transit:shaft_overlap")));

            // Stairs clear of the column build normally.
            AssertAccepted(sim.BuildStairwell(new BuildStairwellCommand(15, 16, 0, 1)));

            // The kept-clear column admits the shaft extension with aligned rooms.
            AssertAccepted(sim.AddElevatorShaft(new AddElevatorShaftCommand(0, 1, 0, 1)));
            for (var floor = 0; floor <= 1; floor++)
            {
                var shaft = FindRoomWithContent(sim.Topology.GetRoomsOnFloor(floor), "transit:elevator_shaft");
                Assert.That(shaft.Bounds.MinX, Is.EqualTo(0));
                Assert.That(shaft.Bounds.MaxX, Is.EqualTo(1));
            }
        }

        [Test]
        public void ExpansionFloor_AlignsSlabBoundsWithFloorBelow()
        {
            var sim = TowerSimulation.CreateGroundFloorStart();
            AssertAccepted(sim.ExpandGroundSlab(new ExpandGroundSlabCommand(-20, 23)));

            var groundSlab = sim.Topology.FloorSlabs[0];
            AssertAccepted(sim.BuildFloorSlab(new BuildFloorSlabCommand(1, groundSlab.MinX, groundSlab.MaxX)));

            var upperSlab = sim.Topology.FloorSlabs[1];
            Assert.That(upperSlab.MinX, Is.EqualTo(groundSlab.MinX));
            Assert.That(upperSlab.MaxX, Is.EqualTo(groundSlab.MaxX));

            // Wall-to-wall zoning stays possible around the reserved column.
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, -20, -2, new ContentId("residential:apartment"), 5)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, 2, 14, new ContentId("commercial:office"), 8)));
            AssertAccepted(sim.AddElevatorShaft(new AddElevatorShaftCommand(0, 1, 0, 1)));
        }

        [Test]
        public void StairsOnlyAccess_CommutersReachWorkWithoutElevator()
        {
            var sim = TowerSimulation.CreateGroundFloorStart(settlementPeriod: 50);
            AssertAccepted(sim.ExpandGroundSlab(new ExpandGroundSlabCommand(-20, 23)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, -14, -5, new ContentId("commercial:diner"), 5)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(0, -20, -15, new ContentId("residential:apartment"), 5)));
            AssertAccepted(sim.BuildFloorSlab(new BuildFloorSlabCommand(1, -20, 23)));
            AssertAccepted(sim.BuildRoom(new BuildRoomCommand(1, -20, -15, new ContentId("residential:apartment"), 5)));
            AssertAccepted(sim.BuildStairwell(new BuildStairwellCommand(15, 16, 0, 1)));

            // No shaft extension: the bank still serves floor 0 only.
            Assert.That(sim.ElevatorBank.MaxFloor, Is.EqualTo(0));

            // Stair rooms share one column on both floors (aligned vertical transit).
            for (var floor = 0; floor <= 1; floor++)
            {
                var stair = FindRoomWithContent(sim.Topology.GetRoomsOnFloor(floor), "amenity:stairwell");
                Assert.That(stair.Bounds.MinX, Is.EqualTo(15));
                Assert.That(stair.Bounds.MaxX, Is.EqualTo(16));
            }

            // Leased residents sleep until ~tick 480; poll across the first work
            // transition and inspect every executed route for vertical modes.
            // (The transit graph models a stair climb as Walk legs between
            // StairLanding nodes on different floors; Elevator legs need a car.)
            var sawStairClimb = false;
            var sawElevatorLeg = false;
            var graph = sim.Topology.TransitGraph;
            for (var i = 0; i < 700; i++)
            {
                sim.AdvanceOneTick();
                foreach (var execution in sim.Transit.ActiveTrips)
                {
                    var route = execution.Trip.PlannedRoute;
                    if (route == null) continue;
                    foreach (var leg in route.Legs)
                    {
                        if (leg.Mode == TransitMode.Elevator)
                        {
                            sawElevatorLeg = true;
                        }
                        else if (leg.Mode == TransitMode.Walk &&
                            graph.TryGetNode(leg.FromNodeId, out var from) &&
                            graph.TryGetNode(leg.ToNodeId, out var to) &&
                            from.Type == TransitNodeType.StairLanding &&
                            to.Type == TransitNodeType.StairLanding &&
                            from.Floor != to.Floor)
                        {
                            sawStairClimb = true;
                        }
                    }
                }
            }

            Assert.That(sim.ResidentCount, Is.EqualTo(2), "Both apartments lease without any elevator.");
            Assert.That(sawStairClimb, Is.True, "At least one commute must climb vertically through the stairwell.");
            Assert.That(sawElevatorLeg, Is.False, "No commute may touch the elevator in a stairs-only tower.");
            Assert.That(sim.ElevatorBank.DeliveredCount, Is.EqualTo(0));
            Assert.That(sim.ElevatorBank.TotalQueuedCount, Is.EqualTo(0));
            Assert.That(sim.Economy.TotalRevenue, Is.GreaterThan(0), "Rent flows even before any elevator exists.");
        }

        private static bool ContainsContent(System.Collections.Generic.IReadOnlyList<Room> rooms, string content)
        {
            for (var i = 0; i < rooms.Count; i++)
                if (rooms[i].ContentType.Value == content) return true;
            return false;
        }

        private static Room FindRoomWithContent(System.Collections.Generic.IReadOnlyList<Room> rooms, string content)
        {
            for (var i = 0; i < rooms.Count; i++)
                if (rooms[i].ContentType.Value == content) return rooms[i];
            throw new AssertionException($"No room with content '{content}' found.");
        }

        private static OneRoof.Domain.Infrastructure.ElectricalFloorProjection FindFloor(
            System.Collections.Generic.IReadOnlyList<OneRoof.Domain.Infrastructure.ElectricalFloorProjection> floors, int floor)
        {
            for (var i = 0; i < floors.Count; i++)
                if (floors[i].Floor == floor) return floors[i];
            throw new AssertionException($"No electrical projection for floor {floor}.");
        }

        private static OneRoof.Domain.Infrastructure.WaterWasteFloorProjection FindWaterFloor(
            System.Collections.Generic.IReadOnlyList<OneRoof.Domain.Infrastructure.WaterWasteFloorProjection> floors, int floor)
        {
            for (var i = 0; i < floors.Count; i++)
                if (floors[i].Floor == floor) return floors[i];
            throw new AssertionException($"No water projection for floor {floor}.");
        }
    }
}
