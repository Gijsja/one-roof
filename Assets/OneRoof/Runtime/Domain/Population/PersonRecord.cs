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
            bool worksOutside = false,
            IEnumerable<SocialTrait> socialTraits = null)
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
            SocialTraits = socialTraits != null
                ? new ReadOnlyCollection<SocialTrait>(new List<SocialTrait>(socialTraits))
                : new ReadOnlyCollection<SocialTrait>(DeriveSocialTraits(Traits, Id));
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

        public EntityId WorkplaceRoomId { get; private set; }

        public WorldLocation WorkplaceLocation { get; private set; }

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
        public IReadOnlyList<SocialTrait> SocialTraits { get; }
        public ResidentWellbeingState Wellbeing { get; }
        public SpecialistRoleState Specialization { get; }

        // ── Mutation methods (called by simulation systems only) ──────────────

        /// <summary>
        /// Moves an unfunded tower worker to the typed Outside labor market. A resident
        /// already commuting keeps their destination until that trip resolves; callers
        /// can retry at the next staffing reconciliation.
        /// </summary>
        public bool ReassignToOutsideWork()
        {
            if (WorkplaceLocation.IsOutside) return true;
            if (_currentActivity == ActivityKind.Commuting) return false;

            WorkplaceRoomId = default;
            WorkplaceLocation = WorldLocation.Outside;
            if (_currentActivity == ActivityKind.Working)
                UpdateActivity(ActivityKind.Idle);
            else if (_currentPurpose == ResidentPurposeKind.WorkingInside)
            {
                _currentPurpose = ResidentPurposeKind.None;
                _purposeStartedAtTick = 0;
                _purposeEndsAtTick = 0;
            }
            return true;
        }

        /// <summary>Accepts a funded tower job without changing the resident's physical location.</summary>
        public bool ReassignToRoomWork(EntityId roomId)
        {
            roomId.EnsureValid();
            if (!WorkplaceLocation.IsOutside) return WorkplaceRoomId.Equals(roomId);
            if (_currentActivity == ActivityKind.Commuting) return false;

            WorkplaceRoomId = roomId;
            WorkplaceLocation = WorldLocation.InRoom(roomId);
            if (_currentActivity == ActivityKind.Working)
                UpdateActivity(ActivityKind.Idle);
            else if (_currentPurpose == ResidentPurposeKind.WorkingOutside)
            {
                _currentPurpose = ResidentPurposeKind.None;
                _purposeStartedAtTick = 0;
                _purposeEndsAtTick = 0;
            }
            return true;
        }

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

        private static List<SocialTrait> DeriveSocialTraits(IReadOnlyList<PersonTrait> traits, EntityId id)
        {
            var result = new List<SocialTrait>();
            if (traits != null && traits.Count > 0)
            {
                switch (traits[0].Kind)
                {
                    case PersonTraitKind.Extrovert: result.Add(new SocialTrait(SocialTraitKind.Charismatic)); break;
                    case PersonTraitKind.Introvert: result.Add(new SocialTrait(SocialTraitKind.Loyal)); break;
                    case PersonTraitKind.NightOwl: result.Add(new SocialTrait(SocialTraitKind.Flirt)); break;
                    case PersonTraitKind.EarlyBird: result.Add(new SocialTrait(SocialTraitKind.HopelessRomantic)); break;
                    case PersonTraitKind.Spendthrift: result.Add(new SocialTrait(SocialTraitKind.HotHeaded)); break;
                    case PersonTraitKind.Frugal: result.Add(new SocialTrait(SocialTraitKind.GrudgeHolder)); break;
                }
            }
            if (result.Count == 0 && id.IsValid)
            {
                var kind = (SocialTraitKind)(Math.Abs(id.Value) % 8);
                result.Add(new SocialTrait(kind));
            }
            return result;
        }
    }
}
