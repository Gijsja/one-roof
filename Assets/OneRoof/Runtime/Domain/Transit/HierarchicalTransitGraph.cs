using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Transit
{
    public sealed class HierarchicalTransitGraph
    {
        private const int VerticalElevatorFloorCost = 10;
        private const int VerticalStairFloorCost = 15;

        private readonly Dictionary<EntityId, TransitNode> _nodesById;
        private readonly Dictionary<EntityId, List<TransitEdge>> _outgoingEdges;
        private readonly Dictionary<CellCoordinate, TransitNode> _nodesByLocation;
        private readonly Dictionary<EntityId, List<TransitNode>> _portalNodesByRoomId;

        public HierarchicalTransitGraph(IEnumerable<TransitNode> nodes, IEnumerable<TransitEdge> edges)
        {
            _nodesById = new Dictionary<EntityId, TransitNode>();
            _outgoingEdges = new Dictionary<EntityId, List<TransitEdge>>();
            _nodesByLocation = new Dictionary<CellCoordinate, TransitNode>();
            _portalNodesByRoomId = new Dictionary<EntityId, List<TransitNode>>();

            var nodeList = new List<TransitNode>();
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    _nodesById[node.Id] = node;
                    _nodesByLocation[node.Location] = node;
                    _outgoingEdges[node.Id] = new List<TransitEdge>();
                    nodeList.Add(node);

                    if (node.RoomId.HasValue)
                    {
                        if (!_portalNodesByRoomId.TryGetValue(node.RoomId.Value, out var roomNodes))
                        {
                            roomNodes = new List<TransitNode>();
                            _portalNodesByRoomId[node.RoomId.Value] = roomNodes;
                        }

                        roomNodes.Add(node);
                    }
                }
            }

            var edgeList = new List<TransitEdge>();
            if (edges != null)
            {
                foreach (var edge in edges)
                {
                    if (edge == null) continue;
                    // P2: drop edges whose source or destination node is not registered — prevents
                    // KeyNotFoundException during routing on malformed content.
                    if (!_outgoingEdges.TryGetValue(edge.FromNodeId, out var outgoing)) continue;
                    if (!_nodesById.ContainsKey(edge.ToNodeId)) continue;
                    outgoing.Add(edge);
                    edgeList.Add(edge);
                }
            }

            Nodes = new ReadOnlyCollection<TransitNode>(nodeList);
            Edges = new ReadOnlyCollection<TransitEdge>(edgeList);
        }

        public IReadOnlyList<TransitNode> Nodes { get; }

        public IReadOnlyList<TransitEdge> Edges { get; }

        public bool TryGetNode(EntityId id, out TransitNode node) => _nodesById.TryGetValue(id, out node);

        public IReadOnlyList<TransitEdge> GetOutgoingEdges(EntityId nodeId)
        {
            if (_outgoingEdges.TryGetValue(nodeId, out var edges))
            {
                return edges;
            }

            return Array.Empty<TransitEdge>();
        }

        public TransitNode GetNodeAt(CellCoordinate coordinate)
        {
            _nodesByLocation.TryGetValue(coordinate, out var node);
            return node;
        }

        public TransitNode GetPortalNodeForRoom(EntityId roomId)
        {
            if (_portalNodesByRoomId.TryGetValue(roomId, out var nodes) && nodes.Count > 0)
            {
                return nodes[0];
            }

            return null;
        }

        public static HierarchicalTransitGraph FromBuildingTopology(BuildingTopology topology)
        {
            if (topology == null)
            {
                throw new ArgumentNullException(nameof(topology));
            }

            var nodes = new List<TransitNode>();
            var edges = new List<TransitEdge>();
            var elevatorStopsByColumn = new Dictionary<int, List<TransitNode>>();

            var nextNodeId = 1000;

            // 1. Build nodes for portals on each floor
            foreach (var floor in topology.Floors)
            {
                var floorNodes = new List<TransitNode>();

                foreach (var portal in floor.Portals)
                {
                    var nodeType = portal.Type switch
                    {
                        PortalType.ElevatorShaftDoor => TransitNodeType.ElevatorStop,
                        PortalType.StairwellDoor => TransitNodeType.StairLanding,
                        _ => TransitNodeType.RoomPortal
                    };

                    var node = new TransitNode(new EntityId(nextNodeId++), nodeType, portal.Location, portal.RoomId);
                    nodes.Add(node);
                    floorNodes.Add(node);

                    if (nodeType == TransitNodeType.ElevatorStop)
                    {
                        if (!elevatorStopsByColumn.TryGetValue(portal.Location.X, out var shaftStops))
                        {
                            shaftStops = new List<TransitNode>();
                            elevatorStopsByColumn[portal.Location.X] = shaftStops;
                        }

                        shaftStops.Add(node);
                    }
                }

                // 2. Connect floor-local nodes with horizontal walk edges
                for (var i = 0; i < floorNodes.Count; i++)
                {
                    for (var j = i + 1; j < floorNodes.Count; j++)
                    {
                        var a = floorNodes[i];
                        var b = floorNodes[j];
                        var walkDistance = Math.Abs(a.Location.X - b.Location.X);

                        edges.Add(new TransitEdge(a.Id, b.Id, walkDistance, TransitMode.Walk));
                        edges.Add(new TransitEdge(b.Id, a.Id, walkDistance, TransitMode.Walk));
                    }
                }
            }

            // 3. Connect vertical elevator shaft stops across floors
            foreach (var column in elevatorStopsByColumn.Values)
            {
                for (var i = 0; i < column.Count; i++)
                {
                    for (var j = i + 1; j < column.Count; j++)
                    {
                        var a = column[i];
                        var b = column[j];
                        var floorDelta = Math.Abs(a.Floor - b.Floor);
                        var transitCost = floorDelta * VerticalElevatorFloorCost;

                        edges.Add(new TransitEdge(a.Id, b.Id, transitCost, TransitMode.Elevator));
                        edges.Add(new TransitEdge(b.Id, a.Id, transitCost, TransitMode.Elevator));
                    }
                }
            }

            // 4. Connect vertical stair landing nodes across floors (Walk mode, higher cost than elevator)
            var stairLandingsByColumn = new Dictionary<int, List<TransitNode>>();
            foreach (var node in nodes)
            {
                if (node.Type == TransitNodeType.StairLanding)
                {
                    if (!stairLandingsByColumn.TryGetValue(node.Location.X, out var columnStairs))
                    {
                        columnStairs = new List<TransitNode>();
                        stairLandingsByColumn[node.Location.X] = columnStairs;
                    }

                    columnStairs.Add(node);
                }
            }

            foreach (var column in stairLandingsByColumn.Values)
            {
                column.Sort((a, b) => a.Floor.CompareTo(b.Floor));

                for (var i = 0; i < column.Count - 1; i++)
                {
                    var a = column[i];
                    var b = column[i + 1];
                    if (Math.Abs(a.Floor - b.Floor) == 1)
                    {
                        var stairCost = VerticalStairFloorCost;
                        edges.Add(new TransitEdge(a.Id, b.Id, stairCost, TransitMode.Walk));
                        edges.Add(new TransitEdge(b.Id, a.Id, stairCost, TransitMode.Walk));
                    }
                }
            }

            return new HierarchicalTransitGraph(nodes, edges);
        }
    }
}
