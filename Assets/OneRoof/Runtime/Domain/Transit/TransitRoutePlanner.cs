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
            var unvisited = new HashSet<EntityId>();

            foreach (var node in _graph.Nodes)
            {
                distances[node.Id] = int.MaxValue;
                unvisited.Add(node.Id);
            }

            distances[originNodeId] = 0;

            while (unvisited.Count > 0)
            {
                EntityId current = default;
                var smallestDistance = int.MaxValue;

                foreach (var id in unvisited)
                {
                    var dist = distances[id];
                    if (dist < smallestDistance)
                    {
                        smallestDistance = dist;
                        current = id;
                    }
                }

                if (smallestDistance == int.MaxValue || current.Equals(destinationNodeId))
                {
                    break;
                }

                unvisited.Remove(current);

                foreach (var edge in _graph.GetOutgoingEdges(current))
                {
                    if (!unvisited.Contains(edge.ToNodeId))
                    {
                        continue;
                    }

                    var alt = smallestDistance + edge.Cost;
                    if (alt < distances[edge.ToNodeId])
                    {
                        distances[edge.ToNodeId] = alt;
                        previousEdge[edge.ToNodeId] = edge;
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
