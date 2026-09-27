using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Time;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Mutable domain record for a single resident.
    /// Simulation systems mutate activity and need state through explicit methods;
    /// no public property setters exist to prevent accidental external mutation.
    /// </summary>
    public sealed class PersonRecord
    {
        private readonly List<NeedState> _needs;
        private ActivityKind _currentActivity;
        private ResidentPurposeKind _currentPurpose;
        private long _purposeStartedAtTick;
        private long _purposeEndsAtTick;
        private long _outsideFoodRetryAfterTick;

        public PersonRecord(
            EntityId id,
            EntityId householdId,
            EntityId homeRoomId,
            EntityId workplaceRoomId,
            DailySchedule schedule,
            IEnumerable<NeedState> needs,
            IEnumerable<PersonTrait> traits,
            IEnumerable<PersonalityFacet> personalityFacets = null,
            bool worksOutside = false)
        {
            id.EnsureValid();
            householdId.EnsureValid();
            homeRoomId.EnsureValid();
            if (!worksOutside) workplaceRoomId.EnsureValid();

            Id = id;
            HouseholdId = householdId;
            HomeRoomId = homeRoomId;
            WorkplaceRoomId = workplaceRoomId;
            WorkplaceLocation = worksOutside ? WorldLocation.Outside : WorldLocation.InRoom(workplaceRoomId);
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));

            _needs = needs != null
                ? new List<NeedState>(needs)
                : new List<NeedState>();

            Needs = new ReadOnlyCollection<NeedState>(_needs);

            Traits = traits != null
                ? new ReadOnlyCollection<PersonTrait>(new List<PersonTrait>(traits))
                : new ReadOnlyCollection<PersonTrait>(new List<PersonTrait>());
            PersonalityFacets = personalityFacets != null
                ? new ReadOnlyCollection<PersonalityFacet>(new List<PersonalityFacet>(personalityFacets))
                : new ReadOnlyCollection<PersonalityFacet>(DeriveFacets(Traits));
            Wellbeing = new ResidentWellbeingState();
            Specialization = new SpecialistRoleState();

            _currentActivity = ActivityKind.Idle;
            _currentRoomId = homeRoomId;
            _currentLocation = WorldLocation.InRoom(homeRoomId);
        }

        // ── Identity ──────────────────────────────────────────────────────────

        public EntityId Id { get; }

        public EntityId HouseholdId { get; }

        public EntityId HomeRoomId { get; }

        public EntityId WorkplaceRoomId { get; }

        public WorldLocation WorkplaceLocation { get; }

        public WorldLocation CurrentLocation => _currentLocation;

        public EntityId CurrentRoomId => _currentRoomId;

        private EntityId _currentRoomId;
        private WorldLocation _currentLocation;

        // ── Schedule & activity ───────────────────────────────────────────────

        public DailySchedule Schedule { get; }

        public ActivityKind CurrentActivity => _currentActivity;
        public ResidentPurposeKind CurrentPurpose => _currentPurpose;
        public long PurposeStartedAtTick => _purposeStartedAtTick;
        public long PurposeEndsAtTick => _purposeEndsAtTick;
        public long OutsideFoodRetryAfterTick => _outsideFoodRetryAfterTick;
        public bool IsOutsideFoodRetryBlocked(Tick tick) => tick.Value < _outsideFoodRetryAfterTick;
        public bool HasCommittedPurposeAt(Tick tick) =>
            _currentPurpose != ResidentPurposeKind.None && tick.Value < _purposeEndsAtTick;

        // ── Needs & traits ────────────────────────────────────────────────────

        public IReadOnlyList<NeedState> Needs { get; }

        public IReadOnlyList<PersonTrait> Traits { get; }
        public IReadOnlyList<PersonalityFacet> PersonalityFacets { get; }
        public ResidentWellbeingState Wellbeing { get; }
        public SpecialistRoleState Specialization { get; }

        // ── Mutation methods (called by simulation systems only) ──────────────

        /// <summary>Updates the resident's current activity. Called by the schedule resolution system.</summary>
        public void UpdateActivity(ActivityKind activity)
        {
            if (_currentActivity != activity)
            {
                _currentPurpose = ResidentPurposeKind.None;
                _purposeStartedAtTick = 0;
                _purposeEndsAtTick = 0;
            }
            _currentActivity = activity;
        }

        public void CommitPurpose(ResidentPurposeKind purpose, Tick startTick, long minimumTicks)
        {
            if (minimumTicks <= 0) throw new ArgumentOutOfRangeException(nameof(minimumTicks));
            _currentPurpose = purpose;
            _purposeStartedAtTick = startTick.Value;
            _purposeEndsAtTick = checked(startTick.Value + minimumTicks);
        }

        public void RestorePurpose(ResidentPurposeKind purpose, long startedAtTick, long endsAtTick)
        {
            if (purpose == ResidentPurposeKind.None || startedAtTick < 0 || endsAtTick <= startedAtTick)
                return;
            _currentPurpose = purpose;
            _purposeStartedAtTick = startedAtTick;
            _purposeEndsAtTick = endsAtTick;
        }

        public void BlockOutsideFoodUntil(long tick)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            _outsideFoodRetryAfterTick = Math.Max(_outsideFoodRetryAfterTick, tick);
        }

        public void RestoreOutsideFoodRetryAfter(long tick)
        {
            _outsideFoodRetryAfterTick = Math.Max(0, tick);
        }

        /// <summary>Updates the resident's current room location. Called upon trip arrival.</summary>
        public void UpdateLocation(EntityId roomId)
        {
            UpdateLocation(WorldLocation.InRoom(roomId));
        }

        public void UpdateLocation(WorldLocation location)
        {
            if (!_currentLocation.Equals(location))
            {
                _currentPurpose = ResidentPurposeKind.None;
                _purposeStartedAtTick = 0;
                _purposeEndsAtTick = 0;
            }
            _currentLocation = location;
            if (!location.IsOutside) _currentRoomId = location.RoomId;
        }

        /// <summary>
        /// Updates the satisfaction level for <paramref name="kind"/>.
        /// If no need of that kind exists, a new entry is appended.
        /// </summary>
        public void UpdateNeed(NeedKind kind, float satisfaction)
        {
            for (var i = 0; i < _needs.Count; i++)
            {
                if (_needs[i].Kind == kind)
                {
                    _needs[i] = _needs[i].WithSatisfaction(satisfaction);
                    return;
                }
            }

            _needs.Add(new NeedState(kind, satisfaction));
        }

        /// <summary>Restores a persisted emergent role state; only aggregate loading calls this.</summary>
        public void RestoreSpecialization(SpecialistRole role, SpecialistRole trainingRole, float trainingProgress)
        {
            Specialization.Restore(role, trainingRole, trainingProgress);
        }

        /// <summary>
        /// Gets the current satisfaction for <paramref name="kind"/>, defaulting to 1f if untracked.
        /// </summary>
        public float GetNeedSatisfaction(NeedKind kind)
        {
            for (var i = 0; i < _needs.Count; i++)
            {
                if (_needs[i].Kind == kind)
                {
                    return _needs[i].Satisfaction;
                }
            }

            return 1f;
        }

        /// <summary>
        /// Returns true if this person tracks <paramref name="kind"/>.
        /// </summary>
        public bool HasNeed(NeedKind kind)
        {
            for (var i = 0; i < _needs.Count; i++)
            {
                if (_needs[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString() =>
            $"Person {Id} (Household {HouseholdId}, Home {HomeRoomId}, Work {WorkplaceRoomId}, Activity {_currentActivity})";

        private static List<PersonalityFacet> DeriveFacets(IReadOnlyList<PersonTrait> traits)
        {
            var result = new List<PersonalityFacet>();
            if (traits == null || traits.Count == 0) return result;
            switch (traits[0].Kind)
            {
                case PersonTraitKind.EarlyBird: result.Add(new PersonalityFacet(PersonalityFacetKind.CommuteSensitive)); break;
                case PersonTraitKind.NightOwl: result.Add(new PersonalityFacet(PersonalityFacetKind.PrivacySeeking)); break;
                case PersonTraitKind.Introvert: result.Add(new PersonalityFacet(PersonalityFacetKind.Resilient)); break;
                case PersonTraitKind.Extrovert: result.Add(new PersonalityFacet(PersonalityFacetKind.CommunityRooted)); break;
                case PersonTraitKind.Frugal: result.Add(new PersonalityFacet(PersonalityFacetKind.FinanciallyCautious)); break;
                case PersonTraitKind.Spendthrift: result.Add(new PersonalityFacet(PersonalityFacetKind.ServiceExpectant)); break;
            }
            return result;
        }
    }
}
