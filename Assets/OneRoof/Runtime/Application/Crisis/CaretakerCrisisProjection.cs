using System;
using System.Collections.Generic;
using OneRoof.Domain.Crisis;

namespace OneRoof.Application.Crisis
{
    public sealed class CaretakerOpponentProjection
    {
        public int Id { get; }
        public int ResidentId { get; }
        public CaretakerOpponentRole Role { get; }
        public int X { get; }
        public int Health { get; }
        public int Facing { get; }
        public int Timer { get; }
        public int AttackFrame { get; }

        internal CaretakerOpponentProjection(CaretakerOpponent value)
        {
            Id = value.Id;
            ResidentId = value.ResidentId;
            Role = value.Role;
            X = value.X;
            Health = value.Health;
            Facing = value.Facing;
            Timer = value.Timer;
            AttackFrame = value.AttackFrame;
        }
    }

    public sealed class CaretakerRouteProjection
    {
        public int Floor { get; }
        public int MinX { get; }
        public int MaxX { get; }
        public int EntryX { get; }
        public int ExitX { get; }
        public bool Encounter { get; }

        internal CaretakerRouteProjection(CaretakerRouteFloor value)
        {
            Floor = value.Floor;
            MinX = value.MinX;
            MaxX = value.MaxX;
            EntryX = value.EntryX;
            ExitX = value.ExitX;
            Encounter = value.Encounter;
        }
    }

    public sealed class CaretakerEvictionProjection
    {
        public int ResidentId { get; }
        public int Floor { get; }
        public string FactionId { get; }
        public string Cause { get; }

        internal CaretakerEvictionProjection(CaretakerEvictionRecord value)
        {
            ResidentId = value.ResidentId;
            Floor = value.Floor;
            FactionId = value.FactionId;
            Cause = value.Cause;
        }
    }

    /// <summary>Detached immutable snapshot; no mutable aggregate references escape the application boundary.</summary>
    public sealed class CaretakerCrisisProjection
    {
        public CaretakerCrisisPhase Phase { get; }
        public string FactionId { get; }
        public string Cause { get; }
        public int WarningDays { get; }
        public int ReliefDays { get; }
        public int RouteIndex { get; }
        public int CurrentFloor { get; }
        public int X { get; }
        public int Facing { get; }
        public int Health { get; }
        public CaretakerAttack Attack { get; }
        public int AttackFrame { get; }
        public int JumpFrames { get; }
        public bool Ducking { get; }
        public bool Paused { get; }
        public int Clock { get; }
        public int Score { get; }
        public bool FloorCleared { get; }
        public bool Practice { get; }
        public bool ConsequencesApplied { get; }
        public IReadOnlyList<int> AffectedResidentIds { get; }
        public IReadOnlyList<int> AffectedFloors { get; }
        public IReadOnlyList<CaretakerRouteProjection> Route { get; }
        public IReadOnlyList<CaretakerOpponentProjection> Opponents { get; }
        public IReadOnlyList<CaretakerEvictionProjection> Evictions { get; }

        public CaretakerCrisisProjection(CaretakerCrisisState state, CaretakerCrisisProjection previous = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            Phase = state.Phase;
            FactionId = state.FactionId;
            Cause = state.Cause;
            WarningDays = state.WarningDays;
            ReliefDays = state.ReliefDays;
            RouteIndex = state.RouteIndex;
            CurrentFloor = state.CurrentFloor;
            X = state.X;
            Facing = state.Facing;
            Health = state.Health;
            Attack = state.Attack;
            AttackFrame = state.AttackFrame;
            JumpFrames = state.JumpFrames;
            Ducking = state.Ducking;
            Paused = state.Paused;
            Clock = state.Clock;
            Score = state.Score;
            FloorCleared = state.FloorCleared;
            Practice = state.Practice;
            ConsequencesApplied = state.ConsequencesApplied;
            AffectedResidentIds = Copy(state.AffectedResidentIds, previous?.AffectedResidentIds);
            AffectedFloors = Copy(state.AffectedFloors, previous?.AffectedFloors);
            if (SameRoute(state.Route, previous?.Route))
                Route = previous.Route;
            else
            {
                var route = new CaretakerRouteProjection[state.Route.Count];
                for (var i = 0; i < route.Length; i++)
                    route[i] = new CaretakerRouteProjection(state.Route[i]);
                Route = Array.AsReadOnly(route);
            }

            var opponents = new CaretakerOpponentProjection[state.Opponents.Count];
            for (var i = 0; i < opponents.Length; i++)
                opponents[i] = new CaretakerOpponentProjection(state.Opponents[i]);
            Opponents = Array.AsReadOnly(opponents);
            var evictions = new CaretakerEvictionProjection[state.Evictions.Count];
            for (var i = 0; i < evictions.Length; i++)
                evictions[i] = new CaretakerEvictionProjection(state.Evictions[i]);
            Evictions = Array.AsReadOnly(evictions);
        }

        private static bool SameRoute(IReadOnlyList<CaretakerRouteFloor> source, IReadOnlyList<CaretakerRouteProjection> previous)
        {
            if (previous == null || source.Count != previous.Count)
                return false;
            for (var i = 0; i < source.Count; i++)
            {
                var a = source[i];
                var b = previous[i];
                if (a.Floor != b.Floor || a.MinX != b.MinX || a.MaxX != b.MaxX || a.EntryX != b.EntryX || a.ExitX != b.ExitX || a.Encounter != b.Encounter)
                    return false;
            }

            return true;
        }

        private static IReadOnlyList<int> Copy(IReadOnlyList<int> source, IReadOnlyList<int> previous)
        {
            var unchanged = previous != null && source.Count == previous.Count;
            if (unchanged)
                for (var i = 0; i < source.Count; i++)
                    if (source[i] != previous[i])
                    {
                        unchanged = false;
                        break;
                    }

            if (unchanged)
                return previous;
            var items = new int[source.Count];
            for (var i = 0; i < items.Length; i++)
                items[i] = source[i];
            return Array.AsReadOnly(items);
        }
    }
}
