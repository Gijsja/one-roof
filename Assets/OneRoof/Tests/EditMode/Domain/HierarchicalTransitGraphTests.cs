using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class HierarchicalTransitGraphTests
    {
        [Test]
        public void TwoFloorFixtureBuildsWalkAndVerticalTransitEdges()
        {
            var graph = TwoFloorTransitFixture.CreateGraph();

            Assert.That(graph.Nodes.Count, Is.EqualTo(4));
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
    }
}
