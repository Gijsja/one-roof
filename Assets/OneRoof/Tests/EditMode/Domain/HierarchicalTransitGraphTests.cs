using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;
using OneRoof.Domain.Trips;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class HierarchicalTransitGraphTests
    {
        [Test]
        public void TwoFloorFixtureBuildsWalkAndVerticalTransitEdges()
        {
            var graph = TwoFloorTransitFixture.CreateGraph();

            Assert.That(graph.Nodes.Count, Is.EqualTo(5));
            Assert.That(graph.OutsideNode, Is.Not.Null);
            Assert.That(graph.Edges.Count, Is.GreaterThan(0));

            var elevatorNodeFloor0 = graph.GetNodeAt(new CellCoordinate(0, 0));
            Assert.That(elevatorNodeFloor0, Is.Not.Null);
            Assert.That(elevatorNodeFloor0.Type, Is.EqualTo(TransitNodeType.ElevatorStop));

            var elevatorNodeFloor1 = graph.GetNodeAt(new CellCoordinate(0, 1));
            Assert.That(elevatorNodeFloor1, Is.Not.Null);
            Assert.That(elevatorNodeFloor1.Type, Is.EqualTo(TransitNodeType.ElevatorStop));
        }

        [Test]
        public void SameFloorRouteUsesWalkOnly()
        {
            var graph = TwoFloorTransitFixture.CreateGraph();
            var planner = new TransitRoutePlanner(graph);

            var route = planner.FindRoute(new CellCoordinate(2, 0), new CellCoordinate(0, 0));

            Assert.That(route, Is.Not.Null);
            Assert.That(route.RequiresVerticalTransit, Is.False);
            Assert.That(route.Legs.Count, Is.EqualTo(1));
            Assert.That(route.Legs[0].Mode, Is.EqualTo(TransitMode.Walk));
            Assert.That(route.TotalCost, Is.EqualTo(2));
        }

        [Test]
        public void MultiFloorRouteTraversesWalkAndElevatorLegs()
        {
            var graph = TwoFloorTransitFixture.CreateGraph();
            var planner = new TransitRoutePlanner(graph);

            var route = planner.FindRoute(new CellCoordinate(2, 0), new CellCoordinate(2, 1));

            Assert.That(route, Is.Not.Null);
            Assert.That(route.RequiresVerticalTransit, Is.True);
            Assert.That(route.Legs.Count, Is.EqualTo(3));
            Assert.That(route.Legs[0].Mode, Is.EqualTo(TransitMode.Walk));
            Assert.That(route.Legs[1].Mode, Is.EqualTo(TransitMode.Elevator));
            Assert.That(route.Legs[2].Mode, Is.EqualTo(TransitMode.Walk));
            Assert.That(route.TotalCost, Is.EqualTo(14));
        }

        [Test]
        public void FiveFloorBuildingTopologyProducesDeterministicCommuteRoute()
        {
            var topology = FiveFloorTopologyFixture.Create();
            var graph = HierarchicalTransitGraph.FromBuildingTopology(topology);
            var planner = new TransitRoutePlanner(graph);

            // Commute from Lobby door (2, 0) to 4th floor Far-East residential door (8, 4)
            var route = planner.FindRoute(new CellCoordinate(2, 0), new CellCoordinate(8, 4));

            Assert.That(route, Is.Not.Null);
            Assert.That(route.RequiresVerticalTransit, Is.True);
            Assert.That(route.Legs.Count, Is.EqualTo(3));

            // Walk to elevator: 2 cells
            Assert.That(route.Legs[0].Cost, Is.EqualTo(2));
            Assert.That(route.Legs[0].Mode, Is.EqualTo(TransitMode.Walk));

            // Vertical ride: 4 floors * 10 = 40
            Assert.That(route.Legs[1].Cost, Is.EqualTo(40));
            Assert.That(route.Legs[1].Mode, Is.EqualTo(TransitMode.Elevator));

            // Walk from elevator to apartment door: 8 cells
            Assert.That(route.Legs[2].Cost, Is.EqualTo(8));
            Assert.That(route.Legs[2].Mode, Is.EqualTo(TransitMode.Walk));

            Assert.That(route.TotalCost, Is.EqualTo(50));
        }

        [Test]
        public void RouteBetweenSameFloorRoomsDoesNotRequireElevator()
        {
            var topology = FiveFloorTopologyFixture.Create();
            var graph = HierarchicalTransitGraph.FromBuildingTopology(topology);
            var planner = new TransitRoutePlanner(graph);

            // Diner door at (-1, 0) to Lobby door at (2, 0)
            var route = planner.FindRoute(new CellCoordinate(-1, 0), new CellCoordinate(2, 0));

            Assert.That(route, Is.Not.Null);
            Assert.That(route.RequiresVerticalTransit, Is.False);
            Assert.That(route.TotalCost, Is.EqualTo(3));
        }

        [Test]
        public void NonExistentNodeReturnsNull()
        {
            var graph = TwoFloorTransitFixture.CreateGraph();
            var planner = new TransitRoutePlanner(graph);

            var route = planner.FindRoute(new CellCoordinate(99, 99), new CellCoordinate(0, 0));
            Assert.That(route, Is.Null);
        }

        [Test]
        public void StairwellPortalsReceiveVerticalWalkEdges()
        {
            // Build a minimal two-floor topology with StairwellDoor portals on each floor,
            // sharing the same X column so FromBuildingTopology groups them into a stair column.
            var stairRoom0 = new Room(new EntityId(10), new ContentId("transit:stair"), new CellBounds(0, 5, 5), new[] { new EntityId(11) }, 5);
            var stairPortal0 = new Portal(new EntityId(11), PortalType.StairwellDoor, new CellCoordinate(5, 0), new EntityId(10));
            var floor0 = new FloorTopology(0, new[] { stairRoom0 }, new[] { stairPortal0 });

            var stairRoom1 = new Room(new EntityId(12), new ContentId("transit:stair"), new CellBounds(1, 5, 5), new[] { new EntityId(13) }, 5);
            var stairPortal1 = new Portal(new EntityId(13), PortalType.StairwellDoor, new CellCoordinate(5, 1), new EntityId(12));
            var floor1 = new FloorTopology(1, new[] { stairRoom1 }, new[] { stairPortal1 });

            var topology = new BuildingTopology(new[] { floor0, floor1 });
            var graph = HierarchicalTransitGraph.FromBuildingTopology(topology);

            // Must have exactly 2 nodes (one stair landing per floor)
            Assert.That(graph.Nodes.Count, Is.EqualTo(2));

            // There must be at least one Walk edge with vertical cost between the two stair-landing nodes
            var stairNode0 = graph.GetNodeAt(new CellCoordinate(5, 0));
            var stairNode1 = graph.GetNodeAt(new CellCoordinate(5, 1));
            Assert.That(stairNode0, Is.Not.Null);
            Assert.That(stairNode1, Is.Not.Null);
            Assert.That(stairNode0.Type, Is.EqualTo(TransitNodeType.StairLanding));

            var edges = graph.GetOutgoingEdges(stairNode0.Id);
            Assert.That(edges, Has.Count.GreaterThan(0), "Stair landing must have at least one outgoing edge");
            Assert.That(edges[0].Mode, Is.EqualTo(TransitMode.Walk), "Stair vertical edge must be Walk mode");
            Assert.That(edges[0].Cost, Is.GreaterThan(0), "Stair vertical edge must have positive cost");
        }

        [Test]
        public void DisconnectedStairwellPortals_DoNotReceiveDirectVerticalWalkEdges()
        {
            // Floor 0 and Floor 2 have stair landings, but Floor 1 is missing
            var stairRoom0 = new Room(new EntityId(20), new ContentId("transit:stair"), new CellBounds(0, 5, 5), new[] { new EntityId(21) }, 5);
            var stairPortal0 = new Portal(new EntityId(21), PortalType.StairwellDoor, new CellCoordinate(5, 0), new EntityId(20));
            var floor0 = new FloorTopology(0, new[] { stairRoom0 }, new[] { stairPortal0 });

            var stairRoom2 = new Room(new EntityId(22), new ContentId("transit:stair"), new CellBounds(2, 5, 5), new[] { new EntityId(23) }, 5);
            var stairPortal2 = new Portal(new EntityId(23), PortalType.StairwellDoor, new CellCoordinate(5, 2), new EntityId(22));
            var floor2 = new FloorTopology(2, new[] { stairRoom2 }, new[] { stairPortal2 });

            var topology = new BuildingTopology(new[] { floor0, floor2 });
            var graph = HierarchicalTransitGraph.FromBuildingTopology(topology);

            var stairNode0 = graph.GetNodeAt(new CellCoordinate(5, 0));
            var stairNode2 = graph.GetNodeAt(new CellCoordinate(5, 2));
            Assert.That(stairNode0, Is.Not.Null);
            Assert.That(stairNode2, Is.Not.Null);

            var edges0 = graph.GetOutgoingEdges(stairNode0.Id);
            Assert.That(edges0, Has.Count.EqualTo(0), "Non-adjacent stair landings must not have direct vertical edges");
        }

        [Test]
        public void PortalNodeIdsRemainStableWhenUnrelatedPortalsAreAdded()
        {
            var first = CreateElevatorAndStairTopology(includeUnrelatedPortal: false);
            var expanded = CreateElevatorAndStairTopology(includeUnrelatedPortal: true);
            var firstGraph = HierarchicalTransitGraph.FromBuildingTopology(first);
            var expandedGraph = HierarchicalTransitGraph.FromBuildingTopology(expanded);
            var firstRoute = new TransitRoutePlanner(firstGraph).FindRoute(new EntityId(101), new EntityId(203));
            var expandedRoute = new TransitRoutePlanner(expandedGraph).FindRoute(new EntityId(101), new EntityId(203));

            foreach (var portalId in new[] { 101, 102, 103, 201, 202, 203 })
            {
                Assert.That(firstGraph.TryGetNode(new EntityId(portalId), out var firstNode), Is.True);
                Assert.That(expandedGraph.TryGetNode(new EntityId(portalId), out var expandedNode), Is.True);
                Assert.That(expandedNode.Id, Is.EqualTo(firstNode.Id));
            }

            Assert.That(expandedRoute.Legs.Count, Is.EqualTo(firstRoute.Legs.Count));
            for (var i = 0; i < firstRoute.Legs.Count; i++)
            {
                Assert.That(expandedRoute.Legs[i].FromNodeId, Is.EqualTo(firstRoute.Legs[i].FromNodeId));
                Assert.That(expandedRoute.Legs[i].ToNodeId, Is.EqualTo(firstRoute.Legs[i].ToNodeId));
            }

            var firstLobbyGraph = HierarchicalTransitGraph.FromBuildingTopology(CreateLobbyTopology(includeUnrelatedPortal: false));
            var expandedLobbyGraph = HierarchicalTransitGraph.FromBuildingTopology(CreateLobbyTopology(includeUnrelatedPortal: true));
            var firstOutside = firstLobbyGraph.OutsideNode;
            var expandedOutside = expandedLobbyGraph.OutsideNode;

            Assert.That(firstOutside, Is.Not.Null);
            Assert.That(expandedOutside, Is.Not.Null);
            Assert.That(firstOutside.Id, Is.EqualTo(new EntityId(100)), "Outside node should use the lobby room's stable ID");
            Assert.That(expandedOutside.Id, Is.EqualTo(firstOutside.Id));

            var firstOutsideRoute = new TransitRoutePlanner(firstLobbyGraph).FindRoute(firstOutside.Id, new EntityId(101));
            var expandedOutsideRoute = new TransitRoutePlanner(expandedLobbyGraph).FindRoute(expandedOutside.Id, new EntityId(101));
            Assert.That(firstOutsideRoute, Is.Not.Null);
            Assert.That(expandedOutsideRoute, Is.Not.Null);
            Assert.That(expandedOutsideRoute.Legs.Count, Is.EqualTo(firstOutsideRoute.Legs.Count));
            for (var i = 0; i < firstOutsideRoute.Legs.Count; i++)
            {
                Assert.That(expandedOutsideRoute.Legs[i].FromNodeId, Is.EqualTo(firstOutsideRoute.Legs[i].FromNodeId));
                Assert.That(expandedOutsideRoute.Legs[i].ToNodeId, Is.EqualTo(firstOutsideRoute.Legs[i].ToNodeId));
            }
        }

        [Test]
        public void ZeroCarElevatorLegReroutesOverStairsWithoutEnqueueing()
        {
            var building = CreateElevatorAndStairTopology(includeUnrelatedPortal: false);
            var topology = BuildingTopologyState.FromBuildingTopology(building);
            var planner = new TransitRoutePlanner(topology.TransitGraph);
            var route = planner.FindRoute(new EntityId(101), new EntityId(203));
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Legs, Has.Some.Matches<TransitEdge>(edge => edge.Mode == TransitMode.Elevator));

            var trip = new TripRecord(
                new EntityId(501), new EntityId(502),
                WorldLocation.InRoom(new EntityId(100)),
                WorldLocation.InRoom(new EntityId(200)),
                TripPurpose.Work, new Tick(0), route);
            var transit = new TransitExecutionSystem();
            transit.SubmitTrip(trip, topology, new Tick(0), null);
            var bankWithoutCars = new ElevatorBank(0, 1, null);

            transit.Advance(new Tick(1), topology, bankWithoutCars, null);

            Assert.That(bankWithoutCars.TotalQueuedCount, Is.Zero);
            Assert.That(transit.ActiveTripCount, Is.EqualTo(1));
            Assert.That(transit.ActiveTrips[0].IsQueuedInElevator, Is.False);
            Assert.That(transit.ActiveTrips[0].Route.Legs,
                Has.None.Matches<TransitEdge>(edge => edge.Mode == TransitMode.Elevator));
            Assert.That(transit.ActiveTrips[0].Route.Legs,
                Has.Some.Matches<TransitEdge>(edge => edge.Cost == 15));
        }

        private static BuildingTopology CreateElevatorAndStairTopology(bool includeUnrelatedPortal)
        {
            var room0 = new Room(new EntityId(100), new ContentId("residential:room"), new CellBounds(0, 0, 8),
                new[] { new EntityId(101), new EntityId(102), new EntityId(103) }, 8);
            var floor0Portals = new System.Collections.Generic.List<Portal>
            {
                new Portal(new EntityId(101), PortalType.ElevatorShaftDoor, new CellCoordinate(0, 0), room0.Id),
                new Portal(new EntityId(102), PortalType.StairwellDoor, new CellCoordinate(3, 0), room0.Id),
                new Portal(new EntityId(103), PortalType.Door, new CellCoordinate(7, 0), room0.Id)
            };
            if (includeUnrelatedPortal)
            {
                var unrelated = new Room(new EntityId(300), new ContentId("service:room"), new CellBounds(0, 10, 12),
                    new[] { new EntityId(301) }, 2);
                floor0Portals.Add(new Portal(new EntityId(301), PortalType.Door, new CellCoordinate(10, 0), unrelated.Id));
                return new BuildingTopology(new[]
                {
                    new FloorTopology(0, new[] { room0, unrelated }, floor0Portals),
                    CreateSecondFloor()
                });
            }

            return new BuildingTopology(new[]
            {
                new FloorTopology(0, new[] { room0 }, floor0Portals),
                CreateSecondFloor()
            });
        }

        private static FloorTopology CreateSecondFloor()
        {
            var room = new Room(new EntityId(200), new ContentId("residential:room"), new CellBounds(1, 0, 8),
                new[] { new EntityId(201), new EntityId(202), new EntityId(203) }, 8);
            return new FloorTopology(1, new[] { room }, new[]
            {
                new Portal(new EntityId(201), PortalType.ElevatorShaftDoor, new CellCoordinate(0, 1), room.Id),
                new Portal(new EntityId(202), PortalType.StairwellDoor, new CellCoordinate(3, 1), room.Id),
                new Portal(new EntityId(203), PortalType.Door, new CellCoordinate(7, 1), room.Id)
            });
        }

        private static BuildingTopology CreateLobbyTopology(bool includeUnrelatedPortal)
        {
            var lobby = new Room(new EntityId(100), new ContentId("amenity:lobby"), new CellBounds(0, 0, 12),
                new[] { new EntityId(101) }, 20);
            var lobbyPortals = new System.Collections.Generic.List<Portal>
            {
                new Portal(new EntityId(101), PortalType.Door, new CellCoordinate(2, 0), lobby.Id)
            };
            var rooms = new System.Collections.Generic.List<Room> { lobby };
            if (includeUnrelatedPortal)
            {
                var unrelated = new Room(new EntityId(200), new ContentId("service:room"), new CellBounds(0, 20, 22),
                    new[] { new EntityId(999999) }, 2);
                rooms.Add(unrelated);
                lobbyPortals.Add(new Portal(new EntityId(999999), PortalType.Door, new CellCoordinate(20, 0), unrelated.Id));
            }

            return new BuildingTopology(new[] { new FloorTopology(0, rooms, lobbyPortals) });
        }
    }
}
