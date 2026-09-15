using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Transit
{
    public sealed class TransitRoute
    {
        public TransitRoute(EntityId originNodeId, EntityId destinationNodeId, CellCoordinate origin, CellCoordinate destination, IReadOnlyList<TransitEdge> legs)
        {
            OriginNodeId = originNodeId;
            DestinationNodeId = destinationNodeId;
            Origin = origin;
            Destination = destination;
            Legs = new ReadOnlyCollection<TransitEdge>(new List<TransitEdge>(legs ?? Array.Empty<TransitEdge>()));

            var totalCost = 0;
            foreach (var leg in Legs)
            {
                totalCost += leg.Cost;
            }

            TotalCost = totalCost;
        }

        public EntityId OriginNodeId { get; }

        public EntityId DestinationNodeId { get; }

        public CellCoordinate Origin { get; }

        public CellCoordinate Destination { get; }

        public IReadOnlyList<TransitEdge> Legs { get; }

        public int TotalCost { get; }

        public bool IsEmpty => Legs.Count == 0 && !OriginNodeId.Equals(DestinationNodeId);

        public bool RequiresVerticalTransit
        {
            get
            {
                foreach (var leg in Legs)
                {
                    if (leg.Mode == TransitMode.Elevator || leg.Mode == TransitMode.Stairs)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
