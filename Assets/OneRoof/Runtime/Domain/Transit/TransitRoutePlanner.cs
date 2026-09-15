using System;
using System.Collections.Generic;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Transit
{
    public sealed class TransitRoutePlanner
    {
        private readonly HierarchicalTransitGraph _graph;

        public TransitRoutePlanner(HierarchicalTransitGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public TransitRoute FindRoute(EntityId originNodeId, EntityId destinationNodeId)
        {
            if (!_graph.TryGetNode(originNodeId, out var originNode) ||
                !_graph.TryGetNode(destinationNodeId, out var destNode))
            {
                return null;
            }

            if (originNodeId.Equals(destinationNodeId))
            {
                return new TransitRoute(originNodeId, destinationNodeId, originNode.Location, destNode.Location, Array.Empty<TransitEdge>());
            }

            var distances = new Dictionary<EntityId, int>();
            var previousEdge = new Dictionary<EntityId, TransitEdge>();

            // SortedSet with a composite (distance, nodeId) key gives deterministic tie-breaking:
            // equal-cost candidates are always visited in ascending node-ID order, producing
            // identical routes across runtimes regardless of HashSet enumeration order.
            var unvisited = new SortedSet<(int Distance, int NodeIdValue, EntityId NodeId)>(
                Comparer<(int Distance, int NodeIdValue, EntityId NodeId)>.Create(
                    (x, y) =>
                    {
                        var d = x.Distance.CompareTo(y.Distance);
                        return d != 0 ? d : x.NodeIdValue.CompareTo(y.NodeIdValue);
                    }));

            foreach (var node in _graph.Nodes)
            {
                distances[node.Id] = int.MaxValue;
            }

            distances[originNodeId] = 0;
            unvisited.Add((0, originNodeId.Value, originNodeId));

            while (unvisited.Count > 0)
            {
                var (smallestDistance, _, current) = unvisited.Min;
                unvisited.Remove(unvisited.Min);

                if (smallestDistance == int.MaxValue || current.Equals(destinationNodeId))
                {
                    break;
                }

                foreach (var edge in _graph.GetOutgoingEdges(current))
                {
                    if (!distances.ContainsKey(edge.ToNodeId))
                    {
                        continue;
                    }

                    var alt = smallestDistance + edge.Cost;
                    if (alt < distances[edge.ToNodeId])
                    {
                        // Remove old entry before updating (SortedSet requires remove+re-add to re-sort)
                        unvisited.Remove((distances[edge.ToNodeId], edge.ToNodeId.Value, edge.ToNodeId));
                        distances[edge.ToNodeId] = alt;
                        previousEdge[edge.ToNodeId] = edge;
                        unvisited.Add((alt, edge.ToNodeId.Value, edge.ToNodeId));
                    }
                }
            }

            if (!previousEdge.ContainsKey(destinationNodeId))
            {
                return null;
            }

            var path = new List<TransitEdge>();
            var curr = destinationNodeId;
            while (!curr.Equals(originNodeId))
            {
                var edge = previousEdge[curr];
                path.Add(edge);
                curr = edge.FromNodeId;
            }

            path.Reverse();
            return new TransitRoute(originNodeId, destinationNodeId, originNode.Location, destNode.Location, path);
        }

        public TransitRoute FindRoute(CellCoordinate origin, CellCoordinate destination)
        {
            var originNode = _graph.GetNodeAt(origin);
            var destNode = _graph.GetNodeAt(destination);

            if (originNode == null || destNode == null)
            {
                return null;
            }

            return FindRoute(originNode.Id, destNode.Id);
        }
    }
}
