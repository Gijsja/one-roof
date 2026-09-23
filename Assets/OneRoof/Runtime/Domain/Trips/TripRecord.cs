using System;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;
using OneRoof.Domain.Transit;

namespace OneRoof.Domain.Trips
{
    /// <summary>
    /// Mutable domain record for a single resident trip.
    /// State transitions are enforced through explicit mutation methods.
    /// <para>
    /// Matches the Data Contracts <c>Trip</c> record:
    /// person, origin, destination, purpose, departure tick, route state, wait time.
    /// </para>
    /// </summary>
    public sealed class TripRecord
    {
        public TripRecord(
            EntityId id,
            EntityId personId,
            EntityId originRoomId,
            EntityId destinationRoomId,
            TripPurpose purpose,
            Tick departureTick,
            TransitRoute plannedRoute)
            : this(id, personId, WorldLocation.InRoom(originRoomId), WorldLocation.InRoom(destinationRoomId), purpose, departureTick, plannedRoute)
        {
        }

        public TripRecord(EntityId id, EntityId personId, WorldLocation origin, WorldLocation destination,
            TripPurpose purpose, Tick departureTick, TransitRoute plannedRoute)
        {
            id.EnsureValid();
            personId.EnsureValid();

            Id = id;
            PersonId = personId;
            Origin = origin;
            Destination = destination;
            Purpose = purpose;
            DepartureTick = departureTick;
            PlannedRoute = plannedRoute; // nullable — null when origin == destination or no route found
            State = TripState.Planned;
            WaitTicks = 0;
            CompletionTick = null;
        }

        // ── Identity ──────────────────────────────────────────────────────────

        public EntityId Id { get; }

        public EntityId PersonId { get; }

        // ── Spatial ───────────────────────────────────────────────────────────

        public WorldLocation Origin { get; }
        public EntityId OriginRoomId => Origin.IsOutside ? throw new InvalidOperationException("Outside has no room ID.") : Origin.RoomId;

        public WorldLocation Destination { get; }
        public EntityId DestinationRoomId => Destination.IsOutside ? throw new InvalidOperationException("Outside has no room ID.") : Destination.RoomId;

        /// <summary>True when origin and destination rooms are the same (no travel needed).</summary>
        public bool IsLocal => Origin.Equals(Destination);

        // ── Intent & timing ───────────────────────────────────────────────────

        public TripPurpose Purpose { get; }

        public Tick DepartureTick { get; }

        public Tick? CompletionTick { get; private set; }

        /// <summary>
        /// The planned route from origin portal to destination portal.
        /// Null when origin equals destination, or when no connected route exists.
        /// </summary>
        public TransitRoute PlannedRoute { get; }

        // ── State & metrics ───────────────────────────────────────────────────

        public TripState State { get; private set; }

        /// <summary>Total ticks spent waiting in elevator queues during this trip.</summary>
        public int WaitTicks { get; private set; }

        // ── Mutation methods (called by simulation systems only) ──────────────

        /// <summary>
        /// Transitions from <see cref="TripState.Planned"/> to <see cref="TripState.InProgress"/>.
        /// </summary>
        public void Begin()
        {
            if (State != TripState.Planned)
            {
                throw new InvalidOperationException(
                    $"Trip {Id}: Begin() called in state {State}; expected Planned.");
            }

            State = TripState.InProgress;
        }

        /// <summary>
        /// Transitions from <see cref="TripState.InProgress"/> to <see cref="TripState.Completed"/>
        /// and records the arrival tick.
        /// </summary>
        public void Complete(Tick arrivalTick)
        {
            if (State != TripState.InProgress)
            {
                throw new InvalidOperationException(
                    $"Trip {Id}: Complete() called in state {State}; expected InProgress.");
            }

            State = TripState.Completed;
            CompletionTick = arrivalTick;
        }

        /// <summary>
        /// Cancels a trip from any non-terminal state.
        /// </summary>
        public void Cancel()
        {
            if (State == TripState.Completed || State == TripState.Cancelled)
            {
                throw new InvalidOperationException(
                    $"Trip {Id}: Cancel() called in terminal state {State}.");
            }

            State = TripState.Cancelled;
        }

        /// <summary>
        /// Accumulates elevator queue wait ticks during transit.
        /// </summary>
        public void AddWaitTicks(int ticks)
        {
            if (ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks), ticks, "Wait ticks cannot be negative.");
            }

            WaitTicks += ticks;
        }

        public override string ToString() =>
            $"Trip {Id} [{State}] Person {PersonId}: {Origin} → {Destination} ({Purpose}) @{DepartureTick}";
    }
}
