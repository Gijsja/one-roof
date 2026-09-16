using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OneRoof.Domain.Identity;

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
    }
}
