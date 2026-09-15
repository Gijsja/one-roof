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
    }
}
