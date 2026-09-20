using System;
using System.Collections.Generic;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Population
{
    /// <summary>
    /// Assigns training opportunities from room capacity, then lets residents acquire roles
    /// autonomously through deterministic suitability ranking. This is intentionally not a
    /// player-to-person assignment system.
    /// </summary>
    public sealed class SpecialistRoleSystem
    {
        public const float TrainingProgressPerTick = 0.34f;

        public SpecialistTrainingCapacity Capacity { get; private set; } = new SpecialistTrainingCapacity(0, 0, 0, 0);
        public float ServiceEfficiencyMultiplier { get; private set; } = 1f;
        public float CrisisResponseMultiplier { get; private set; } = 1f;

        public void Advance(PopulationState population, BuildingTopologyState topology, Tick currentTick)
        {
            if (population == null || topology == null) return;
            Capacity = SpecialistTrainingCapacity.FromTopology(topology);
            TrainForRole(population, SpecialistRole.Service, Capacity.ServiceSlots);
            TrainForRole(population, SpecialistRole.Maintenance, Capacity.MaintenanceSlots);
            TrainForRole(population, SpecialistRole.Security, Capacity.SecuritySlots);
            TrainForRole(population, SpecialistRole.Knowledge, Capacity.KnowledgeSlots);
            RecalculateMultipliers(population);
        }

        private static void TrainForRole(PopulationState population, SpecialistRole role, int capacity)
        {
            var occupied = 0;
            for (var i = 0; i < population.Persons.Count; i++)
            {
                var state = population.Persons[i].Specialization;
                if (state.Role == role || state.TrainingRole == role) occupied++;
            }

            while (occupied < capacity)
            {
                PersonRecord candidate = null;
                var bestScore = float.MinValue;
                for (var i = 0; i < population.Persons.Count; i++)
                {
                    var person = population.Persons[i];
                    if (person.Specialization.Role != SpecialistRole.None || person.Specialization.IsTraining) continue;
                    var score = Suitability(person, role);
                    if (candidate == null || score > bestScore || (Math.Abs(score - bestScore) < .0001f && person.Id.Value < candidate.Id.Value))
                    {
                        candidate = person;
                        bestScore = score;
                    }
                }
                if (candidate == null) return;
                candidate.Specialization.AdvanceTowards(role, TrainingProgressPerTick);
                occupied++;
            }

            for (var i = 0; i < population.Persons.Count; i++)
            {
                var state = population.Persons[i].Specialization;
                if (state.TrainingRole == role) state.AdvanceTowards(role, TrainingProgressPerTick);
            }
        }

        private void RecalculateMultipliers(PopulationState population)
        {
            var service = 0;
            var knowledge = 0;
            var maintenance = 0;
            var security = 0;
            for (var i = 0; i < population.Persons.Count; i++)
            {
                switch (population.Persons[i].Specialization.Role)
                {
                    case SpecialistRole.Service: service++; break;
                    case SpecialistRole.Knowledge: knowledge++; break;
                    case SpecialistRole.Maintenance: maintenance++; break;
                    case SpecialistRole.Security: security++; break;
                }
            }
            ServiceEfficiencyMultiplier = 1f + Math.Min(.20f, service * .04f + knowledge * .02f);
            CrisisResponseMultiplier = 1f + Math.Min(.25f, maintenance * .08f + security * .06f + knowledge * .02f);
        }

        private static float Suitability(PersonRecord person, SpecialistRole role)
        {
            var score = 1f - person.GetNeedSatisfaction(NeedKind.Purpose);
            if (person.Traits.Count == 0) return score;
            var trait = person.Traits[0].Kind;
            if (role == SpecialistRole.Maintenance && trait == PersonTraitKind.Frugal) score += .35f;
            if (role == SpecialistRole.Security && trait == PersonTraitKind.EarlyBird) score += .35f;
            if (role == SpecialistRole.Service && trait == PersonTraitKind.Extrovert) score += .35f;
            if (role == SpecialistRole.Knowledge && trait == PersonTraitKind.NightOwl) score += .35f;
            return score;
        }
    }

    /// <summary>Immutable per-role training-slot counts derived from the built tower.</summary>
    public readonly struct SpecialistTrainingCapacity
    {
        public SpecialistTrainingCapacity(int maintenanceSlots, int securitySlots, int serviceSlots, int knowledgeSlots)
        {
            MaintenanceSlots = maintenanceSlots;
            SecuritySlots = securitySlots;
            ServiceSlots = serviceSlots;
            KnowledgeSlots = knowledgeSlots;
        }

        public int MaintenanceSlots { get; }
        public int SecuritySlots { get; }
        public int ServiceSlots { get; }
        public int KnowledgeSlots { get; }

        public static SpecialistTrainingCapacity FromTopology(BuildingTopologyState topology)
        {
            var maintenance = 0; var security = 0; var service = 0; var knowledge = 0;
            foreach (var room in topology.Rooms.Values)
            {
                var content = room.ContentType.Value ?? string.Empty;
                var slots = Math.Max(1, room.Capacity / 8);
                if (content.Contains("training"))
                {
                    maintenance += slots;
                    security += slots;
                    service += slots;
                    knowledge += slots;
                }
                else if (content.Contains("maintenance")) maintenance += slots;
                else if (content.Contains("security")) security += slots;
                else if (content.Contains("diner") || content.Contains("retail")) service += slots;
                else if (content.Contains("office")) knowledge += slots;
            }
            return new SpecialistTrainingCapacity(maintenance, security, service, knowledge);
        }
    }
}
