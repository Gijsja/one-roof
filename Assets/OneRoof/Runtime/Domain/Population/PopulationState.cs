using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Randomness;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Indexed collection of all persons and households in the active simulation.
    /// Acts as the authoritative in-memory store for population state.
    /// Presentation receives read-only projections derived from this store, not direct references.
    /// </summary>
    public sealed class PopulationState
    {
        private readonly Dictionary<EntityId, PersonRecord> _persons;
        private readonly Dictionary<EntityId, HouseholdRecord> _households;
        private readonly List<PersonRecord> _personList;
        private readonly List<HouseholdRecord> _householdList;

        public PopulationState(
            IEnumerable<PersonRecord> persons,
            IEnumerable<HouseholdRecord> households)
        {
            _persons = new Dictionary<EntityId, PersonRecord>();
            _households = new Dictionary<EntityId, HouseholdRecord>();
            _personList = new List<PersonRecord>();
            _householdList = new List<HouseholdRecord>();

            if (persons != null)
            {
                foreach (var person in persons)
                {
                    if (person == null)
                    {
                        throw new ArgumentNullException(nameof(persons), "Person entries must not be null.");
                    }

                    if (_persons.ContainsKey(person.Id))
                    {
                        throw new ArgumentException($"Duplicate person ID {person.Id}.", nameof(persons));
                    }

                    _persons.Add(person.Id, person);
                    _personList.Add(person);
                }
            }

            if (households != null)
            {
                foreach (var household in households)
                {
                    if (household == null)
                    {
                        throw new ArgumentNullException(nameof(households), "Household entries must not be null.");
                    }

                    if (_households.ContainsKey(household.Id))
                    {
                        throw new ArgumentException($"Duplicate household ID {household.Id}.", nameof(households));
                    }

                    _households.Add(household.Id, household);
                    _householdList.Add(household);
                }
            }
        }

        // ── Collections ───────────────────────────────────────────────────────

        /// <summary>All persons in insertion order.</summary>
        public IReadOnlyList<PersonRecord> Persons => _personList;

        /// <summary>All households in insertion order.</summary>
        public IReadOnlyList<HouseholdRecord> Households => _householdList;

        public int ResidentCount => _persons.Count;

        public void AddPerson(PersonRecord person)
        {
            if (person == null) throw new ArgumentNullException(nameof(person));
            if (_persons.ContainsKey(person.Id)) throw new ArgumentException($"Duplicate person ID {person.Id}.");

            _persons.Add(person.Id, person);
            _personList.Add(person);
        }

        public void AddHousehold(HouseholdRecord household)
        {
            if (household == null) throw new ArgumentNullException(nameof(household));
            if (_households.ContainsKey(household.Id)) throw new ArgumentException($"Duplicate household ID {household.Id}.");

            _households.Add(household.Id, household);
            _householdList.Add(household);
        }

        public int PersonCount => _persons.Count;

        public int HouseholdCount => _households.Count;

        // ── Lookups ───────────────────────────────────────────────────────────

        public PersonRecord GetPerson(EntityId id)
        {
            if (!_persons.TryGetValue(id, out var person))
            {
                throw new KeyNotFoundException($"Person {id} not found in population state.");
            }

            return person;
        }

        public bool TryGetPerson(EntityId id, out PersonRecord person) =>
            _persons.TryGetValue(id, out person);

        public HouseholdRecord GetHousehold(EntityId id)
        {
            if (!_households.TryGetValue(id, out var household))
            {
                throw new KeyNotFoundException($"Household {id} not found in population state.");
            }

            return household;
        }

        public bool TryGetHousehold(EntityId id, out HouseholdRecord household) =>
            _households.TryGetValue(id, out household);

        // ── Serialization ──────────────────────────────────────────────────────

        public PopulationSaveData ToSaveData()
        {
            var householdList = new List<HouseholdSaveData>(_householdList.Count);
            foreach (var h in _householdList)
            {
                var mIds = new int[h.MemberIds.Count];
                for (var i = 0; i < h.MemberIds.Count; i++) mIds[i] = h.MemberIds[i].Value;

                householdList.Add(new HouseholdSaveData
                {
                    id = h.Id.Value,
                    homeRoomId = h.HomeRoomId.Value,
                    memberIds = mIds,
                    budget = h.Budget,
                    satisfaction = h.Satisfaction
                });
            }

            var personList = new List<PersonSaveData>(_personList.Count);
            foreach (var p in _personList)
            {
                float hunger = 1f, rest = 1f, social = 1f;
                foreach (var need in p.Needs)
                {
                    if (need.Kind == NeedKind.Hunger) hunger = need.Satisfaction;
                    else if (need.Kind == NeedKind.Rest) rest = need.Satisfaction;
                    else if (need.Kind == NeedKind.Social) social = need.Satisfaction;
                }

                personList.Add(new PersonSaveData
                {
                    id = p.Id.Value,
                    householdId = p.HouseholdId.Value,
                    homeRoomId = p.HomeRoomId.Value,
                    workplaceRoomId = p.WorkplaceRoomId.Value,
                    currentRoomId = p.CurrentRoomId.Value,
                    currentActivity = (int)p.CurrentActivity,
                    trait = p.Traits.Count > 0 ? (int)p.Traits[0].Kind : 0,
                    hungerSatisfaction = hunger,
                    restSatisfaction = rest,
                    socialSatisfaction = social
                });
            }

            return new PopulationSaveData
            {
                households = householdList.ToArray(),
                persons = personList.ToArray()
            };
        }

        public static PopulationState FromSaveData(PopulationSaveData data, IRandomStream randomStream = null)
        {
            var households = new List<HouseholdRecord>();
            if (data?.households != null)
            {
                foreach (var h in data.households)
                {
                    var mList = new List<EntityId>();
                    if (h.memberIds != null)
                    {
                        foreach (var mid in h.memberIds) mList.Add(new EntityId(mid));
                    }
                    households.Add(new HouseholdRecord(new EntityId(h.id), mList, new EntityId(h.homeRoomId), h.budget, h.satisfaction));
                }
            }

            var persons = new List<PersonRecord>();
            if (data?.persons != null)
            {
                var rng = randomStream ?? new DeterministicRandomStream(1337);
                foreach (var p in data.persons)
                {
                    var traitKind = Enum.IsDefined(typeof(PersonTraitKind), p.trait) ? (PersonTraitKind)p.trait : PersonTraitKind.EarlyBird;
                    var trait = new PersonTrait(traitKind);
                    var schedule = DailySchedule.Standard(trait, rng, baseSleepEnd: 15);
                    var needs = new[]
                    {
                        new NeedState(NeedKind.Hunger, p.hungerSatisfaction),
                        new NeedState(NeedKind.Rest, p.restSatisfaction),
                        new NeedState(NeedKind.Social, p.socialSatisfaction)
                    };
                    var traits = new[] { trait };
                    var person = new PersonRecord(
                        new EntityId(p.id),
                        new EntityId(p.householdId),
                        new EntityId(p.homeRoomId),
                        new EntityId(p.workplaceRoomId),
                        schedule,
                        needs,
                        traits);

                    person.UpdateLocation(new EntityId(p.currentRoomId > 0 ? p.currentRoomId : p.homeRoomId));
                    person.UpdateActivity((ActivityKind)p.currentActivity);
                    persons.Add(person);
                }
            }

            return new PopulationState(persons, households);
        }
    }
}
